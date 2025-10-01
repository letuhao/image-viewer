namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// View session settings value object
/// </summary>
public class ViewSessionSettings
{
    public bool IsFullscreen { get; private set; }
    public bool IsSlideshowMode { get; private set; }
    public int SlideshowIntervalSeconds { get; private set; }
    public bool IsRandomOrder { get; private set; }
    public bool IsLoopEnabled { get; private set; }
    public int PreloadCount { get; private set; }
    public string ImageFitMode { get; private set; }
    public bool ShowImageInfo { get; private set; }

    public ViewSessionSettings(
        bool isFullscreen = false,
        bool isSlideshowMode = false,
        int slideshowIntervalSeconds = 5,
        bool isRandomOrder = false,
        bool isLoopEnabled = true,
        int preloadCount = 3,
        string imageFitMode = "contain",
        bool showImageInfo = true)
    {
        IsFullscreen = isFullscreen;
        IsSlideshowMode = isSlideshowMode;
        SlideshowIntervalSeconds = slideshowIntervalSeconds;
        IsRandomOrder = isRandomOrder;
        IsLoopEnabled = isLoopEnabled;
        PreloadCount = preloadCount;
        ImageFitMode = imageFitMode;
        ShowImageInfo = showImageInfo;
    }

    public static ViewSessionSettings Default()
    {
        return new ViewSessionSettings();
    }

    public void SetFullscreen(bool enabled)
    {
        IsFullscreen = enabled;
    }

    public void SetSlideshowMode(bool enabled)
    {
        IsSlideshowMode = enabled;
    }

    public void UpdateSlideshowInterval(int seconds)
    {
        if (seconds < 1)
            throw new ArgumentException("Slideshow interval must be at least 1 second", nameof(seconds));

        SlideshowIntervalSeconds = seconds;
    }

    public void SetRandomOrder(bool enabled)
    {
        IsRandomOrder = enabled;
    }

    public void SetLoopEnabled(bool enabled)
    {
        IsLoopEnabled = enabled;
    }

    public void UpdatePreloadCount(int count)
    {
        if (count < 0)
            throw new ArgumentException("Preload count cannot be negative", nameof(count));

        PreloadCount = count;
    }

    public void UpdateImageFitMode(string fitMode)
    {
        if (string.IsNullOrWhiteSpace(fitMode))
            throw new ArgumentException("Image fit mode cannot be null or empty", nameof(fitMode));

        var validModes = new[] { "contain", "cover", "fill", "none", "scale-down" };
        if (!validModes.Contains(fitMode))
            throw new ArgumentException($"Invalid image fit mode: {fitMode}", nameof(fitMode));

        ImageFitMode = fitMode;
    }

    public void SetShowImageInfo(bool enabled)
    {
        ShowImageInfo = enabled;
    }
}
