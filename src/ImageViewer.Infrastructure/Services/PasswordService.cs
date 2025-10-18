using ImageViewer.Application.Services;
using System.Text;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Password service implementation using BCrypt
/// </summary>
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    /// <summary>
    /// Hash password using BCrypt with work factor 12
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    /// <exception cref="ArgumentException">Thrown when password is null or empty</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (password.Length > MaxPasswordLength)
            throw new ArgumentException($"Password cannot exceed {MaxPasswordLength} characters", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">Hashed password to verify against</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Check if password meets strength requirements
    /// </summary>
    /// <param name="password">Password to check</param>
    /// <returns>True if password is strong, false otherwise</returns>
    public bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            return false;

        // Check for uppercase letter
        if (!password.Any(char.IsUpper))
            return false;

        // Check for lowercase letter
        if (!password.Any(char.IsLower))
            return false;

        // Check for digit
        if (!password.Any(char.IsDigit))
            return false;

        // Check for special character
        if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
            return false;

        return true;
    }

    /// <summary>
    /// Get password strength score (0-100)
    /// </summary>
    /// <param name="password">Password to analyze</param>
    /// <returns>Password strength score</returns>
    public int GetPasswordStrengthScore(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        int score = 0;

        // Length score (0-40 points)
        if (password.Length >= MinPasswordLength)
            score += 20;
        if (password.Length >= 12)
            score += 10;
        if (password.Length >= 16)
            score += 10;

        // Character variety score (0-40 points)
        if (password.Any(char.IsLower))
            score += 10;
        if (password.Any(char.IsUpper))
            score += 10;
        if (password.Any(char.IsDigit))
            score += 10;
        if (password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
            score += 10;

        // Pattern penalty (0-20 points deducted)
        if (HasRepeatingCharacters(password))
            score -= 5;
        if (HasSequentialCharacters(password))
            score -= 5;
        if (HasCommonPatterns(password))
            score -= 10;

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Generate random password
    /// </summary>
    /// <param name="length">Password length</param>
    /// <param name="includeSpecialChars">Include special characters</param>
    /// <returns>Generated password</returns>
    public string GenerateRandomPassword(int length = 12, bool includeSpecialChars = true)
    {
        if (length < MinPasswordLength || length > MaxPasswordLength)
            throw new ArgumentException($"Password length must be between {MinPasswordLength} and {MaxPasswordLength}", nameof(length));

        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var chars = lowercase + uppercase + digits;
        if (includeSpecialChars)
            chars += special;

        var password = new StringBuilder(length);
        var random = new Random();

        // Ensure at least one character from each required category
        password.Append(lowercase[random.Next(lowercase.Length)]);
        password.Append(uppercase[random.Next(uppercase.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        if (includeSpecialChars)
            password.Append(special[random.Next(special.Length)]);

        // Fill the rest with random characters
        for (int i = password.Length; i < length; i++)
        {
            password.Append(chars[random.Next(chars.Length)]);
        }

        // Shuffle the password
        var shuffled = password.ToString().ToCharArray();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return new string(shuffled);
    }

    private static bool HasRepeatingCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i] == password[i + 2])
                return true;
        }
        return false;
    }

    private static bool HasSequentialCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (char.IsLetter(password[i]) && char.IsLetter(password[i + 1]) && char.IsLetter(password[i + 2]))
            {
                if (password[i] + 1 == password[i + 1] && password[i + 1] + 1 == password[i + 2])
                    return true;
            }
        }
        return false;
    }

    private static bool HasCommonPatterns(string password)
    {
        var commonPatterns = new[]
        {
            "123", "abc", "qwe", "asd", "zxc", "password", "admin", "user", "test"
        };

        var lowerPassword = password.ToLower();
        return commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
    }
}
