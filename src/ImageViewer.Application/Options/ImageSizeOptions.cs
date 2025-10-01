namespace ImageViewer.Application.Options;

public class ImageSizeOptions
{
    public int ThumbnailWidth { get; set; } = 300;
    public int ThumbnailHeight { get; set; } = 300;
    public int CacheWidth { get; set; } = 1280;
    public int CacheHeight { get; set; } = 720;
    public int JpegQuality { get; set; } = 95;
}


