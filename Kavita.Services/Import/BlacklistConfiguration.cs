using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kavita.Models.Entities.Enums;

namespace Kavita.Services.Import;

/// <summary>
/// Static singleton configuration for managing file and folder blacklists.
/// Provides predefined patterns for excluding files and folders from processing.
/// </summary>
public static class BlacklistConfiguration
{
    /// <summary>
    /// Folder patterns to exclude from processing
    /// </summary>
    private static readonly HashSet<string> FolderPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // OS-specific metadata folders
        ".DS_Store",
        "Thumbs.db",
        "@eaDir",
        "__MACOSX",
        "Desktop.ini",

        // Version control directories
        ".git",
        ".svn",
        ".hg",
        ".bzr",

        // Package manager directories
        "node_modules",
        ".npm",
        ".yarn",
        ".pnpm-store",
        "vendor",

        // Build and cache directories
        "build",
        "dist",
        ".build",
        ".cache",
        "tmp",
        "temp",
        ".temp",
        "output",
        ".output",

        // IDE and editor configurations
        ".idea",
        ".vscode",
        ".project",
        ".settings",
        "*.swo",
        "*.swn",

        // Log and data directories
        "logs",
        ".logs",
        "data",
        ".data",
        "storage",
        ".storage",

        // Backup and archive directories
        "backups",
        ".backups",
        "archives",
        ".archives",
        "backup",
        ".backup",

        // Documentation directories
        "docs",
        ".docs",
        "documentation",
        "doc",

        // Configuration directories
        "config",
        ".config",
        "configs",
        ".configs",
        "configuration",

        // Test directories
        "tests",
        ".tests",
        "test",
        "spec",
        ".spec",

        // Script directories
        "scripts",
        ".scripts",
        "src",
        "source",
        "lib",
        "libs",

        // Environment and deployment directories
        "env",
        ".env",
        "environment",
        "deploy",
        ".deploy",
        "deployment",

        // CI/CD directories
        ".github",
        ".gitlab",
        "ci",
        "cd",
        ".ci",
        ".cd",

        // Media and asset directories
        "assets",
        ".assets",
        "media",
        ".media",
        "images",
        "videos",
        "audio",

