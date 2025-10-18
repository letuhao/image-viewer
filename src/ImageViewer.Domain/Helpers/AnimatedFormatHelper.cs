using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageViewer.Domain.Helpers;

/// <summary>
/// Helper class for detecting and handling animated image formats
/// 中文：动画图片格式检测和处理辅助类
/// Tiếng Việt: Lớp trợ giúp phát hiện và xử lý định dạng ảnh động
/// </summary>
public static class AnimatedFormatHelper
{
    /// <summary>
    /// List of animated image formats that should not be converted to cache images
    /// </summary>
    private static readonly HashSet<string> AnimatedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gif",
        ".apng",
        ".webp" // WebP can be animated
    };

    /// <summary>
    /// List of video formats that should not be converted to cache images
    /// </summary>
    private static readonly HashSet<string> VideoFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".avi",
        ".mov",
        ".wmv",
        ".flv",
        ".mkv",
        ".webm"
    };

    /// <summary>
    /// Check if a file is an animated format that should preserve its original format
    /// </summary>
    /// <param name="filename">Filename or path to check</param>
    /// <returns>True if the file is an animated format</returns>
    public static bool IsAnimatedFormat(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        var extension = System.IO.Path.GetExtension(filename);
        return AnimatedFormats.Contains(extension) || VideoFormats.Contains(extension);
    }

    /// <summary>
    /// Check if a file format string is animated
    /// </summary>
    /// <param name="format">Format string (e.g., "gif", "webp", ".gif")</param>
    /// <returns>True if the format is animated</returns>
    public static bool IsAnimatedFormatString(string format)
    {
        if (string.IsNullOrEmpty(format))
            return false;

        // Normalize format string
        var normalizedFormat = format.StartsWith(".") ? format : $".{format}";
        return AnimatedFormats.Contains(normalizedFormat) || VideoFormats.Contains(normalizedFormat);
    }

    /// <summary>
    /// Get the list of all animated file extensions
    /// </summary>
    /// <returns>List of animated file extensions</returns>
    public static IEnumerable<string> GetAnimatedExtensions()
    {
        return AnimatedFormats.Concat(VideoFormats);
    }

    /// <summary>
    /// Check if a file should be copied as-is instead of being processed for cache
    /// </summary>
    /// <param name="filename">Filename or path to check</param>
    /// <returns>True if the file should be copied as-is</returns>
    public static bool ShouldCopyAsIs(string filename)
    {
        return IsAnimatedFormat(filename);
    }

    /// <summary>
    /// Get the appropriate cache strategy for a file
    /// </summary>
    /// <param name="filename">Filename or path</param>
    /// <returns>Cache strategy: "copy" for animated files, "process" for static images</returns>
    public static string GetCacheStrategy(string filename)
    {
        return IsAnimatedFormat(filename) ? "copy" : "process";
    }
}

