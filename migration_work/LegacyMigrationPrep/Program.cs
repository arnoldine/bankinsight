using LegacyMigrationPrep;

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
