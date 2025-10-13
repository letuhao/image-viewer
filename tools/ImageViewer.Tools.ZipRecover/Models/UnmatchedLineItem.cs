using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace ImageViewer.Tools.ZipRecover.Models;

/// <summary>
/// Represents a line that didn't match the regex pattern
/// </summary>
public partial class UnmatchedLineItem : ObservableObject
{
    [ObservableProperty]
    private string _line = string.Empty;

    [ObservableProperty]
    private string _extractedPath = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _status = "Unmatched";

    /// <summary>
    /// Whether the extracted path exists on disk
    /// </summary>
    public bool IsValidFile => !string.IsNullOrEmpty(ExtractedPath) && File.Exists(ExtractedPath);
}
