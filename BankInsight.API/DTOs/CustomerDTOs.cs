using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BankInsight.API.DTOs;

public class CreateCustomerRequest : IValidatableObject
{
    [StringLength(100, ErrorMessage = "FirstName must not exceed 100 characters")]
    public string? FirstName { get; set; }

    [StringLength(100, ErrorMessage = "LastName must not exceed 100 characters")]
    public string? LastName { get; set; }

    [StringLength(100, ErrorMessage = "OtherName must not exceed 100 characters")]
    public string? OtherName { get; set; }

    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [StringLength(50, ErrorMessage = "Type must not exceed 50 characters")]
    public string Type { get; set; } = "INDIVIDUAL";

    [RegularExpression(@"^[A-Z0-9-]{6,30}$", ErrorMessage = "Invalid Ghana Card format")]
    public string? GhanaCard { get; set; }

    [StringLength(50, ErrorMessage = "IdType must not exceed 50 characters")]
    public string? IdType { get; set; }

    [StringLength(100, ErrorMessage = "IdNumber must not exceed 100 characters")]
    public string? IdNumber { get; set; }

    [StringLength(50, ErrorMessage = "DigitalAddress must not exceed 50 characters")]
    public string? DigitalAddress { get; set; }

    [StringLength(255, ErrorMessage = "Address must not exceed 255 characters")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string? BranchId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(30, ErrorMessage = "Gender must not exceed 30 characters")]
    public string? Gender { get; set; }

    [StringLength(20, ErrorMessage = "KycLevel must not exceed 20 characters")]
    public string? KycLevel { get; set; }

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "RiskRating must not exceed 50 characters")]
    public string? RiskRating { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var resolvedName = string.IsNullOrWhiteSpace(Name)
            ? string.Join(" ", new[] { FirstName, OtherName, LastName }.Where(value => !string.IsNullOrWhiteSpace(value)))
            : Name;

        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            yield return new ValidationResult("Name is required", new[] { nameof(Name), nameof(FirstName), nameof(LastName) });
        }
        else if (resolvedName.Trim().Length < 2 || resolvedName.Trim().Length > 255)
        {
            yield return new ValidationResult("Name must be between 2 and 255 characters", new[] { nameof(Name) });
        }
    }
}

public class UpdateCustomerRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "DigitalAddress must not exceed 50 characters")]
    public string? DigitalAddress { get; set; }

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "RiskRating must not exceed 50 characters")]
    public string? RiskRating { get; set; }
}

public class CustomerNoteDto
{
    public string Id { get; set; } = string.Empty;
    public string Author { get; set; } = "System";
    public string Text { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Category { get; set; } = "GENERAL";
}

public class CustomerDocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string UploadDate { get; set; } = string.Empty;
}

public class CustomerProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "INDIVIDUAL";
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DigitalAddress { get; set; }
    public string? KycLevel { get; set; }
    public string? RiskRating { get; set; }
    public string? GhanaCard { get; set; }
    public string? Employer { get; set; }
    public string? MaritalStatus { get; set; }
    public string? SpouseName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? Tin { get; set; }
    public string? Sector { get; set; }
    public string? BusinessRegNo { get; set; }
    public string? CreatedAt { get; set; }
    public List<CustomerNoteDto> Notes { get; set; } = new();
    public List<CustomerDocumentDto> Documents { get; set; } = new();
    public List<CustomerMediaDto> MediaAssets { get; set; } = new();
    public CustomerMediaDto? ProfilePhoto { get; set; }
    public CustomerMediaDto? Signature { get; set; }
    public CustomerMediaDto? IdCardFront { get; set; }
    public CustomerMediaDto? IdCardBack { get; set; }
    public CustomerKycReadinessDto KycReadiness { get; set; } = new();
}

public class CreateCustomerNoteRequest
{
    [Required(ErrorMessage = "Note text is required")]
    [StringLength(1000, MinimumLength = 2, ErrorMessage = "Note text must be between 2 and 1000 characters")]
    public string Text { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Category must not exceed 30 characters")]
    public string? Category { get; set; } = "GENERAL";
}

public class CreateCustomerDocumentRequest
{
    [Required(ErrorMessage = "Document type is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Document type must be between 2 and 100 characters")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Document name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Document name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;
}

public class CustomerMediaDto
{
    public string Id { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string? MediaSide { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/png";
    public string PreviewUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public long? FileSizeBytes { get; set; }
    public string? UploadedBy { get; set; }
    public string UploadedAt { get; set; } = string.Empty;
}

public class UploadCustomerMediaRequest
{
    [Required(ErrorMessage = "MediaType is required")]
    [StringLength(30, ErrorMessage = "MediaType must not exceed 30 characters")]
    public string MediaType { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "MediaSide must not exceed 10 characters")]
    public string? MediaSide { get; set; }

    [Required(ErrorMessage = "FileName is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "FileName must be between 2 and 255 characters")]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ContentType is required")]
    [StringLength(100, ErrorMessage = "ContentType must not exceed 100 characters")]
    public string ContentType { get; set; } = "image/png";

    [Required(ErrorMessage = "Image data is required")]
    public string DataUrl { get; set; } = string.Empty;
}

public class CustomerKycStatusResponse
{
    public string CustomerId { get; set; } = string.Empty;
    public string KycLevel { get; set; } = "TIER1";
    public decimal TransactionLimit { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal RemainingDailyLimit { get; set; }
    public bool IsUnlimited { get; set; }
    public bool GhanaCardMatchesProfile { get; set; }
    public decimal TodayPostedTotal { get; set; }
    public CustomerKycReadinessDto? Readiness { get; set; }
}

public class CustomerKycChecklistItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public class CustomerKycReadinessDto
{
    public bool IsReadyForAccountOpening { get; set; }
    public bool IsReadyForLoanOrigination { get; set; }
    public List<string> MissingRequirements { get; set; } = new();
    public List<CustomerKycChecklistItemDto> Checklist { get; set; } = new();
}

public class ValidateGhanaCardRequest
{
    [Required(ErrorMessage = "CustomerId is required")]
    public string CustomerId { get; set; } = string.Empty;

    [Required(ErrorMessage = "GhanaCardNumber is required")]
    public string GhanaCardNumber { get; set; } = string.Empty;
}

public class ValidateGhanaCardResponse
{
    public bool IsValid { get; set; }
}
