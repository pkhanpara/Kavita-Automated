using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kavita.Services.Import;

/// <summary>
/// Event arguments for file creation events
/// </summary>
public class FileCreatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the path of the created file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the FileCreatedEventArgs class
    /// </summary>
    /// <param name="filePath">The path of the created file</param>
    public FileCreatedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Event arguments for file change events
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the path of the changed file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the FileChangedEventArgs class
    /// </summary>
    /// <param name="filePath">The path of the changed file</param>
    public FileChangedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Event arguments for file deletion events
/// </summary>
public class FileDeletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the path of the deleted file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the FileDeletedEventArgs class
    /// </summary>
    /// <param name="filePath">The path of the deleted file</param>
    public FileDeletedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Event arguments for download completion events
/// </summary>
public class DownloadCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the path of the completed download
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the download metadata
    /// </summary>
    public DownloadMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the DownloadCompletedEventArgs class
    /// </summary>
    /// <param name="filePath">The path of the completed download</param>
    public DownloadCompletedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Represents download metadata for tracking download progress
/// </summary>
public class DownloadMetadata
{
    /// <summary>
    /// Gets or sets the total size of the download in bytes
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the downloaded size in bytes
    /// </summary>
    public long DownloadedSize { get; set; }

    /// <summary>
    /// Gets or sets the download start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the download completion time
    /// </summary>
    public DateTime CompletionTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the download is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Gets the download progress percentage
    /// </summary>
    public double ProgressPercentage => TotalSize > 0 ? (DownloadedSize * 100.0) / TotalSize : 0;
}

/// <summary>
/// Event arguments for file processing events
/// </summary>
public class FileProcessingEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the path of the file being processed
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing status
    /// </summary>
    public FileProcessingStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the processing message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the FileProcessingEventArgs class
    /// </summary>
    /// <param name="filePath">The path of the file being processed</param>
    /// <param name="status">The processing status</param>
    public FileProcessingEventArgs(string filePath, FileProcessingStatus status)
    {
        FilePath = filePath;
        Status = status;
    }
}

/// <summary>
/// Represents the processing status of a file
/// </summary>
public enum FileProcessingStatus
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
    /// The file processing completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The file processing encountered an error
    /// </summary>
    Failed = 3
}

/// <summary>
/// Event arguments for import folder events
/// </summary>
public class ImportFolderEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the path of the import folder
    /// </summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type
    /// </summary>
    public ImportFolderEventType EventType { get; set; }

    /// <summary>
    /// Initializes a new instance of the ImportFolderEventArgs class
    /// </summary>
    /// <param name="folderPath">The path of the import folder</param>
    /// <param name="eventType">The event type</param>
    public ImportFolderEventArgs(string folderPath, ImportFolderEventType eventType)
    {
        FolderPath = folderPath;
        EventType = eventType;
    }
}

/// <summary>
/// Represents the type of import folder event
/// </summary>
public enum ImportFolderEventType
{
    /// <summary>
    /// The import folder was initialized
    /// </summary>
    Initialized = 0,

    /// <summary>
    /// The import folder was started
    /// </summary>
    Started = 1,

    /// <summary>
    /// The import folder was stopped
    /// </summary>
    Stopped = 2,

    /// <summary>
    /// The import folder encountered an error
    /// </summary>
    Error = 3,

    /// <summary>
    /// The import folder configuration was updated
    /// </summary>
    ConfigurationUpdated = 4
}

/// <summary>
/// Import folder watcher for monitoring file system changes
/// </summary>
public class ImportFolderWatcher : IImportFolderWatcher, IDisposable
{
    private readonly ILogger<ImportFolderWatcher> _logger;
    private readonly IFileSystem _fileSystem;
    private FileSystemWatcher? _fileSystemWatcher;
    private Timer? _pollingTimer;
    private readonly TimeSpan _pollingInterval;
    private readonly HashSet<string> _processedFiles;
    private readonly HashSet<string> _blacklistedExtensions;
    private bool _isWatching;
    private bool _isPolling;

    /// <summary>
    /// Initializes a new instance of the ImportFolderWatcher class
    /// </summary>
    /// <param name="logger">The logger instance for logging operations</param>
    public ImportFolderWatcher(ILogger<ImportFolderWatcher> logger)
        : this(logger, TimeSpan.FromSeconds(30))
    {
    }

