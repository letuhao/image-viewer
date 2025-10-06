namespace ImageViewer.Application.DTOs.Notifications;

/// <summary>
/// Result DTO for template variable validation
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// Whether the template variables are valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of missing variables that are required by the template
    /// </summary>
    public List<string> MissingVariables { get; set; } = new List<string>();

    /// <summary>
    /// List of extra variables that are not used by the template
    /// </summary>
    public List<string> ExtraVariables { get; set; } = new List<string>();

    /// <summary>
    /// List of all variables found in the template
    /// </summary>
    public List<string> TemplateVariables { get; set; } = new List<string>();

    /// <summary>
    /// List of all variables provided for validation
    /// </summary>
    public List<string> ProvidedVariables { get; set; } = new List<string>();
}
