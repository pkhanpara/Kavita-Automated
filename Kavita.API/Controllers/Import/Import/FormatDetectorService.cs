using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kavita.Models.Entities.Enums;

namespace Kavita.Services.Import;

/// <summary>
/// Service for detecting and managing media formats within the Kavita Importer.
/// Provides format detection, priority management, and format-specific processing.
/// </summary>
public class FormatDetectorService : IFormatDetectorService
{
    private readonly Dictionary<MediaFormat, FormatPriority> _formatPriorities;
    private readonly Dictionary<string, MediaFormat> _extensionToFormatMap;
    private readonly Dictionary<string, Func<string, bool>> _formatValidators;
    private readonly ILogger<FormatDetectorService> _logger;

    /// <summary>
    /// Initializes a new instance of the FormatDetectorService class
    /// </summary>
    /// <param name="logger">The logger instance for logging operations</param>
    public FormatDetectorService(ILogger<FormatDetectorService> logger)
    {
        _logger = logger;
        _formatPriorities = new Dictionary<MediaFormat, FormatPriority>();
        _extensionToFormatMap = new Dictionary<string, MediaFormat>(StringComparer.OrdinalIgnoreCase);
        _formatValidators = new Dictionary<string, Func<string, bool>>(StringComparer.OrdinalIgnoreCase);

        InitializeFormatMappings();
    }

    /// <summary>
    /// Initializes the format mappings and validators
    /// </summary>
    private void InitializeFormatMappings()
    {
        // Define format extension mappings
        var extensionMappings = new Dictionary<string, MediaFormat>(StringComparer.OrdinalIgnoreCase)
        {
            // Primary eBook formats
            { ".epub", MediaFormat.EPUB },
            { ".pdf", MediaFormat.PDF },
            { ".mobi", MediaFormat.MOBI },
            { ".azw3", MediaFormat.AZW3 },

            // Comic book formats
            { ".cbz", MediaFormat.CBZ },
            { ".cbr", MediaFormat.CBR },
            { ".cb7", MediaFormat.CB7 },

            // Image formats
            { ".png", MediaFormat.PNG },
            { ".jpg", MediaFormat.JPEG },
            { ".jpeg", MediaFormat.JPEG },
            { ".gif", MediaFormat.GIF },
            { ".bmp", MediaFormat.BMP },
            { ".svg", MediaFormat.SVG },
            { ".webp", MediaFormat.WebP },
            { ".tiff", MediaFormat.TIFF },
            { ".avif", MediaFormat.AVIF }
        };

        // Add extension mappings
        foreach (var mapping in extensionMappings)
        {
            if (!_extensionToFormatMap.ContainsKey(mapping.Key))
            {
                _extensionToFormatMap.Add(mapping.Key, mapping.Value);
            }
        }

        // Define format validators
        _formatValidators["EPUB"] = ValidateEpubFile;
        _formatValidators["PDF"] = ValidatePdfFile;
        _formatValidators["CBZ"] = ValidateCbzFile;
        _formatValidators["CBR"] = ValidateCbrFile;
        _formatValidators["Image"] = ValidateImageFile;
        _formatValidators["Download"] = ValidateDownloadFile;
    }

    /// <summary>
    /// Sets the format priorities for the detector service
    /// </summary>
    /// <param name="formatPriorities">The list of format priorities to set</param>
    public void SetFormatPriorities(IEnumerable<FormatPriority> formatPriorities)
    {
        foreach (var formatPriority in formatPriorities)
        {
            _formatPriorities[formatPriority.Format] = formatPriority;

            // Map extensions to the format
            foreach (var extension in formatPriority.Extensions)
            {
                var ext = extension.StartsWith(".") ? extension : $".{extension}";
                if (!_extensionToFormatMap.ContainsKey(ext))
                {
                    _extensionToFormatMap[ext] = formatPriority.Format;
                }
            }
        }

        _logger.LogInformation("Set format priorities for {Count} formats", formatPriorities.Count());
    }

    /// <summary>
    /// Detects the media format of a file based on its extension and content
    /// </summary>
    /// <param name="filePath">The path of the file to detect</param>
    /// <returns>The detected media format</returns>
    public MediaFormat DetectFormat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Invalid file path provided for format detection");
            return MediaFormat.EPUB; // Default to EPUB
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var format = GetFormatFromExtension(extension);