    /// <summary>
    /// Initializes a new instance of the ImportFolderWatcher class
    /// </summary>
    /// <param name="logger">The logger instance for logging operations</param>
    /// <param name="pollingInterval">The polling interval for monitoring changes</param>
    public ImportFolderWatcher(ILogger<ImportFolderWatcher> logger, TimeSpan pollingInterval)
    {
        _logger = logger;
        _fileSystem = new FileSystem();
        _pollingInterval = pollingInterval;
        _processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _blacklistedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".partial",
            ".download",
            ".incomplete",
            ".part",
            ".tmp",
            ".cache",
            ".log"
        };
    }

    /// <summary>
    /// Gets a value indicating whether the watcher is currently monitoring files
    /// </summary>
    public bool IsWatching => _isWatching;

    /// <summary>
    /// Gets a value indicating whether the polling timer is active
    /// </summary>
    public bool IsPolling => _isPolling;

    /// <summary>
    /// Gets the list of processed files
    /// </summary>
    public IReadOnlyCollection<string> ProcessedFiles => _processedFiles;

    /// <summary>
    /// Gets the list of blacklisted file extensions
    /// </summary>
    public IReadOnlyCollection<string> BlacklistedExtensions => _blacklistedExtensions;

    /// <summary>
    /// Event triggered when a file is created
    /// </summary>
    public event EventHandler<string>? FileCreated;

    /// <summary>
    /// Event triggered when a file is changed
    /// </summary>
    public event EventHandler<string>? FileChanged;

    /// <summary>
    /// Event triggered when a file is deleted
    /// </summary>
    public event EventHandler<string>? FileDeleted;

    /// <summary>
    /// Event triggered when a download is completed
    /// </summary>
    public event EventHandler<DownloadCompletedEventArgs>? DownloadComplete;

    /// <summary>
    /// Event triggered when a file is being processed
    /// </summary>
    public event EventHandler<FileProcessingEventArgs>? FileProcessing;

    /// <summary>
    /// Event triggered when the import folder experiences an event
    /// </summary>
    public event EventHandler<ImportFolderEventArgs>? ImportFolderEvent;

    /// <summary>
    /// Starts watching the specified folder for file system changes
    /// </summary>
    /// <param name="folderPath">The path of the folder to watch</param>
    /// <param name="pollingIntervalSeconds">The polling interval in seconds</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task StartWatchingAsync(
        string folderPath,
        int pollingIntervalSeconds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));
        }

        try
        {
            _logger.LogInformation("Starting file watcher for folder: {FolderPath}", folderPath);

            // Ensure the folder exists
            if (!_fileSystem.Directory.Exists(folderPath))
            {
                _fileSystem.Directory.CreateDirectory(folderPath);
                _logger.LogInformation("Created folder: {FolderPath}", folderPath);
            }

            // Start the file system watcher
            await StartFileSystemWatcherAsync(folderPath, cancellationToken);

            // Start the polling timer
            await StartPollingTimerAsync(pollingIntervalSeconds, cancellationToken);

            _isWatching = true;
            _isPolling = true;

            ImportFolderEvent?.Invoke(this, new ImportFolderEventArgs(folderPath, ImportFolderEventType.Started));

            _logger.LogInformation("File watcher started successfully for: {FolderPath}", folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting file watcher for: {FolderPath}", folderPath);
            throw;
        }
    }

    /// <summary>
    /// Starts the file system watcher for the specified folder
    /// </summary>
    /// <param name="folderPath">The path of the folder to watch</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task StartFileSystemWatcherAsync(string folderPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            _fileSystemWatcher?.Dispose();

            _fileSystemWatcher = new FileSystemWatcher(folderPath)
            {
                Filter = "*.*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _fileSystemWatcher.Changed += OnFileSystemChanged;
            _fileSystemWatcher.Created += OnFileSystemCreated;
            _fileSystemWatcher.Deleted += OnFileSystemDeleted;
            _fileSystemWatcher.Renamed += OnFileSystemRenamed;
            _fileSystemWatcher.Error += OnFileSystemError;

            _logger.LogDebug("File system watcher initialized for: {FolderPath}", folderPath);
        }, cancellationToken);
    }

    /// <summary>
    /// Starts the polling timer for monitoring file changes
    /// </summary>
    /// <param name="intervalSeconds">The polling interval in seconds</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task StartPollingTimerAsync(int intervalSeconds, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            _pollingTimer?.Dispose();

            _pollingTimer = new Timer(
                OnPollingTimerTick,
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(intervalSeconds));

            _logger.LogDebug("Polling timer started with interval: {IntervalSeconds} seconds", intervalSeconds);
        }, cancellationToken);
    }

    /// <summary>
    /// Handles the polling timer tick event
    /// </summary>
    /// <param name="state">The state object</param>
    private void OnPollingTimerTick(object? state)
    {
        try
        {
            if (!_isWatching || _fileSystemWatcher == null)
            {
                return;
            }

            var folderPath = _fileSystemWatcher.Path;
            PollForChanges(folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during polling timer tick");
        }
    }

    /// <summary>
    /// Polls the specified folder for file changes
    /// </summary>
    /// <param name="folderPath">The path of the folder to poll</param>
    private void PollForChanges(string folderPath)
    {
        try
        {
            var files = _fileSystem.Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            foreach (var filePath in files)
            {
                ProcessFileIfNewOrChanged(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling for changes in: {FolderPath}", folderPath);
        }
    }

    /// <summary>
    /// Processes a file if it is new or has been changed
    /// </summary>
    /// <param name="filePath">The path of the file to process</param>
    private void ProcessFileIfNewOrChanged(string filePath)
    {
        if (_processedFiles.Contains(filePath))
        {
            return;
        }

        var fileInfo = _fileSystem.FileInfo.New(filePath);

        if (!fileInfo.Exists)
        {
            return;
        }

        // Check if the file is a download file
        if (IsDownloadFile(filePath))
        {
            FileProcessing?.Invoke(this, new FileProcessingEventArgs(
                filePath,
                FileProcessingStatus.Processing));
        }

        _processedFiles.Add(filePath);
    }

    /// <summary>
    /// Handles the file system changed event
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="eventArgs">The event arguments</param>
    private void OnFileSystemChanged(object sender, FileSystemEventArgs eventArgs)
    {
        _logger.LogTrace("File system changed: {FullPath}", eventArgs.FullPath);

        if (ShouldProcessFile(eventArgs.FullPath))
        {
            FileChanged?.Invoke(this, eventArgs.FullPath);
        }
    }

    /// <summary>
    /// Handles the file system created event
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="eventArgs">The event arguments</param>
    private void OnFileSystemCreated(object sender, FileSystemEventArgs eventArgs)
    {
        _logger.LogTrace("File system created: {FullPath}", eventArgs.FullPath);

        if (ShouldProcessFile(eventArgs.FullPath))
        {
            FileCreated?.Invoke(this, eventArgs.FullPath);
        }
    }

    /// <summary>
    /// Handles the file system deleted event
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="eventArgs">The event arguments</param>
    private void OnFileSystemDeleted(object sender, FileSystemEventArgs eventArgs)
    {
        _logger.LogTrace("File system deleted: {FullPath}", eventArgs.FullPath);

        if (_processedFiles.Remove(eventArgs.FullPath))
        {
            FileDeleted?.Invoke(this, eventArgs.FullPath);
        }
    }

    /// <summary>
    /// Handles the file system renamed event
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="eventArgs">The event arguments</param>
    private void OnFileSystemRenamed(object sender, RenamedEventArgs eventArgs)
    {
        _logger.LogTrace("File system renamed: {OldPath} -> {FullPath}", eventArgs.OldName, eventArgs.FullPath);

        if (ShouldProcessFile(eventArgs.FullPath))
        {
            FileChanged?.Invoke(this, eventArgs.FullPath);
        }
    }

    /// <summary>
    /// Handles the file system error event
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="eventArgs">The event arguments</param>
    private void OnFileSystemError(object sender, ErrorEventArgs eventArgs)
    {
        _logger.LogError(eventArgs.GetException(), "File system error occurred");

        ImportFolderEvent?.Invoke(this, new ImportFolderEventArgs(
            _fileSystemWatcher?.Path ?? string.Empty,
            ImportFolderEventType.Error));
    }

    /// <summary>
    /// Determines whether a file should be processed
    /// </summary>
    /// <param name="filePath">The path of the file to check</param>
    /// <returns>True if the file should be processed; otherwise, false</returns>
    private bool ShouldProcessFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // Skip blacklisted extensions
        if (_blacklistedExtensions.Contains(extension))
        {
            return true;
        }

        return !string.IsNullOrEmpty(extension);
    }

    /// <summary>
    /// Determines whether a file is a download file
    /// </summary>
    /// <param name="filePath">The path of the file to check</param>
    /// <returns>True if the file is a download file; otherwise, false</returns>
    private bool IsDownloadFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return BlacklistConfiguration.IsDownloadFile(filePath) ||
               _blacklistedExtensions.Contains(extension);
    }

    /// <summary>
    /// Stops watching the folder for file system changes
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task StopWatchingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping file watcher...");

            // Stop the file system watcher
            _fileSystemWatcher?.Dispose();
            _fileSystemWatcher = null;

            // Stop the polling timer
            _pollingTimer?.Dispose();
            _pollingTimer = null;

            _isWatching = false;
            _isPolling = false;

            ImportFolderEvent?.Invoke(this, new ImportFolderEventArgs(
                string.Empty,
                ImportFolderEventType.Stopped));

            _logger.LogInformation("File watcher stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping file watcher");
            throw;
        }
    }

    /// <summary>
    /// Handles the download completion event
    /// </summary>
    /// <param name="filePath">The path of the completed download</param>
    /// <param name="metadata">The download metadata</param>
    public void HandleDownloadCompletion(string filePath, DownloadMetadata metadata)
    {
        _logger.LogInformation("Download completed: {FilePath} (Progress: {ProgressPercentage}%)",
            filePath,
            metadata.ProgressPercentage);

        DownloadComplete?.Invoke(this, new DownloadCompletedEventArgs(filePath)
        {
            Metadata = metadata
        });
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
    /// </summary>
    public void Dispose()
    {
        StopWatchingAsync(CancellationToken.None).Wait();
        _logger.LogInformation("Import folder watcher disposed");
    }
}