        // Temporary storage
        ".tmp",
        ".temporary",
        "scratch",
        "spool"
    };

    /// <summary>
    /// File patterns to exclude from processing
    /// </summary>
    private static readonly HashSet<string> FilePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Temporary files
        "*.tmp",
        "*.temp",
        "*.tempfile",
        "*.tempdata",

        // Log files
        "*.log",
        "*.logs",
        "*.txt",
        "*.md",
        "*.rst",

        // Configuration files
        "*.json",
        "*.yaml",
        "*.yml",
        "*.xml",
        "*.ini",
        "*.conf",
        "*.cfg",
        "*.config",

        // Data files
        "*.csv",
        "*.dat",
        "*.db",
        "*.sqlite",
        "*.sql",

        // Archive files
        "*.tar",
        "*.gz",
        "*.zip",
        "*.rar",
        "*.7z",
        "*.bz2",

        // Cache files
        "*.cache",
        "*.idx",
        "*.cache.idx",

        // Lock files
        "*.lock",
        "package-lock.json",
        "yarn.lock",
        "Cargo.lock",
        "Podfile.lock",
        "Gemfile.lock",

        // Backup files
        "*.bak",
        "*.backup",
        "*.old",
        "*.orig"
    };

    /// <summary>
    /// File extension patterns to exclude from processing
    /// </summary>
    private static readonly HashSet<string> ExtensionPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Source code and script files
        ".cs",
        ".csproj",
        ".sln",
        ".cshtml",
        ".razor",

        // Web technologies
        ".html",
        ".htm",
        ".css",
        ".scss",
        ".less",
        ".js",
        ".ts",
        ".jsx",
        ".tsx",

        // Documentation formats (media formats Kavita imports, like .pdf, must NOT be here)
        ".doc",
        ".docx",
        ".odt",
        ".rtf",
        ".tex",

        // Image formats not supported by the importer (supported ones such as
        // .png/.jpg/.webp/.gif/.avif/.bmp/.svg/.tiff are importable and must NOT be here)
        ".ico",
        ".heic",
        ".heif",

        // Video formats
        ".mp4",
        ".avi",
        ".mkv",
        ".mov",
        ".wmv",
        ".flv",
        ".webm",
        ".m4v",

        // Audio formats
        ".mp3",
        ".wav",
        ".aac",
        ".flac",
        ".ogg",
        ".m4a",
        ".wma",
        ".aiff",

        // E-book formats not supported by the importer (.epub/.mobi/.azw3 are importable
        // and must NOT be here)
        ".azw",
        ".kfx",
        ".lrf",

        // Import-specific extensions
        ".partial",
        ".download",
        ".pending",
        ".staging"
    };

    /// <summary>
    /// Download-related file extensions for tracking incomplete downloads
    /// </summary>
    private static readonly HashSet<string> DownloadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".partial",
        ".download",
        ".incomplete",
        ".part",
        ".downloading"
    };

    /// <summary>
    /// Precompiled regular expressions for pattern matching
    /// </summary>
    private static readonly Dictionary<string, Regex> PatternRegexes = new();

    /// <summary>
    /// Initializes the BlacklistConfiguration singleton
    /// </summary>
    static BlacklistConfiguration()
    {
        InitializePatternRegexes();
    }

    /// <summary>
    /// Initializes regular expressions for all pattern matching
    /// </summary>
    private static void InitializePatternRegexes()
    {
        // Convert folder patterns to regex
        foreach (var folder in FolderPatterns)
        {
            if (folder.StartsWith("*"))
            {
                var regex = new Regex($"^{Regex.Escape(folder).Replace(@"\*", ".*")}$", RegexOptions.IgnoreCase);
                PatternRegexes[folder] = regex;
            }
            else
            {
                var regex = new Regex($"^{Regex.Escape(folder)}$", RegexOptions.IgnoreCase);
                PatternRegexes[folder] = regex;
            }
        }

        // Convert file patterns to regex
        foreach (var file in FilePatterns)
        {
            if (file.StartsWith("*."))
            {
                var regex = new Regex($"^{Regex.Escape(file).Replace(@"\*", ".*").Replace(@"\.", ".")}$", RegexOptions.IgnoreCase);
                PatternRegexes[file] = regex;
            }
        }

        // Convert extension patterns to regex
        foreach (var extension in ExtensionPatterns)
        {
            var regex = new Regex($"^{Regex.Escape(extension)}$", RegexOptions.IgnoreCase);
            PatternRegexes[extension] = regex;
        }
    }

    /// <summary>
    /// Checks if a folder path should be excluded from processing
    /// </summary>
    /// <param name="folderPath">The folder path to check</param>
    /// <returns>True if the folder should be excluded; otherwise, false</returns>
    public static bool IsFolderBlacklisted(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return false;

        var folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar));

        // Check against exact folder patterns
        if (FolderPatterns.Contains(folderName))
            return true;

        // Check against pattern-based folder rules
        foreach (var pattern in FolderPatterns)
        {
            if (PatternRegexes.TryGetValue(pattern, out var regex) && regex.IsMatch(folderName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a file should be excluded from processing
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if the file should be excluded; otherwise, false</returns>
    public static bool IsFileBlacklisted(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // Check exact file patterns
        if (FilePatterns.Contains(fileName))
            return true;

        // Check pattern-based file rules
        foreach (var pattern in FilePatterns)
        {
            if (PatternRegexes.TryGetValue(pattern, out var regex) && regex.IsMatch(fileName))
                return true;
        }

        // Check extension patterns
        if (ExtensionPatterns.Contains(extension))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a file is a download-related file (e.g., partial downloads)
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if the file is a download-related file; otherwise, false</returns>
    public static bool IsDownloadFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return DownloadExtensions.Contains(extension);
    }

    /// <summary>
    /// Checks if a file is complete and ready for processing
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if the file is complete and ready; otherwise, false</returns>
    public static bool IsFileComplete(string filePath)
    {
        if (!IsDownloadFile(filePath))
            return true;

        // For download-related files, check if the file has finished downloading
        // This can be determined by checking for the absence of partial indicators
        var fileInfo = new FileInfo(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // Files without download extensions are considered complete
        if (!DownloadExtensions.Contains(extension))
            return true;

        // Check if file size is stable (no recent changes indicating active download)
        // A file is considered complete if it hasn't changed in the last 2 minutes
        var timeSinceLastChange = DateTime.Now - fileInfo.LastWriteTime;
        return timeSinceLastChange.TotalMinutes >= 2;
    }

    /// <summary>
    /// Gets all folder patterns in the blacklist
    /// </summary>
    /// <returns>A read-only collection of folder patterns</returns>
    public static IReadOnlyCollection<string> GetFolderPatterns()
    {
        return new ReadOnlyCollection<string>(FolderPatterns.ToList());
    }

    /// <summary>
    /// Gets all file patterns in the blacklist
    /// </summary>
    /// <returns>A read-only collection of file patterns</returns>
    public static IReadOnlyCollection<string> GetFilePatterns()
    {
        return new ReadOnlyCollection<string>(FilePatterns.ToList());
    }

    /// <summary>
    /// Gets all extension patterns in the blacklist
    /// </summary>
    /// <returns>A read-only collection of extension patterns</returns>
    public static IReadOnlyCollection<string> GetExtensionPatterns()
    {
        return new ReadOnlyCollection<string>(ExtensionPatterns.ToList());
    }

    /// <summary>
    /// Gets all download-related extensions
    /// </summary>
    /// <returns>A read-only collection of download extensions</returns>
    public static IReadOnlyCollection<string> GetDownloadExtensions()
    {
        return new ReadOnlyCollection<string>(DownloadExtensions.ToList());
    }

    /// <summary>
    /// Determines if a path contains any blacklisted folders
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path contains blacklisted folders; otherwise, false</returns>
    public static bool HasBlacklistedFolders(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var paths = path.Split(Path.PathSeparator).ToList();
        paths.Add(path);

        return paths.Any(IsFolderBlacklisted);
    }

    /// <summary>
    /// Generates a descriptive summary of the blacklist configuration
    /// </summary>
    /// <returns>A string containing the blacklist configuration summary</returns>
    public static string GetConfigurationSummary()
    {
        var summary = new System.Text.StringBuilder();

        summary.AppendLine("=== Blacklist Configuration Summary ===\n");

        summary.AppendLine($"Total Folder Patterns: {FolderPatterns.Count}");
        summary.AppendLine($"Total File Patterns: {FilePatterns.Count}");
        summary.AppendLine($"Total Extension Patterns: {ExtensionPatterns.Count}");
        summary.AppendLine($"Download Extensions: {DownloadExtensions.Count}\n");

        summary.AppendLine("Key Features:");
        summary.AppendLine("- OS-specific folder support (Windows, macOS, Linux)");
        summary.AppendLine("- Version control integration (.git, .svn, .hg)");
        summary.AppendLine("- Build and cache directory management");
        summary.AppendLine("- Download tracking with .partial and .download extensions");
        summary.AppendLine("- Pattern-based filtering with regex matching");

        return summary.ToString();
    }
}
