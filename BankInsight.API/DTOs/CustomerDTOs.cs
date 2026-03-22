using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is required")]
    [StringLength(50, ErrorMessage = "Type must not exceed 50 characters")]
    public string Type { get; set; } = "INDIVIDUAL";

    [RegularExpression(@"^[A-Z]{2}\d{8,}$", ErrorMessage = "Invalid GhanaCard format")]
    public string? GhanaCard { get; set; }

    [StringLength(50, ErrorMessage = "DigitalAddress must not exceed 50 characters")]
    public string? DigitalAddress { get; set; }

    [StringLength(20, ErrorMessage = "KycLevel must not exceed 20 characters")]
    public string? KycLevel { get; set; }

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "RiskRating must not exceed 50 characters")]
    public string? RiskRating { get; set; }
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
