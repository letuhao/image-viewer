namespace ImageViewer.Domain.Enums;

/// <summary>
/// Security alert types
/// </summary>
public enum SecurityAlertType
{
    /// <summary>
    /// Login from new location
    /// </summary>
    NewLocationLogin,

    /// <summary>
    /// Suspicious activity detected
    /// </summary>
    SuspiciousActivity,

    /// <summary>
    /// Multiple failed login attempts
    /// </summary>
    FailedLoginAttempts,

    /// <summary>
    /// Unauthorized access attempt
    /// </summary>
    UnauthorizedAccess,

    /// <summary>
    /// Account lockout
    /// </summary>
    AccountLockout,

    /// <summary>
    /// Password change
    /// </summary>
    PasswordChange,

    /// <summary>
    /// Two-factor authentication enabled/disabled
    /// </summary>
    TwoFactorChange,

    /// <summary>
    /// Device registration
    /// </summary>
    DeviceRegistration,

    /// <summary>
    /// Session termination
    /// </summary>
    SessionTermination,

    /// <summary>
    /// Data breach notification
    /// </summary>
    DataBreach,

    /// <summary>
    /// System security update
    /// </summary>
    SecurityUpdate,

    /// <summary>
    /// Custom security alert
    /// </summary>
    Custom
}
