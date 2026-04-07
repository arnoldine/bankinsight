using LegacyMigrationPrep;

if (args.Length > 0 && string.Equals(args[0], "inspect", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: LegacyMigrationPrep inspect <xlsx-path> <sheet-name>");
        return;
    }

    var rows = XlsxReader.ReadSheet(args[1], args[2]);
    Console.WriteLine($"Rows: {rows.Count}");
    foreach (var row in rows.Take(10))
    {
        Console.WriteLine(string.Join(" | ", row.Select(kvp => $"{kvp.Key}={kvp.Value}")));
    }

    return;
}

if (args.Length > 0 && string.Equals(args[0], "summarize-tb", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: LegacyMigrationPrep summarize-tb <xlsx-path> <sheet-name>");
        return;
    }

    var rows = XlsxReader.ReadSheet(args[1], args[2]);
    var grouped = rows
        .GroupBy(row => row.TryGetValue("Account Code", out var code) ? code : string.Empty)
        .Select(group => new
        {
            AccountCode = group.Key,
            Description = group.FirstOrDefault()?.GetValueOrDefault("Description") ?? string.Empty,
            Branches = group.Count(),
            TotalBalance = group.Sum(row =>
            {
                var raw = row.GetValueOrDefault("Balance") ?? "0";
                return decimal.TryParse(raw, out var value) ? value : 0m;
            })
        })
        .OrderBy(item => item.AccountCode)
        .ToList();

    Console.WriteLine($"Unique accounts: {grouped.Count}");
    foreach (var item in grouped.Take(80))
    {
        Console.WriteLine($"{item.AccountCode} | {item.Description} | branches={item.Branches} | total={item.TotalBalance}");
    }

    return;
}

if (args.Length > 0 && string.Equals(args[0], "build-tb-gl", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Usage: LegacyMigrationPrep build-tb-gl <xlsx-path> <sheet-name> <output-dir>");
        return;
    }

    var rows = XlsxReader.ReadSheet(args[1], args[2]);
    var trialBalanceOutputDir = args[3];
    Directory.CreateDirectory(trialBalanceOutputDir);

    var details = rows
        .Select(row =>
        {
            var (code, currency) = ParseAccountCode(row.GetValueOrDefault("Account Code"));
            var branchCode = (row.GetValueOrDefault("Branch Code") ?? string.Empty).Trim();
            var description = (row.GetValueOrDefault("Description") ?? string.Empty).Trim();
            var balance = ParseDecimal(row.GetValueOrDefault("Balance"));

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["code"] = code,
                ["branch_code"] = branchCode,
                ["name"] = description,
                ["category"] = MapTrialBalanceCategory(code, description),
                ["currency"] = currency,
                ["balance"] = balance.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["is_header"] = "false"
            };
        })
        .Where(row => !string.IsNullOrWhiteSpace(row["code"]))
        .OrderBy(row => row["code"])
        .ThenBy(row => row["branch_code"])
        .ToList();

    var aggregated = details
        .GroupBy(row => $"{row["code"]}|{row["currency"]}", StringComparer.OrdinalIgnoreCase)
        .Select(group =>
        {
            var first = group.First();
            var total = group.Sum(item => ParseDecimal(item["balance"]));
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["code"] = first["code"],
                ["name"] = first["name"],
                ["category"] = first["category"],
                ["currency"] = first["currency"],
                ["balance"] = total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["is_header"] = "false"
            };
        })
        .OrderBy(row => row["code"])
        .ToList();

    CsvWriter.Write(
        Path.Combine(trialBalanceOutputDir, "gl_accounts.csv"),
        aggregated,
        "code", "name", "category", "currency", "balance", "is_header");

    CsvWriter.Write(
        Path.Combine(trialBalanceOutputDir, "gl_accounts_branch_detail.csv"),
        details,
        "code", "branch_code", "name", "category", "currency", "balance", "is_header");

    var summary = string.Join(Environment.NewLine, new[]
    {
        "# Trial Balance Migration Summary",
        string.Empty,
        $"- Source workbook: `{args[1]}`",
        $"- Sheet: `{args[2]}`",
        $"- Raw branch-level rows: `{details.Count}`",
        $"- Aggregated GL accounts: `{aggregated.Count}`",
        string.Empty,
        "Category mapping heuristic:",
        "- `100-199` => `INCOME`",
        "- `200-299` => `EXPENSE`",
        "- `300-399` => `ASSET`",
        "- `400-499` => `LIABILITY`",
        "- `500-599` => `EQUITY`",
        "- fallback uses description keywords before defaulting to `ASSET`"
    });

    File.WriteAllText(Path.Combine(trialBalanceOutputDir, "tb_migration_summary.md"), summary);

    Console.WriteLine($"Trial balance prepared in: {trialBalanceOutputDir}");
    Console.WriteLine($"Branch-level rows: {details.Count}");
    Console.WriteLine($"Aggregated GL accounts: {aggregated.Count}");
    return;
}

