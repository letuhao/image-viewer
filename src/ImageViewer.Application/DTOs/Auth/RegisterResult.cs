namespace ImageViewer.Application.DTOs.Auth;

/// <summary>
/// User registration result DTO
/// </summary>
public class RegisterResult
{
    /// <summary>
    /// Whether registration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User ID if registration was successful
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Whether email verification is required
    /// </summary>
    public bool RequiresEmailVerification { get; set; }

    /// <summary>
    /// Verification token if email verification is required
    /// </summary>
    public string? VerificationToken { get; set; }

    /// <summary>
    /// Error message if registration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
