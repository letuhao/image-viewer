namespace ImageViewer.Application.Options;

public class CacheSizeOption
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ImageCachePresetsOptions
{
    public Dictionary<string, List<CacheSizeOption>> Presets { get; set; } = new();
}


