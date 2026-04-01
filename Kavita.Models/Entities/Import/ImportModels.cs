using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kavita.Models.Entities.Enums;

namespace Kavita.Services.Import;

/// <summary>
/// Enumeration of supported media formats with priority levels
/// </summary>
public enum MediaFormat
{
    /// <summary>
    /// Electronic Publication format
    /// </summary>
    EPUB = 1,

    /// <summary>
    /// Portable Document Format
    /// </summary>
    PDF = 2,

    /// <summary>
    /// Comic Book Archive (ZIP-based)
    /// </summary>
    CBZ = 3,

    /// <summary>
    /// Comic Book RAR Archive
    /// </summary>
    CBR = 4,

    /// <summary>
    /// Portable Network Graphics
    /// </summary>
    PNG = 5,

    /// <summary>
    /// Joint Photographic Experts Group
    /// </summary>
    JPEG = 6,

    /// <summary>
    /// WebP Image Format
    /// </summary>
    WebP = 7,

    /// <summary>
    /// Graphics Interchange Format
    /// </summary>
    GIF = 8,

    /// <summary>
    /// AV1 Image File Format
    /// </summary>
    AVIF = 9,

    /// <summary>
    /// Bitmap Image File
    /// </summary>
    BMP = 10,

    /// <summary>
    /// Scalable Vector Graphics
    /// </summary>
    SVG = 11,

    /// <summary>
    /// Tagged Image File Format
    /// </summary>
    TIFF = 12,

    /// <summary>
    /// Mobile eBook Format
    /// </summary>
    MOBI = 13,

    /// <summary>
    /// Amazon Kindle Format
    /// </summary>
    AZW3 = 14,

    /// <summary>
    /// Comic Book Seven Archive
    /// </summary>
    CB7 = 15
}

/// <summary>
/// Represents the priority level of a media format
/// </summary>
public class FormatPriority
{
    /// <summary>
    /// Gets or sets the media format
    /// </summary>
    public MediaFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the priority value (higher is more important)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the file extensions associated with this format
    /// </summary>
    public List<string> Extensions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the format is preferred
    /// </summary>
    public bool IsPreferred { get; set; }

    /// <summary>
    /// Initializes a new instance of the FormatPriority class
    /// </summary>
    public FormatPriority()
    {
    }

    /// <summary>
    /// Initializes a new instance of the FormatPriority class with specified values
    /// </summary>
    public FormatPriority(MediaFormat format, int priority, params string[] extensions)
    {
        Format = format;
        Priority = priority;
        Extensions = extensions.ToList();
    }
}

/// <summary>
/// Represents the status of an imported file
/// </summary>
public class ImportFileStatus
{
    /// <summary>
    /// Gets or sets the unique identifier for the import operation
    /// </summary>
    public Guid ImportId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the full path of the imported file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the imported file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file format of the imported file
    /// </summary>
    public MediaFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was last modified
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the current status of the import process
    /// </summary>
    public ImportStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the priority level of the file format
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets any error messages related to the import process
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the file is a download file
    /// </summary>
    public bool IsDownloadFile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file is complete and ready for processing
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Gets or sets the source folder where the file was imported from
    /// </summary>
    public string SourceFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target folder where the file should be organized
    /// </summary>
    public string TargetFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of actions performed during the import process
    /// </summary>
    public List<string> Actions { get; set; } = new();

    /// <summary>
    /// Converts the import file status to a dictionary for serialization
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "ImportId", ImportId.ToString() },
            { "FilePath", FilePath },
            { "FileName", FileName },
            { "Format", Format.ToString() },
            { "FileSize", FileSize },
            { "CreatedAt", CreatedAt.ToString("o") },
            { "LastModified", LastModified.ToString("o") },
            { "Status", Status.ToString() },
            { "Priority", Priority },
            { "ErrorMessage", ErrorMessage },
            { "IsDownloadFile", IsDownloadFile },
            { "IsComplete", IsComplete },
            { "SourceFolder", SourceFolder },
            { "TargetFolder", TargetFolder }
        };
    }
}

