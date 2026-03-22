using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    public string? RoleId { get; set; }
    public string? BranchId { get; set; }
    public string? AvatarInitials { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 255 characters")]
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string? Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    public string? RoleId { get; set; }
    public string? BranchId { get; set; }

    [StringLength(20, ErrorMessage = "Status must not exceed 20 characters")]
    public string? Status { get; set; }

    [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 255 characters")]
    public string? Password { get; set; }
}
