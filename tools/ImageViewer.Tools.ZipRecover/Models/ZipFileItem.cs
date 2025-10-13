using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageViewer.Tools.ZipRecover.Models;

/// <summary>
/// Represents a ZIP file item in the processing list
/// </summary>
public partial class ZipFileItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private ZipFileStatus _status = ZipFileStatus.Pending;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private int _validFilesCount;

    [ObservableProperty]
    private int _totalFilesCount;

    [ObservableProperty]
    private long _originalSize;

    [ObservableProperty]
    private long _recoveredSize;

    [ObservableProperty]
    private string _archiveType = string.Empty;

    [ObservableProperty]
    private string _healthStatus = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _currentStep = "Ready";

    [ObservableProperty]
    private double _fileProgressPercentage;

    [ObservableProperty]
    private string _processingDetails = string.Empty;

    public string StatusText => Status switch
    {
        ZipFileStatus.Pending => "Pending",
        ZipFileStatus.Processing => "Processing...",
        ZipFileStatus.Success => "✅ Success",
        ZipFileStatus.Failed => "❌ Failed",
        ZipFileStatus.Skipped => "⏭️ Skipped",
        _ => "Unknown"
    };

    public string StatusColor => Status switch
    {
        ZipFileStatus.Pending => "Gray",
        ZipFileStatus.Processing => "Blue",
        ZipFileStatus.Success => "Green",
        ZipFileStatus.Failed => "Red",
        ZipFileStatus.Skipped => "Orange",
        _ => "Gray"
    };
}

/// <summary>
/// Status of ZIP file processing
/// </summary>
public enum ZipFileStatus
{
    Pending,
    Processing,
    Success,
    Failed,
    Skipped
}

/// <summary>
/// Log entry for the log viewer
/// </summary>
public partial class LogEntry : ObservableObject
{
    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private string _level = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    public string LevelColor => Level switch
    {
        "Information" => "Black",
        "Success" => "Green",
        "Warning" => "Orange",
        "Error" => "Red",
        "Debug" => "Gray",
        _ => "Black"
    };
}
