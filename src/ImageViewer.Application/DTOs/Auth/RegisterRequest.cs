using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Auth;

/// <summary>
/// User registration request DTO
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Username
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and hyphens")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [StringLength(50)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    [StringLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// Whether to accept terms and conditions
    /// </summary>
    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
    public bool AcceptTerms { get; set; }

    /// <summary>
    /// IP address of the registration attempt
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the registration attempt
    /// </summary>
    public string? UserAgent { get; set; }
}
