namespace LegacyMigrationPrep;

using static ValueHelpers;

internal static class MigrationBuilder
{
    public static PreparedMigration Build(
        List<Dictionary<string, string>> customerAccountRows,
        List<Dictionary<string, string>> loanRows,
        List<Dictionary<string, string>> depositRows,
        List<Dictionary<string, string>> kycRows,
        List<Dictionary<string, string>> loanExtraRows,
        List<Dictionary<string, string>> chequeRows)
    {
        var depositByAccount = depositRows
            .Where(row => !string.IsNullOrWhiteSpace(Get(row, "ACCOUNT NUMBER")))
            .GroupBy(row => Get(row, "ACCOUNT NUMBER"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var kycByAccount = kycRows
            .Where(row => !string.IsNullOrWhiteSpace(Get(row, "ACCOUNT NUMBER")))
            .GroupBy(row => Get(row, "ACCOUNT NUMBER"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var loanExtraByContract = loanExtraRows
            .Where(row => !string.IsNullOrWhiteSpace(Get(row, "ContractNumber")))
            .GroupBy(row => Get(row, "ContractNumber"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var products = BuildProducts(customerAccountRows, loanRows);
        var customers = BuildCustomers(customerAccountRows, depositByAccount, kycByAccount);
        var accounts = BuildAccounts(customerAccountRows, depositByAccount, products);
        var loans = BuildLoans(loanRows, loanExtraByContract, products);
        var glTemplate = new List<Dictionary<string, string>>
        {
            NewRecord(("code", string.Empty), ("name", string.Empty), ("category", string.Empty), ("currency", "GHS"), ("balance", "0"), ("is_header", "false"))
        };

        return new PreparedMigration
        {
            Products = products,
            Customers = customers,
            Accounts = accounts,
            Loans = loans,
            GlTemplate = glTemplate,
            CustomerEnrichment = BuildCustomerEnrichment(customerAccountRows, depositByAccount, kycByAccount),
            LoanEnrichment = BuildLoanEnrichment(loanRows, loanExtraByContract),
            ChequeInventory = BuildChequeInventory(chequeRows)
        };
    }

    private static Dictionary<string, Dictionary<string, string>> BuildProducts(
        List<Dictionary<string, string>> customerAccountRows,
        List<Dictionary<string, string>> loanRows)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in customerAccountRows)
        {
            var productName = Get(row, "Product Name");
            if (string.IsNullOrWhiteSpace(productName)) continue;
            var productId = BuildStableCode("PRD", productName);
            if (result.ContainsKey(productId)) continue;

            result[productId] = NewRecord(
                ("id", productId), ("name", productName), ("type", "DEPOSIT"),
                ("currency", NormalizeCurrency(Get(row, "Currency Of Account"))), ("status", "ACTIVE"),
                ("interest_rate", string.Empty), ("description", $"Migrated from legacy account product '{productName}'."),
                ("interest_method", string.Empty), ("min_amount", string.Empty), ("max_amount", string.Empty),
                ("min_term", string.Empty), ("max_term", string.Empty), ("default_term", string.Empty));
        }

        foreach (var row in loanRows)
        {
            var productName = Get(row, "Loan Product");
            if (string.IsNullOrWhiteSpace(productName)) continue;
            var productId = BuildStableCode("LNP", productName);
            if (result.ContainsKey(productId)) continue;

            result[productId] = NewRecord(
                ("id", productId), ("name", productName), ("type", "LOAN"),
                ("currency", NormalizeCurrency(Get(row, "Currency"))), ("status", "ACTIVE"),
                ("interest_rate", NormalizeDecimal(Get(row, "Interest Rate (% P.A)"))),
                ("description", $"Migrated from legacy loan product '{productName}'."),
                ("interest_method", NormalizeInterestMethod(Get(row, "Interest Charge Type"))),
                ("min_amount", string.Empty), ("max_amount", string.Empty),
                ("min_term", string.Empty), ("max_term", string.Empty),
                ("default_term", NormalizeInteger(Get(row, "Number of Payments Agreed"))));
        }

        return result;
    }

    private static List<Dictionary<string, string>> BuildCustomers(
        List<Dictionary<string, string>> customerAccountRows,
        IReadOnlyDictionary<string, Dictionary<string, string>> depositByAccount,
        IReadOnlyDictionary<string, Dictionary<string, string>> kycByAccount)
    {
        var grouped = customerAccountRows
            .Where(row => !string.IsNullOrWhiteSpace(Get(row, "Bank Specific CIN")))
            .GroupBy(row => Get(row, "Bank Specific CIN"), StringComparer.OrdinalIgnoreCase);

        var results = new List<Dictionary<string, string>>();

        foreach (var group in grouped)
        {
            var first = group.First();
            var accountIds = group.Select(row => Get(row, "Account Number")).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var depositMatches = accountIds.Where(depositByAccount.ContainsKey).Select(id => depositByAccount[id]).ToList();
            var kycMatches = accountIds.Where(kycByAccount.ContainsKey).Select(id => kycByAccount[id]).ToList();

            var customerType = NormalizeCustomerType(Get(first, "Customer Type"));
            var name = customerType == "Corporate"
                ? FirstNonEmpty(group.Select(row => Get(row, "Company Name"))) ?? group.Key
                : JoinName(
                    FirstNonEmpty(group.Select(row => Get(row, "First Name"))),
                    FirstNonEmpty(group.Select(row => Get(row, "Middle Name"))),
                    FirstNonEmpty(group.Select(row => Get(row, "Surname"))));

            var pep = group.Any(row => NormalizeYesNo(Get(row, "Politically Exposed Person")) == "YES");
            var risk = pep ? "High" : NormalizeRiskLevel(FirstNonEmpty(kycMatches.Select(row => Get(row, "RISK LEVEL"))));
            var createdAt = FirstNonEmpty(depositMatches.Select(row => NormalizeDateTime(Get(row, "OPEN DATE")))) ?? "2026-03-31T00:00:00Z";
            var primaryPhone = NormalizePhone(
                FirstNonEmpty(group.Select(row => Get(row, "Mobile Phone Number"))) ??
                FirstNonEmpty(group.Select(row => Get(row, "Main Phone Number"))) ??
                FirstNonEmpty(group.Select(row => Get(row, "Mobile Money Number"))));
            var secondaryPhone = DistinctSecondaryPhone(
                primaryPhone,
                FirstNonEmpty(group.Select(row => Get(row, "Main Phone Number"))) ??
                FirstNonEmpty(group.Select(row => Get(row, "Mobile Money Number"))));

            results.Add(NewRecord(
                ("id", SafeTruncate(group.Key, 50)),
                ("type", SafeTruncate(customerType, 20)),
                ("name", string.IsNullOrWhiteSpace(name) ? group.Key : name),
                ("email", FirstNonEmpty(group.Select(row => Get(row, "Email"))) ?? string.Empty),
                ("phone", SafeTruncate(primaryPhone, 20)),
                ("secondary_phone", SafeTruncate(secondaryPhone, 20)),
                ("digital_address", FirstNonEmpty(depositMatches.Select(row => Get(row, "DIGITAL ADDRESS"))) ?? string.Empty),
                ("postal_address", CombineAddress(
                    FirstNonEmpty(group.Select(row => Get(row, "Home Address"))),
                    FirstNonEmpty(group.Select(row => Get(row, "Postal Address"))))),
                ("kyc_level", SafeTruncate(string.IsNullOrWhiteSpace(FirstNonEmpty(group.Select(row => Get(row, "ID Number")))) ? "Tier 1" : "Tier 2", 20)),
                ("risk_rating", SafeTruncate(risk, 20)),
                ("gender", SafeTruncate(NormalizeGender(FirstNonEmpty(group.Select(row => Get(row, "Gender")))), 10)),
                ("date_of_birth", NormalizeDateOnly(FirstNonEmpty(group.Select(row => Get(row, "DOB"))) ?? FirstNonEmpty(group.Select(row => Get(row, "Date of Birth"))))),
                ("ghana_card", SafeTruncate(IsGhanaCard(FirstNonEmpty(group.Select(row => Get(row, "ID Type")))) ? FirstNonEmpty(group.Select(row => Get(row, "ID Number"))) ?? string.Empty : string.Empty, 50)),
                ("nationality", SafeTruncate(FirstNonEmpty(group.Select(row => Get(row, "Country"))) ?? "Ghana", 50)),
                ("marital_status", SafeTruncate(FirstNonEmpty(depositMatches.Select(row => Get(row, "MARITAL STATUS"))) ?? string.Empty, 20)),
                ("spouse_name", string.Empty),
                ("employer", string.Empty),
                ("job_title", FirstNonEmpty(depositMatches.Select(row => Get(row, "OCCUPATION"))) ?? string.Empty),
                ("ssnit_no", string.Empty),
                ("business_reg_no", SafeTruncate(customerType == "Corporate" ? FirstNonEmpty(group.Select(row => Get(row, "Company Number (If Any)"))) ?? string.Empty : string.Empty, 50)),
                ("registration_date", string.Empty),
                ("tin", SafeTruncate(FirstNonEmpty(group.Select(row => Get(row, "TIN Number"))) ?? string.Empty, 50)),
                ("sector", SafeTruncate(FirstNonEmpty(depositMatches.Select(row => Get(row, "SECTOR"))) ?? FirstNonEmpty(group.Select(row => Get(row, "Economic Sector"))) ?? string.Empty, 50)),
                ("legal_form", SafeTruncate(customerType == "Corporate" ? FirstNonEmpty(group.Select(row => Get(row, "Institution Type"))) ?? string.Empty : string.Empty, 50)),
                ("branch_id", SafeTruncate(NormalizeBranch(FirstNonEmpty(group.Select(row => Get(row, "Account Branch"))) ?? FirstNonEmpty(group.Select(row => Get(row, "Branch Number/Code"))) ?? FirstNonEmpty(group.Select(row => Get(row, "Branch Name")))), 50)),
                ("created_at", createdAt)));
        }

        return results.OrderBy(row => row["id"], StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<Dictionary<string, string>> BuildAccounts(
        List<Dictionary<string, string>> customerAccountRows,
        IReadOnlyDictionary<string, Dictionary<string, string>> depositByAccount,
        IReadOnlyDictionary<string, Dictionary<string, string>> productCatalog)
    {
        var results = new List<Dictionary<string, string>>();
        foreach (var row in customerAccountRows)
        {
            var accountId = Get(row, "Account Number");
            if (!LooksValidId(accountId, 6)) continue;

            depositByAccount.TryGetValue(accountId, out var depositRow);
            var productName = Get(row, "Product Name");
            var productId = string.IsNullOrWhiteSpace(productName) ? string.Empty : BuildStableCode("PRD", productName);

            results.Add(NewRecord(
                ("id", accountId),
                ("customer_id", Get(row, "Bank Specific CIN")),
                ("branch_id", NormalizeBranch(Get(row, "Account Branch"))),
                ("product_code", productCatalog.ContainsKey(productId) ? productId : string.Empty),
                ("type", NormalizeAccountType(Get(row, "Account Type"), productName)),
                ("currency", NormalizeCurrency(Get(row, "Currency Of Account"))),
                ("balance", FirstNonEmpty(new[] { NormalizeDecimal(Get(row, "Account Balance In Original Currency")), NormalizeDecimal(Get(row, "Account Balance")), NormalizeDecimal(Get(row, "Account Balance In Cedis")) }) ?? "0"),
                ("lien_amount", "0"),
                ("status", NormalizeAccountStatus(Get(row, "Status Of Account"))),
                ("last_trans_date", string.Empty),
                ("created_at", depositRow is null ? "2026-03-31T00:00:00Z" : NormalizeDateTime(Get(depositRow, "OPEN DATE")))));
        }

        return results.OrderBy(row => row["id"], StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<Dictionary<string, string>> BuildLoans(
        List<Dictionary<string, string>> loanRows,
        IReadOnlyDictionary<string, Dictionary<string, string>> loanExtraByContract,
        IReadOnlyDictionary<string, Dictionary<string, string>> productCatalog)
    {
        var results = new List<Dictionary<string, string>>();
        foreach (var row in loanRows)
        {
            var loanId = FirstNonEmpty(new[] { Get(row, "ContractNumber"), Get(row, "Account Number") });
            if (string.IsNullOrWhiteSpace(loanId)) continue;

            loanExtraByContract.TryGetValue(loanId, out var extra);
            var productName = Get(row, "Loan Product");
            var productId = string.IsNullOrWhiteSpace(productName) ? string.Empty : BuildStableCode("LNP", productName);
            var principal = FirstNonEmpty(new[] { NormalizeDecimal(Get(row, "Loan Amount Disbursed")), NormalizeDecimal(Get(row, "Loan Amount Approved")), NormalizeDecimal(extra is null ? string.Empty : Get(extra, "RepaymentAmount")) }) ?? "0";
            var outstandingBalance = FirstNonEmpty(new[] { NormalizeDecimal(Get(row, "Loan Principal Balance (Without Interest)")), principal }) ?? "0";

            results.Add(NewRecord(
                ("id", loanId), ("customer_id", Get(row, "Customer ID")), ("group_id", string.Empty),
                ("product_code", productCatalog.ContainsKey(productId) ? productId : string.Empty), ("loan_product_id", string.Empty),
                ("principal", principal), ("rate", NormalizeDecimal(Get(row, "Interest Rate (% P.A)"))),
                ("term_months", NormalizeLoanTermMonths(Get(row, "Number of Payments Agreed"), Get(row, "Frequency of Payments"))),
                ("interest_method", NormalizeInterestMethod(Get(row, "Interest Charge Type"))),
                ("repayment_frequency", NormalizeRepaymentFrequency(Get(row, "Frequency of Payments"))),
                ("schedule_type", NormalizeRepaymentFrequency(Get(row, "Frequency of Payments"))),
                ("disbursement_date", NormalizeDateOnly(Get(row, "Loan Disbursement Date"))),
                ("status", NormalizeLoanStatus(Get(row, "ContractStatusId"), outstandingBalance, Get(row, "Loan Disbursement Date"))),
                ("application_date", FirstNonEmpty(new[] { NormalizeDateTime(Get(row, "Date of Approval")), NormalizeDateTime(Get(row, "Loan Disbursement Date")) }) ?? "2026-03-31T00:00:00Z"),
                ("approved_at", NormalizeDateTime(Get(row, "Date of Approval"))),
                ("approved_by", SafeTruncate(Get(row, "Branch Manager"), 50)),
                ("maker_id", SafeTruncate(Get(row, "Loan Officer"), 50)),
                ("checker_id", SafeTruncate(Get(row, "Branch Manager"), 50)),
                ("disbursed_at", NormalizeDateTime(Get(row, "Loan Disbursement Date"))),
                ("outstanding_balance", outstandingBalance),
                ("collateral_type", SafeTruncate(FirstNonEmpty(new[] { Get(row, "Type of Security"), Get(row, "Description of Security") }) ?? string.Empty, 50)),
                ("collateral_value", FirstNonEmpty(new[] { NormalizeDecimal(Get(row, "Value of Allowable Security")), NormalizeDecimal(Get(row, "Value of Security")), NormalizeDecimal(Get(row, "Force Sale Value of Security")) }) ?? string.Empty),
                ("servicing_account_id", extra is null ? string.Empty : Get(extra, "PaymentsContractNumber")),
                ("collateral_account_id", string.Empty),
                ("par_bucket", NormalizeParBucket(Get(row, "Days in Arrears"))),
                ("branch_id", NormalizeBranch(FirstNonEmpty(new[] { Get(row, "Branch Number/Code"), Get(row, "Branch Name") })))));
        }

        return results.OrderBy(row => row["id"], StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<Dictionary<string, string>> BuildCustomerEnrichment(
        List<Dictionary<string, string>> customerAccountRows,
        IReadOnlyDictionary<string, Dictionary<string, string>> depositByAccount,
        IReadOnlyDictionary<string, Dictionary<string, string>> kycByAccount)
    {
        var rows = new List<Dictionary<string, string>>();
        foreach (var row in customerAccountRows)
        {
            var accountId = Get(row, "Account Number");
            if (string.IsNullOrWhiteSpace(accountId)) continue;

            depositByAccount.TryGetValue(accountId, out var deposit);
            kycByAccount.TryGetValue(accountId, out var kyc);

            rows.Add(NewRecord(
                ("customer_id", Get(row, "Bank Specific CIN")), ("account_id", accountId),
                ("sector", deposit is null ? string.Empty : Get(deposit, "SECTOR")),
                ("occupation", deposit is null ? string.Empty : Get(deposit, "OCCUPATION")),
                ("place_of_birth", deposit is null ? string.Empty : Get(deposit, "PLACE OF BIRTH")),
                ("marital_status", deposit is null ? string.Empty : Get(deposit, "MARITAL STATUS")),
                ("digital_address", deposit is null ? string.Empty : Get(deposit, "DIGITAL ADDRESS")),
                ("next_of_kin", deposit is null ? string.Empty : Get(deposit, "NEXT OF KIN")),
                ("next_of_kin_relation", deposit is null ? string.Empty : Get(deposit, "NEXT OF KIN RELATION")),
                ("next_of_kin_telephone", deposit is null ? string.Empty : Get(deposit, "NEXT OF KIN TELEPHONE")),
                ("next_of_kin_gender", deposit is null ? string.Empty : Get(deposit, "NEXT OF KIN GENDER")),
                ("withdrawal_mandate", deposit is null ? string.Empty : Get(deposit, "WITHDRAWAL MANDATE")),
                ("proof_of_address", kyc is null ? string.Empty : Get(kyc, "PROOF OF ADDRESS")),
                ("risk_level", kyc is null ? string.Empty : Get(kyc, "RISK LEVEL")),
                ("risk_type", kyc is null ? string.Empty : Get(kyc, "RISK TYPE")),
                ("source_of_funds", kyc is null ? string.Empty : Get(kyc, "Source of Funds")),
                ("other_bank", kyc is null ? string.Empty : Get(kyc, "OTHER BANK")),
                ("other_bank_account_no", kyc is null ? string.Empty : Get(kyc, "OTHER BANK ACCOUNT NO"))));
        }

        return rows.OrderBy(x => x["account_id"], StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<Dictionary<string, string>> BuildLoanEnrichment(
        List<Dictionary<string, string>> loanRows,
        IReadOnlyDictionary<string, Dictionary<string, string>> loanExtraByContract)
    {
        var rows = new List<Dictionary<string, string>>();
        foreach (var row in loanRows)
        {
            var loanId = FirstNonEmpty(new[] { Get(row, "ContractNumber"), Get(row, "Account Number") });
            if (string.IsNullOrWhiteSpace(loanId) || !loanExtraByContract.TryGetValue(loanId, out var extra)) continue;

            rows.Add(NewRecord(
                ("loan_id", loanId), ("customer_id", Get(row, "Customer ID")),
                ("interest_balance", NormalizeDecimal(Get(extra, "INTEREST BALANCE"))),
                ("penalty_balance", NormalizeDecimal(Get(extra, "PENALTY BALANCE"))),
                ("repayment_amount", NormalizeDecimal(Get(extra, "RepaymentAmount"))),
                ("total_interest", NormalizeDecimal(Get(extra, "TotalInterest"))),
                ("accrued_interest", NormalizeDecimal(Get(extra, "AccruedInterest"))),
                ("contract_status_id", Get(extra, "ContractStatusId")),
                ("application_status_id", Get(extra, "ApplicationStatusId")),
                ("payments_contract_number", Get(extra, "PaymentsContractNumber"))));
        }

        return rows.OrderBy(x => x["loan_id"], StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<Dictionary<string, string>> BuildChequeInventory(List<Dictionary<string, string>> chequeRows)
    {
        return chequeRows
            .Where(row => !string.IsNullOrWhiteSpace(Get(row, "Account")) && !string.IsNullOrWhiteSpace(Get(row, "Cheque No")))
            .Select(row => NewRecord(
                ("account_id", Get(row, "Account")),
                ("cheque_no", Get(row, "Cheque No")),
                ("cheque_status", Get(row, "ChequeStatus")),
                ("used_flag", NormalizeYesNo(Get(row, "Used(Yes/No)"))),
                ("date_used", NormalizeDateOnly(Get(row, "DateUsed"))),
                ("issued_by", Get(row, "Issued By")),
                ("created_by", Get(row, "Created By")),
                ("first_number", Get(row, "FirstNumber")),
                ("last_number", Get(row, "LastNumber")),
                ("range", Get(row, "Range"))))
            .OrderBy(x => x["account_id"], StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x["cheque_no"], StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
