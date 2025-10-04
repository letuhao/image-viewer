using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs.Auth;

/// <summary>
/// Change password request DTO
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password confirmation
    /// </summary>
    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