var root = RepoPaths.FindRepoRoot();
var sourceBog = Path.Combine(root, "migration_work", "source_bog");
var sourceRe = Path.Combine(root, "migration_work", "source_re");
var outputDir = Path.Combine(root, "migration_work", "prepared");

Directory.CreateDirectory(outputDir);

var customerWorkbook = Path.Combine(sourceBog, "vwExp_bog_MAFI900_SingleCustomer_View-2026-03-31.xlsx");
var loanWorkbook = Path.Combine(sourceBog, "vwExp_bog_MAFI100_ActiveCreditContracts-2026-03-31.xlsx");
var additionalWorkbook = Path.Combine(sourceRe, "ADDITIONAL REQUIREMENTS_March2026.xlsx");
var chequeWorkbook = Path.Combine(sourceRe, "Cheque Nos20260331.xlsx");

var customerAccountRows = XlsxReader.ReadSheet(customerWorkbook, "Sheet1");
var loanRows = XlsxReader.ReadSheet(loanWorkbook, "Sheet1");
var depositRows = XlsxReader.ReadSheet(additionalWorkbook, "DEPOSIT");
var kycRows = XlsxReader.ReadSheet(additionalWorkbook, "KYC");
var loanExtraRows = XlsxReader.ReadSheet(additionalWorkbook, "LOANS");
var chequeRows = XlsxReader.ReadSheet(chequeWorkbook, "Cheque Nos");

var prepared = MigrationBuilder.Build(
    customerAccountRows,
    loanRows,
    depositRows,
    kycRows,
    loanExtraRows,
    chequeRows);

CsvWriter.Write(
    Path.Combine(outputDir, "customers.csv"),
    prepared.Customers,
    "id", "type", "name", "email", "phone", "secondary_phone", "digital_address", "postal_address", "kyc_level",
    "risk_rating", "gender", "date_of_birth", "ghana_card", "nationality", "marital_status", "spouse_name",
    "employer", "job_title", "ssnit_no", "business_reg_no", "registration_date", "tin", "sector", "legal_form",
    "branch_id", "created_at");

CsvWriter.Write(
    Path.Combine(outputDir, "products.csv"),
    prepared.Products.Values.OrderBy(x => x["id"]).ToList(),
    "id", "name", "type", "currency", "status", "interest_rate", "description", "interest_method",
    "min_amount", "max_amount", "min_term", "max_term", "default_term");

CsvWriter.Write(
    Path.Combine(outputDir, "accounts.csv"),
    prepared.Accounts,
    "id", "customer_id", "branch_id", "product_code", "type", "currency", "balance", "lien_amount",
    "status", "last_trans_date", "created_at");

CsvWriter.Write(
    Path.Combine(outputDir, "loans.csv"),
    prepared.Loans,
    "id", "customer_id", "group_id", "product_code", "loan_product_id", "principal", "rate", "term_months",
    "interest_method", "repayment_frequency", "schedule_type", "disbursement_date", "status", "application_date",
    "approved_at", "approved_by", "maker_id", "checker_id", "disbursed_at", "outstanding_balance", "collateral_type",
    "collateral_value", "servicing_account_id", "collateral_account_id", "par_bucket", "branch_id");

