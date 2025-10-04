namespace ImageViewer.Application.Services;

/// <summary>
/// Interface for password hashing and verification operations
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hash a password using BCrypt
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">Hashed password</param>
    /// <returns>True if password matches hash</returns>
    bool VerifyPassword(string password, string hashedPassword);

    /// <summary>
    /// Check if password meets strength requirements
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>True if password is strong enough</returns>
    bool IsStrongPassword(string password);
}
