namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Two-factor authentication setup result DTO
/// </summary>
public class TwoFactorSetupResult
{
    /// <summary>
    /// QR code URL for setup
    /// </summary>
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Manual setup key
    /// </summary>
    public string ManualSetupKey { get; set; } = string.Empty;

    /// <summary>
    /// Backup codes
    /// </summary>
    public List<string> BackupCodes { get; set; } = new();
}