/// <summary>
/// Represents the current status of the import process
/// </summary>
public enum ImportStatus
{
    /// <summary>
    /// The file is pending processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The file is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// The file has been successfully imported
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The file import encountered an error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The file is being downloaded
    /// </summary>
    Downloading = 4,

    /// <summary>
    /// The file is partially imported
    /// </summary>
    Partial = 5
}

/// <summary>
/// Configuration settings for the Kavita Importer service
/// </summary>
public class ImportSettings
{
    /// <summary>
    /// Gets or sets the path to the import folder
    /// </summary>
    public string ImportFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target library folder for organized content
    /// </summary>
    public string TargetLibraryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether automatic monitoring is enabled
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether automatic organization is enabled
    /// </summary>
    public bool EnableOrganization { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether duplicate detection is enabled
    /// </summary>
    public bool EnableDuplicateDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum file size (in MB) to be processed automatically
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Gets or sets the list of preferred file formats
    /// </summary>
    public List<FormatPriority> PreferredFormats { get; set; } = new();

    /// <summary>
    /// Gets or sets the polling interval (in seconds) for monitoring changes
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to process only new files
    /// </summary>
    public bool ProcessNewFilesOnly { get; set; }

    /// <summary>
    /// Gets or sets the list of blacklisted folders
    /// </summary>
    public List<string> BlacklistedFolders { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of blacklisted file patterns
    /// </summary>
    public List<string> BlacklistedPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of download extensions for tracking incomplete downloads
    /// </summary>
    public List<string> DownloadExtensions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Creates a deep copy of the current ImportSettings instance
    /// </summary>
    public ImportSettings Clone()
    {
        return new ImportSettings
        {
            ImportFolderPath = ImportFolderPath,
            TargetLibraryPath = TargetLibraryPath,
            EnableMonitoring = EnableMonitoring,
            EnableOrganization = EnableOrganization,
            EnableDuplicateDetection = EnableDuplicateDetection,
            MaxFileSizeMB = MaxFileSizeMB,
            PreferredFormats = PreferredFormats.Select(fp => new FormatPriority
            {
                Format = fp.Format,
                Priority = fp.Priority,
                Extensions = fp.Extensions.ToList(),
                IsPreferred = fp.IsPreferred
            }).ToList(),
            PollingIntervalSeconds = PollingIntervalSeconds,
            ProcessNewFilesOnly = ProcessNewFilesOnly,
            BlacklistedFolders = BlacklistedFolders.ToList(),
            BlacklistedPatterns = BlacklistedPatterns.ToList(),
            DownloadExtensions = DownloadExtensions.ToList(),
            EnableDetailedLogging = EnableDetailedLogging
        };
    }
}

/// <summary>
/// Represents the result of an import operation
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the import operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the total number of files processed
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files successfully imported
    /// </summary>
    public int ImportedFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files that failed during import
    /// </summary>
    public int FailedFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files skipped during import
    /// </summary>
    public int SkippedFiles { get; set; }

    /// <summary>
    /// Gets or sets the total execution time of the import operation
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the list of imported file statuses
    /// </summary>
    public List<ImportFileStatus> ImportedFilesList { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of error messages encountered during import
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets the summary of the import operation
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Creates a success result with the specified parameters
    /// </summary>
    public static ImportResult CreateSuccess(int totalFiles, int importedFiles, int failedFiles, int skippedFiles, string summary)
    {
        return new ImportResult
        {
            Success = true,
            TotalFiles = totalFiles,
            ImportedFiles = importedFiles,
            FailedFiles = failedFiles,
            SkippedFiles = skippedFiles,
            ExecutionTime = TimeSpan.Zero,
            Summary = summary
        };
    }

    /// <summary>
    /// Creates a failure result with the specified error message
    /// </summary>
    public static ImportResult CreateFailure(string errorMessage)
    {
        return new ImportResult
        {
            Success = false,
            TotalFiles = 0,
            ImportedFiles = 0,
            FailedFiles = 1,
            SkippedFiles = 0,
            ExecutionTime = TimeSpan.Zero,
            ErrorMessages = new List<string> { errorMessage },
            Summary = $"Import failed: {errorMessage}"
        };
    }
}
