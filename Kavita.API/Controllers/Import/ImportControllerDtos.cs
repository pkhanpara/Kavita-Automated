using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Kavita.Models.Entities.Enums;

namespace Kavita.API.Controllers;

/// <summary>
/// DTO for import configuration settings
/// </summary>
public class ImportConfigurationDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the configuration
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the import configuration
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the import configuration
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the import folder
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ImportFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the target library folder
    /// </summary>
    [StringLength(500)]
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
    /// Gets or sets the polling interval (in seconds) for monitoring changes
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to process only new files
    /// </summary>
    public bool ProcessNewFilesOnly { get; set; }

    /// <summary>
    /// Gets or sets the list of preferred file formats
    /// </summary>
    public List<FormatPreferenceDto> PreferredFormats { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of blacklisted folders
    /// </summary>
    public List<string> BlacklistedFolders { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of blacklisted file patterns
    /// </summary>
    public List<string> BlacklistedPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of download extensions
    /// </summary>
    public List<string> DownloadExtensions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the configuration was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for format preference settings
/// </summary>
public class FormatPreferenceDto
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
}

/// <summary>
/// DTO for import file status
/// </summary>
public class ImportFileStatusDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the import operation
    /// </summary>
    public Guid ImportId { get; set; }

    /// <summary>
    /// Gets or sets the full path of the imported file
    /// </summary>
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the imported file
    /// </summary>
    [StringLength(200)]
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
    [StringLength(1000)]
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
    [StringLength(500)]
    public string SourceFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target folder where the file should be organized
    /// </summary>
    [StringLength(500)]
    public string TargetFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of actions performed during the import process
    /// </summary>
    public List<string> Actions { get; set; } = new();
}

/// <summary>
/// DTO for import statistics
/// </summary>
public class ImportStatisticsDto
{
    /// <summary>
    /// Gets or sets the total number of imported files
    /// </summary>
    public int TotalImportedFiles { get; set; }

    /// <summary>
    /// Gets or sets the total number of files processed
    /// </summary>
    public int TotalFilesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of files successfully imported
    /// </summary>
    public int TotalFilesImported { get; set; }

    /// <summary>
    /// Gets or sets the total number of files that failed during import
    /// </summary>
    public int TotalFilesFailed { get; set; }

    /// <summary>
    /// Gets or sets the total number of files skipped during import
    /// </summary>
    public int TotalFilesSkipped { get; set; }

    /// <summary>
    /// Gets or sets the total size of imported files in bytes
    /// </summary>
    public long TotalImportedSize { get; set; }

    /// <summary>
    /// Gets or sets the average processing time per file in milliseconds
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the list of preferred formats
    /// </summary>
    public List<FormatPreferenceDto> PreferredFormats { get; set; } = new();

    /// <summary>
    /// Gets or sets the current monitoring status
    /// </summary>
    public bool IsMonitoring { get; set; }

    /// <summary>
    /// Gets or sets the current initialization status
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Gets or sets the import folder path
    /// </summary>
    [StringLength(500)]
    public string ImportFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target library path
    /// </summary>
    [StringLength(500)]
    public string TargetLibraryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the statistics were generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for import result
/// </summary>
public class ImportResultDto
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
    public string ExecutionTime { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of imported file statuses
    /// </summary>
    public List<ImportFileStatusDto> ImportedFilesList { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of error messages encountered during import
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets the summary of the import operation
    /// </summary>
    [StringLength(2000)]
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// DTO for blacklisted folders
/// </summary>
public class BlacklistedFolderDto
{
    /// <summary>
    /// Gets or sets the name of the blacklisted folder
    /// </summary>
    [Required]
    [StringLength(200)]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path of the blacklisted folder
    /// </summary>
    [StringLength(500)]
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the folder is blacklisted
    /// </summary>
    public bool IsBlacklisted { get; set; }

    /// <summary>
    /// Gets or sets the pattern used for blacklisting
    /// </summary>
    [StringLength(200)]
    public string BlacklistPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the folder was added to the blacklist
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for request to import files
/// </summary>
public class ImportFilesRequestDto
{
    /// <summary>
    /// Gets or sets the list of file paths to import
    /// </summary>
    public List<string> FilePaths { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to move files after import
    /// </summary>
    public bool MoveFiles { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing files
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Gets or sets the target library path for the import operation
    /// </summary>
    [StringLength(500)]
    public string TargetLibraryPath { get; set; } = string.Empty;
}

/// <summary>
/// DTO for request to update import settings
/// </summary>
public class UpdateImportSettingsRequestDto
{
    /// <summary>
    /// Gets or sets the path to the import folder
    /// </summary>
    [StringLength(500)]
    public string ImportFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the target library folder
    /// </summary>
    [StringLength(500)]
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
    /// Gets or sets the polling interval (in seconds) for monitoring changes
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to process only new files
    /// </summary>
    public bool ProcessNewFilesOnly { get; set; }

    /// <summary>
    /// Gets or sets the list of preferred file formats
    /// </summary>
    public List<FormatPreferenceDto> PreferredFormats { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of blacklisted folders
    /// </summary>
    public List<string> BlacklistedFolders { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of blacklisted file patterns
    /// </summary>
    public List<string> BlacklistedPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of download extensions
    /// </summary>
    public List<string> DownloadExtensions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; }
}
