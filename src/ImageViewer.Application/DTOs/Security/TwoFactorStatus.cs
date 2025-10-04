namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Two-factor authentication status DTO
/// </summary>
public class TwoFactorStatus
{
    /// <summary>
    /// Is two-factor authentication enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Setup date
    /// </summary>
    public DateTime? SetupDate { get; set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime? LastUsedDate { get; set; }

    /// <summary>
    /// Number of backup codes remaining
    /// </summary>
    public int BackupCodesRemaining { get; set; }
}
