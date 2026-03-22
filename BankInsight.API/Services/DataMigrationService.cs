using System.Globalization;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class DataMigrationService
{
    private readonly ApplicationDbContext _context;

    public DataMigrationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MigrationImportResult> ImportAsync(string dataset, Stream csvStream)
    {
        var normalizedDataset = NormalizeDataset(dataset);
        var rows = ReadCsvRows(csvStream);

        return normalizedDataset switch
        {
            "customers" => await ImportCustomersAsync(rows),
            "products" => await ImportProductsAsync(rows),
            "accounts" => await ImportAccountsAsync(rows),
            "loans" => await ImportLoansAsync(rows),
            "gl_accounts" => await ImportGlAccountsAsync(rows),
            _ => throw new NotSupportedException($"Dataset '{dataset}' is not supported.")
        };
    }

    public IReadOnlyList<MigrationDatasetInfo> GetDatasets()
    {
        return new List<MigrationDatasetInfo>
        {
            new("customers", "Customers", "templates/customers_template.csv", "Migrate customer master records before account and loan data.", new[] { "id", "type", "name", "email", "phone", "branch_id", "kyc_level", "risk_rating", "created_at" }),
            new("products", "Products", "templates/products_template.csv", "Migrate deposit and lending products referenced by accounts and loans.", new[] { "id", "name", "type", "currency", "status", "interest_rate", "created_note" }),
            new("accounts", "Accounts", "templates/accounts_template.csv", "Migrate customer accounts after customers and products are loaded.", new[] { "id", "customer_id", "branch_id", "product_code", "type", "currency", "balance", "status", "created_at" }),
            new("loans", "Loans", "templates/loans_template.csv", "Migrate loan portfolio balances and metadata.", new[] { "id", "customer_id", "product_code", "principal", "rate", "term_months", "status", "application_date" }),
            new("gl_accounts", "GL Accounts", "templates/gl_accounts_template.csv", "Migrate chart of accounts and opening balances.", new[] { "code", "name", "category", "currency", "balance", "is_header" }),
        };
    }

    private static string NormalizeDataset(string dataset)
    {
        return dataset.Trim().ToLowerInvariant() switch
        {
            "customers" => "customers",
            "products" => "products",
            "accounts" => "accounts",
            "loans" => "loans",
            "gl-accounts" => "gl_accounts",
            "gl_accounts" => "gl_accounts",
            _ => dataset.Trim().ToLowerInvariant(),
        };
    }

    private static List<Dictionary<string, string>> ReadCsvRows(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<Dictionary<string, string>>();

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (csv.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header)?.Trim() ?? string.Empty;
            }
            rows.Add(row);
        }

        return rows;
    }

    private async Task<MigrationImportResult> ImportCustomersAsync(List<Dictionary<string, string>> rows)
    {
        var result = new MigrationImportResult("customers", rows.Count);

        foreach (var row in rows)
        {
            try
            {
                var id = Required(row, "id");
                var type = Required(row, "type");
                var name = Required(row, "name");

                var entity = await _context.Customers.FirstOrDefaultAsync(x => x.Id == id);
                var isNew = entity is null;
                entity ??= new Customer { Id = id };

                entity.Type = type;
                entity.Name = name;
                entity.Email = Optional(row, "email");
                entity.Phone = Optional(row, "phone");
                entity.SecondaryPhone = Optional(row, "secondary_phone");
                entity.DigitalAddress = Optional(row, "digital_address");
                entity.PostalAddress = Optional(row, "postal_address");
                entity.KycLevel = Optional(row, "kyc_level") ?? "Tier 1";
                entity.RiskRating = Optional(row, "risk_rating") ?? "Low";
                entity.Gender = Optional(row, "gender");
                entity.DateOfBirth = ParseDateOnly(Optional(row, "date_of_birth"));
                entity.GhanaCard = Optional(row, "ghana_card");
                entity.Nationality = Optional(row, "nationality");
                entity.MaritalStatus = Optional(row, "marital_status");
                entity.SpouseName = Optional(row, "spouse_name");
                entity.Employer = Optional(row, "employer");
                entity.JobTitle = Optional(row, "job_title");
                entity.SsnitNo = Optional(row, "ssnit_no");
                entity.BusinessRegNo = Optional(row, "business_reg_no");
                entity.RegistrationDate = ParseDateOnly(Optional(row, "registration_date"));
                entity.Tin = Optional(row, "tin");
                entity.Sector = Optional(row, "sector");
                entity.LegalForm = Optional(row, "legal_form");
                entity.BranchId = Optional(row, "branch_id") ?? "BR001";
                entity.CreatedAt = ParseDateTime(Optional(row, "created_at")) ?? DateTime.UtcNow;

                if (isNew)
                {
                    _context.Customers.Add(entity);
                    result.Imported++;
                }
                else
                {
                    result.Updated++;
                }
            }
            catch (Exception ex)
            {
                result.AddError(row, ex.Message);
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    private async Task<MigrationImportResult> ImportProductsAsync(List<Dictionary<string, string>> rows)
    {
        var result = new MigrationImportResult("products", rows.Count);

        foreach (var row in rows)
        {
            try
            {
                var id = Required(row, "id");
                var name = Required(row, "name");
                var type = Required(row, "type");

                var entity = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);
                var isNew = entity is null;
                entity ??= new Product { Id = id };

                entity.Name = name;
                entity.Type = type;
                entity.Description = Optional(row, "description");
                entity.Currency = Optional(row, "currency") ?? "GHS";
                entity.Status = Optional(row, "status") ?? "ACTIVE";
                entity.InterestRate = ParseDecimal(Optional(row, "interest_rate"));
                entity.InterestMethod = Optional(row, "interest_method");
                entity.MinAmount = ParseDecimal(Optional(row, "min_amount"));
                entity.MaxAmount = ParseDecimal(Optional(row, "max_amount"));
                entity.MinTerm = ParseInt(Optional(row, "min_term"));
                entity.MaxTerm = ParseInt(Optional(row, "max_term"));
                entity.DefaultTerm = ParseInt(Optional(row, "default_term"));

                if (isNew)
                {
                    _context.Products.Add(entity);
                    result.Imported++;
                }
                else
                {
                    result.Updated++;
                }
            }
            catch (Exception ex)
            {
                result.AddError(row, ex.Message);
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    private async Task<MigrationImportResult> ImportAccountsAsync(List<Dictionary<string, string>> rows)
    {
        var result = new MigrationImportResult("accounts", rows.Count);
        var customerIds = (await _context.Customers.Select(c => c.Id).ToListAsync()).ToHashSet();

        foreach (var row in rows)
        {
            try
            {
                var id = Required(row, "id");
                var customerId = Required(row, "customer_id");
                if (!customerIds.Contains(customerId))
                {
                    throw new InvalidOperationException($"Customer '{customerId}' does not exist.");
                }

                var entity = await _context.Accounts.FirstOrDefaultAsync(x => x.Id == id);
                var isNew = entity is null;
                entity ??= new Account { Id = id };

                entity.CustomerId = customerId;
                entity.BranchId = Optional(row, "branch_id");
                entity.ProductCode = Optional(row, "product_code");
                entity.Type = Required(row, "type");
                entity.Currency = Optional(row, "currency") ?? "GHS";
                entity.Balance = ParseDecimal(Optional(row, "balance")) ?? 0m;
                entity.LienAmount = ParseDecimal(Optional(row, "lien_amount")) ?? 0m;
                entity.Status = Optional(row, "status") ?? "ACTIVE";
                entity.LastTransDate = ParseDateTime(Optional(row, "last_trans_date"));
                entity.CreatedAt = ParseDateTime(Optional(row, "created_at")) ?? DateTime.UtcNow;

                if (isNew)
                {
                    _context.Accounts.Add(entity);
                    result.Imported++;
                }
                else
                {
                    result.Updated++;
                }
            }
            catch (Exception ex)
            {
                result.AddError(row, ex.Message);
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    private async Task<MigrationImportResult> ImportLoansAsync(List<Dictionary<string, string>> rows)
    {
        var result = new MigrationImportResult("loans", rows.Count);
        var customerIds = (await _context.Customers.Select(c => c.Id).ToListAsync()).ToHashSet();

        foreach (var row in rows)
        {
            try
            {
                var id = Required(row, "id");
                var customerId = Required(row, "customer_id");
                if (!customerIds.Contains(customerId))
                {
                    throw new InvalidOperationException($"Customer '{customerId}' does not exist.");
                }

                var entity = await _context.Loans.FirstOrDefaultAsync(x => x.Id == id);
                var isNew = entity is null;
                entity ??= new Loan { Id = id };

                entity.CustomerId = customerId;
                entity.GroupId = Optional(row, "group_id");
                entity.ProductCode = Optional(row, "product_code");
                entity.LoanProductId = Optional(row, "loan_product_id");
                entity.Principal = ParseDecimal(Required(row, "principal")) ?? throw new InvalidOperationException("Invalid principal value.");
                entity.Rate = ParseDecimal(Required(row, "rate")) ?? throw new InvalidOperationException("Invalid rate value.");
                entity.TermMonths = ParseInt(Required(row, "term_months")) ?? throw new InvalidOperationException("Invalid term_months value.");
                entity.InterestMethod = Optional(row, "interest_method") ?? "Flat";
                entity.RepaymentFrequency = Optional(row, "repayment_frequency") ?? "Monthly";
                entity.ScheduleType = Optional(row, "schedule_type") ?? "Monthly";
                entity.DisbursementDate = ParseDateOnly(Optional(row, "disbursement_date"));
                entity.Status = Optional(row, "status") ?? "PENDING";
                entity.ApplicationDate = ParseDateTime(Optional(row, "application_date")) ?? DateTime.UtcNow;
                entity.ApprovedAt = ParseDateTime(Optional(row, "approved_at"));
                entity.ApprovedBy = Optional(row, "approved_by");
                entity.MakerId = Optional(row, "maker_id");
                entity.CheckerId = Optional(row, "checker_id");
                entity.DisbursedAt = ParseDateTime(Optional(row, "disbursed_at"));
                entity.OutstandingBalance = ParseDecimal(Optional(row, "outstanding_balance"));
                entity.CollateralType = Optional(row, "collateral_type");
                entity.CollateralValue = ParseDecimal(Optional(row, "collateral_value"));
                entity.ParBucket = Optional(row, "par_bucket") ?? "0";
                entity.BranchId = Optional(row, "branch_id") ?? "BR001";

                if (isNew)
                {
                    _context.Loans.Add(entity);
                    result.Imported++;
                }
                else
                {
                    result.Updated++;
                }
            }
            catch (Exception ex)
            {
                result.AddError(row, ex.Message);
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    private async Task<MigrationImportResult> ImportGlAccountsAsync(List<Dictionary<string, string>> rows)
    {
        var result = new MigrationImportResult("gl_accounts", rows.Count);

        foreach (var row in rows)
        {
            try
            {
                var code = Required(row, "code");
                var name = Required(row, "name");
                var category = Required(row, "category");

                var entity = await _context.GlAccounts.FirstOrDefaultAsync(x => x.Code == code);
                var isNew = entity is null;
                entity ??= new GlAccount { Code = code };

                entity.Name = name;
                entity.Category = category;
                entity.Currency = Optional(row, "currency") ?? "GHS";
                entity.Balance = ParseDecimal(Optional(row, "balance")) ?? 0m;
                entity.IsHeader = ParseBool(Optional(row, "is_header")) ?? false;

                if (isNew)
                {
                    _context.GlAccounts.Add(entity);
                    result.Imported++;
                }
                else
                {
                    result.Updated++;
                }
            }
            catch (Exception ex)
            {
                result.AddError(row, ex.Message);
            }
        }

        await _context.SaveChangesAsync();
        return result;
    }

    private static string Required(Dictionary<string, string> row, string field)
    {
        if (!row.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Field '{field}' is required.");
        }

        return value.Trim();
    }

    private static string? Optional(Dictionary<string, string> row, string field)
    {
        if (!row.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid decimal value '{value}'.");
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid integer value '{value}'.");
    }

    private static bool? ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        if (value == "1")
        {
            return true;
        }

        if (value == "0")
        {
            return false;
        }

        throw new InvalidOperationException($"Invalid boolean value '{value}'.");
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid date value '{value}'. Use YYYY-MM-DD.");
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid date/time value '{value}'. Use ISO-8601 format.");
    }
}

public sealed record MigrationDatasetInfo(
    string Key,
    string Name,
    string TemplatePath,
    string Description,
    IReadOnlyList<string> RequiredColumns);

public sealed class MigrationImportResult
{
    public MigrationImportResult(string dataset, int totalRows)
    {
        Dataset = dataset;
        TotalRows = totalRows;
    }

    public string Dataset { get; }
    public int TotalRows { get; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; } = new();

    public void AddError(Dictionary<string, string> row, string message)
    {
        Failed++;
        var id = row.TryGetValue("id", out var value) && !string.IsNullOrWhiteSpace(value) ? value : "N/A";
        Errors.Add($"Row {Failed} (id={id}): {message}");
    }
}
