namespace ImageViewer.Application.DTOs.Security;

/// <summary>
/// Security metrics DTO
/// </summary>
public class SecurityMetrics
{
    /// <summary>
    /// Total login attempts
    /// </summary>
    public long TotalLoginAttempts { get; set; }

    /// <summary>
    /// Successful logins
    /// </summary>
    public long SuccessfulLogins { get; set; }

    /// <summary>
    /// Failed logins
    /// </summary>
    public long FailedLogins { get; set; }

    /// <summary>
    /// Security alerts
    /// </summary>
    public long SecurityAlerts { get; set; }

    /// <summary>
    /// Two-factor authentications
    /// </summary>
    public long TwoFactorAuthentications { get; set; }

    /// <summary>
    /// Device registrations
    /// </summary>
    public long DeviceRegistrations { get; set; }

    /// <summary>
    /// Risk assessments
    /// </summary>
    public long RiskAssessments { get; set; }

    /// <summary>
    /// Period start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Period end date
    /// </summary>
    public DateTime? EndDate { get; set; }
}