CsvWriter.Write(
    Path.Combine(outputDir, "gl_accounts_template.csv"),
    prepared.GlTemplate,
    "code", "name", "category", "currency", "balance", "is_header");

CsvWriter.Write(
    Path.Combine(outputDir, "customer_account_enrichment.csv"),
    prepared.CustomerEnrichment,
    "customer_id", "account_id", "sector", "occupation", "place_of_birth", "marital_status", "digital_address",
    "next_of_kin", "next_of_kin_relation", "next_of_kin_telephone", "next_of_kin_gender", "withdrawal_mandate",
    "proof_of_address", "risk_level", "risk_type", "source_of_funds", "other_bank", "other_bank_account_no");

CsvWriter.Write(
    Path.Combine(outputDir, "loan_balance_enrichment.csv"),
    prepared.LoanEnrichment,
    "loan_id", "customer_id", "interest_balance", "penalty_balance", "repayment_amount", "total_interest",
    "accrued_interest", "contract_status_id", "application_status_id", "payments_contract_number");

CsvWriter.Write(
    Path.Combine(outputDir, "cheque_inventory.csv"),
    prepared.ChequeInventory,
    "account_id", "cheque_no", "cheque_status", "used_flag", "date_used", "issued_by", "created_by", "first_number", "last_number", "range");

File.WriteAllText(
    Path.Combine(outputDir, "MIGRATION_MAPPING_SUMMARY.md"),
    SummaryBuilder.Build(prepared));

Console.WriteLine($"Prepared migration files in: {outputDir}");
Console.WriteLine($"Customers: {prepared.Customers.Count}");
Console.WriteLine($"Products: {prepared.Products.Count}");
Console.WriteLine($"Accounts: {prepared.Accounts.Count}");
Console.WriteLine($"Loans: {prepared.Loans.Count}");

static (string Code, string Currency) ParseAccountCode(string? raw)
{
    var value = (raw ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(value))
    {
        return (string.Empty, "GHS");
    }

    var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length > 1 && parts[^1].Length == 3)
    {
        return (parts[0].Trim(), parts[^1].Trim().ToUpperInvariant());
    }

    return (value, "GHS");
}

static decimal ParseDecimal(string? raw)
{
    return decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value)
        ? value
        : 0m;
}

static string MapTrialBalanceCategory(string code, string description)
{
    var firstSegment = code.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
    if (int.TryParse(firstSegment, out var majorCode))
    {
        if (majorCode is >= 100 and < 200)
        {
            return "INCOME";
        }

        if (majorCode is >= 200 and < 300)
        {
            return "EXPENSE";
        }

        if (majorCode is >= 300 and < 400)
        {
            return "ASSET";
        }

        if (majorCode is >= 400 and < 500)
        {
            return "LIABILITY";
        }

        if (majorCode is >= 500 and < 600)
        {
            return "EQUITY";
        }
    }

    var normalizedDescription = (description ?? string.Empty).Trim().ToUpperInvariant();
    if (normalizedDescription.Contains("INCOME") || normalizedDescription.Contains("COMM.") || normalizedDescription.Contains("INT."))
    {
        return "INCOME";
    }

    if (normalizedDescription.Contains("EXPENSE") || normalizedDescription.Contains("DEPRECIATION") || normalizedDescription.Contains("TAX"))
    {
        return "EXPENSE";
    }

    if (normalizedDescription.Contains("PAYABLE") || normalizedDescription.Contains("DEPOSIT") || normalizedDescription.Contains("BORROW"))
    {
        return "LIABILITY";
    }

    if (normalizedDescription.Contains("CAPITAL") || normalizedDescription.Contains("EQUITY") || normalizedDescription.Contains("RESERVE"))
    {
        return "EQUITY";
    }

    return "ASSET";
}