/// <summary>
/// Interface for the Import Folder Watcher
/// </summary>
public interface IImportFolderWatcher : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the watcher is currently monitoring files
    /// </summary>
    bool IsWatching { get; }

    /// <summary>
    /// Gets a value indicating whether the polling timer is active
    /// </summary>
    bool IsPolling { get; }

    /// <summary>
    /// Gets the list of processed files
    /// </summary>
    IReadOnlyCollection<string> ProcessedFiles { get; }

    /// <summary>
    /// Gets the list of blacklisted file extensions
    /// </summary>
    IReadOnlyCollection<string> BlacklistedExtensions { get; }

    /// <summary>
    /// Event triggered when a file is created
    /// </summary>
    event EventHandler<string> FileCreated;

    /// <summary>
    /// Event triggered when a file is changed
    /// </summary>
    event EventHandler<string> FileChanged;

    /// <summary>
    /// Event triggered when a file is deleted
    /// </summary>
    event EventHandler<string> FileDeleted;

    /// <summary>
    /// Event triggered when a download is completed
    /// </summary>
    event EventHandler<DownloadCompletedEventArgs> DownloadComplete;

    /// <summary>
    /// Event triggered when a file is being processed
    /// </summary>
    event EventHandler<FileProcessingEventArgs> FileProcessing;

    /// <summary>
    /// Event triggered when the import folder experiences an event
    /// </summary>
    event EventHandler<ImportFolderEventArgs> ImportFolderEvent;

    /// <summary>
    /// Starts watching the specified folder for file system changes
    /// </summary>
    /// <param name="folderPath">The path of the folder to watch</param>
    /// <param name="pollingIntervalSeconds">The polling interval in seconds</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StartWatchingAsync(
        string folderPath,
        int pollingIntervalSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops watching the folder for file system changes
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StopWatchingAsync(CancellationToken cancellationToken = default);
}