        // Validate the format using format-specific validators
        if (_formatValidators.TryGetValue(format.ToString(), out var validator))
        {
            var isValid = validator(filePath);
            if (!isValid)
            {
                _logger.LogDebug("Format validation failed for: {FilePath}, falling back to default format", filePath);
                format = MediaFormat.EPUB;
            }
        }

        return format;
    }

    /// <summary>
    /// Gets the format priority for a specified media format
    /// </summary>
    /// <param name="format">The media format to get the priority for</param>
    /// <returns>The format priority, or default priority if not found</returns>
    public int GetFormatPriority(MediaFormat format)
    {
        if (_formatPriorities.TryGetValue(format, out var priority))
        {
            return priority.Priority;
        }

        // Return default priority for unknown formats
        return GetDefaultFormatPriority(format);
    }

    /// <summary>
    /// Gets the media format from a file extension
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot)</param>
    /// <returns>The corresponding media format</returns>
    public MediaFormat GetFormatFromExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return MediaFormat.EPUB;
        }

        var ext = extension.StartsWith(".") ? extension : $".{extension}";

        if (_extensionToFormatMap.TryGetValue(ext, out var format))
        {
            return format;
        }

        // Fallback to generic format based on extension pattern
        return GetFormatByExtensionPattern(ext);
    }

    /// <summary>
    /// Gets the format priority for a file extension
    /// </summary>
    /// <param name="extension">The file extension to get the priority for</param>
    /// <returns>The format priority value</returns>
    public int GetFormatPriorityForExtension(string extension)
    {
        var format = GetFormatFromExtension(extension);
        return GetFormatPriority(format);
    }

    /// <summary>
    /// Determines the media format based on the extension pattern
    /// </summary>
    /// <param name="extension">The file extension to analyze</param>
    /// <returns>The determined media format</returns>
    private MediaFormat GetFormatByExtensionPattern(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            // eBook formats
            ".epub" or ".mobi" or ".azw3" or ".azw" or ".kfx" or ".lrf" => MediaFormat.EPUB,
            ".pdf" => MediaFormat.PDF,
            ".cbz" or ".cb7" => MediaFormat.CBZ,
            ".cbr" => MediaFormat.CBR,

            // Image formats
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".svg" or ".webp" or ".tiff" or ".tif" or ".avif"
                => GetImageFormatByExtension(extension),

            // Video formats
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" or ".m4v" => MediaFormat.PDF,

            // Audio formats
            ".mp3" or ".wav" or ".aac" or ".flac" or ".ogg" or ".m4a" or ".wma" or ".aiff" => MediaFormat.PDF,

            // Download and temporary files
            ".partial" or ".download" or ".incomplete" or ".part" or ".downloading" => MediaFormat.EPUB,

            _ => MediaFormat.EPUB
        };
    }

    /// <summary>
    /// Gets the specific image format based on the extension
    /// </summary>
    /// <param name="extension">The file extension to analyze</param>
    /// <returns>The specific image format</returns>
    private MediaFormat GetImageFormatByExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => MediaFormat.PNG,
            ".jpg" or ".jpeg" => MediaFormat.JPEG,
            ".gif" => MediaFormat.GIF,
            ".bmp" => MediaFormat.BMP,
            ".svg" => MediaFormat.SVG,
            ".webp" => MediaFormat.WebP,
            ".tiff" or ".tif" => MediaFormat.TIFF,
            ".avif" => MediaFormat.AVIF,
            _ => MediaFormat.PNG
        };
    }

    /// <summary>
    /// Gets the default priority value for a media format
    /// </summary>
    /// <param name="format">The media format to get the default priority for</param>
    /// <returns>The default priority value</returns>
    private int GetDefaultFormatPriority(MediaFormat format)
    {
        return format switch
        {
            MediaFormat.EPUB or MediaFormat.PDF => 90,
            MediaFormat.CBZ or MediaFormat.CBR or MediaFormat.MOBI or MediaFormat.AZW3 or MediaFormat.CB7 => 85,
            MediaFormat.PNG or MediaFormat.JPEG or MediaFormat.WebP or MediaFormat.GIF
                or MediaFormat.BMP or MediaFormat.SVG or MediaFormat.TIFF or MediaFormat.AVIF => 75,
            _ => 70
        };
    }

    /// <summary>
    /// Creates a format priority for a media format
    /// </summary>
    /// <param name="format">The media format to create the priority for</param>
    /// <param name="priority">The priority value</param>
    /// <param name="extensions">The file extensions associated with the format</param>
    /// <returns>A new FormatPriority instance</returns>
    public FormatPriority CreateFormatPriority(
        MediaFormat format,
        int priority,
        params string[] extensions)
    {
        return new FormatPriority
        {
            Format = format,
            Priority = priority,
            Extensions = extensions.ToList(),
            IsPreferred = priority >= 85
        };
    }

    /// <summary>
    /// Validates an EPUB file
    /// </summary>
    /// <param name="filePath">The path of the file to validate</param>
    /// <returns>True if the file is a valid EPUB; otherwise, false</returns>
    private bool ValidateEpubFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists && fileInfo.Length > 0 && fileInfo.Extension.Equals(".epub", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating EPUB file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Validates a PDF file
    /// </summary>
    /// <param name="filePath">The path of the file to validate</param>
    /// <returns>True if the file is a valid PDF; otherwise, false</returns>
    private bool ValidatePdfFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists && fileInfo.Length > 0 && fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating PDF file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Validates a CBZ file
    /// </summary>
    /// <param name="filePath">The path of the file to validate</param>
    /// <returns>True if the file is a valid CBZ; otherwise, false</returns>
    private bool ValidateCbzFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists && fileInfo.Length > 0 && fileInfo.Extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating CBZ file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Validates a CBR file
    /// </summary>
    /// <param name="filePath">The path of the file to validate</param>
    /// <returns>True if the file is a valid CBR; otherwise, false</returns>
    private bool ValidateCbrFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists && fileInfo.Length > 0 && fileInfo.Extension.Equals(".cbr", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating CBR file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Validates an image file
    /// </summary>
    /// <param name="filePath">The path of the file to validate</param>
    /// <returns>True if the file is a valid image; otherwise, false</returns>
    private bool ValidateImageFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg", ".webp", ".tiff", ".avif" };

            return fileInfo.Exists &&
                   fileInfo.Length > 0 &&
                   imageExtensions.Contains(fileInfo.Extension.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating image file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Validates a download file
    /// </summary>
    /// <param name="filePath">The path of the file to validate</param>
    /// <returns>True if the file is a valid download file; otherwise, false</returns>
    private bool ValidateDownloadFile(string filePath)
    {
        try
        {
            var downloadExtensions = new[] { ".partial", ".download", ".incomplete", ".part", ".downloading" };
            var fileInfo = new FileInfo(filePath);

            return fileInfo.Exists &&
                   downloadExtensions.Contains(fileInfo.Extension.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating download file: {FilePath}", filePath);
            return false;
        }
    }
}

/// <summary>
/// Interface for the Format Detector Service
/// </summary>
public interface IFormatDetectorService
{
    /// <summary>
    /// Sets the format priorities for the detector service
    /// </summary>
    /// <param name="formatPriorities">The list of format priorities to set</param>
    void SetFormatPriorities(IEnumerable<FormatPriority> formatPriorities);

    /// <summary>
    /// Detects the media format of a file based on its extension and content
    /// </summary>
    /// <param name="filePath">The path of the file to detect</param>
    /// <returns>The detected media format</returns>
    MediaFormat DetectFormat(string filePath);

    /// <summary>
    /// Gets the format priority for a specified media format
    /// </summary>
    /// <param name="format">The media format to get the priority for</param>
    /// <returns>The format priority, or default priority if not found</returns>
    int GetFormatPriority(MediaFormat format);

    /// <summary>
    /// Gets the media format from a file extension
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot)</param>
    /// <returns>The corresponding media format</returns>
    MediaFormat GetFormatFromExtension(string extension);

    /// <summary>
    /// Gets the format priority for a file extension
    /// </summary>
    /// <param name="extension">The file extension to get the priority for</param>
    /// <returns>The format priority value</returns>
    int GetFormatPriorityForExtension(string extension);

    /// <summary>
    /// Creates a format priority for a media format
    /// </summary>
    /// <param name="format">The media format to create the priority for</param>
    /// <param name="priority">The priority value</param>
    /// <param name="extensions">The file extensions associated with the format</param>
    /// <returns>A new FormatPriority instance</returns>
    FormatPriority CreateFormatPriority(MediaFormat format, int priority, params string[] extensions);
}
