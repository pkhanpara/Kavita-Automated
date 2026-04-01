using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kavita.Models.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Kavita.Services.Import;

/// <summary>
/// Main service for managing the Kavita Importer functionality.
/// Handles file import, organization, and monitoring of the import folder.
/// </summary>
public class KavitaImporterService : IKavitaImporterService, IDisposable
{
    private readonly ILogger<KavitaImporterService> _logger;
    private readonly IDirectoryService _directoryService;
    private readonly ImportSettings _settings;
    private readonly FormatDetectorService _formatDetector;
    private readonly DirectoryStructureBuilder _directoryBuilder;
    private readonly ImportFolderWatcher _folderWatcher;
    private readonly Dictionary<string, ImportFileStatus> _importedFiles;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isInitialized;
    private bool _isMonitoring;
    private Task? _monitoringTask;

    /// <summary>
    /// Initializes a new instance of the KavitaImporterService class
    /// </summary>
    /// <param name="logger">The logger instance for logging operations</param>
    /// <param name="directoryService">The directory service for file system operations</param>
    public KavitaImporterService(
        ILogger<KavitaImporterService> logger,
        IDirectoryService directoryService)
    {
        _logger = logger;
        _directoryService = directoryService;
        _settings = new ImportSettings();
        _formatDetector = new FormatDetectorService();
        _directoryBuilder = new DirectoryStructureBuilder();
        _folderWatcher = new ImportFolderWatcher(logger);
        _importedFiles = new Dictionary<string, ImportFileStatus>(StringComparer.OrdinalIgnoreCase);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets a value indicating whether the service is initialized
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets a value indicating whether the service is currently monitoring the import folder
    /// </summary>
    public bool IsMonitoring => _isMonitoring;

    /// <summary>
    /// Gets the current import settings
    /// </summary>
    public ImportSettings Settings => _settings;

    /// <summary>
    /// Gets the list of imported files
    /// </summary>
    public IReadOnlyDictionary<string, ImportFileStatus> ImportedFiles => _importedFiles;

    /// <summary>
    /// Initializes the Kavita Importer service
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the initialization operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogInformation("Kavita Importer service is already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing Kavita Importer service...");

            // Validate and create import folder if it doesn't exist
            await EnsureImportFolderExistsAsync(cancellationToken);

            // Load preferred formats from configuration
            await LoadPreferredFormatsAsync(cancellationToken);

            // Initialize the directory structure
            await _directoryBuilder.InitializeAsync(_settings.ImportFolderPath, cancellationToken);

            // Start the folder watcher
            await StartMonitoringAsync(cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("Kavita Importer service initialized successfully");
            _logger.LogInformation(BlacklistConfiguration.GetConfigurationSummary());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Kavita Importer service");
            throw;
        }
    }

    /// <summary>
    /// Ensures that the import folder exists and creates it if necessary
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task EnsureImportFolderExistsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ImportFolderPath))
        {
            throw new InvalidOperationException("Import folder path is not configured");
        }

        if (!_directoryService.Exists(_settings.ImportFolderPath))
        {
            _logger.LogInformation("Creating import folder: {FolderPath}", _settings.ImportFolderPath);
            Directory.CreateDirectory(_settings.ImportFolderPath);
        }

        // Validate the import folder against blacklist patterns
        if (BlacklistConfiguration.IsFolderBlacklisted(_settings.ImportFolderPath))
        {
            _logger.LogWarning("Import folder is blacklisted. Consider adjusting configuration.");
        }

        _logger.LogDebug("Import folder validated: {FolderPath}", _settings.ImportFolderPath);
    }

    /// <summary>
    /// Loads the preferred file formats from configuration
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task LoadPreferredFormatsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading preferred file formats...");

        // Define default format priorities
        var defaultFormats = new List<FormatPriority>
        {
            new FormatPriority(MediaFormat.EPUB, 100, ".epub"),
            new FormatPriority(MediaFormat.PDF, 100, ".pdf"),
            new FormatPriority(MediaFormat.CBZ, 90, ".cbz"),
            new FormatPriority(MediaFormat.CBR, 90, ".cbr"),
            new FormatPriority(MediaFormat.PNG, 80, ".png"),
            new FormatPriority(MediaFormat.JPEG, 80, ".jpg", ".jpeg"),
            new FormatPriority(MediaFormat.WebP, 75, ".webp"),
            new FormatPriority(MediaFormat.GIF, 70, ".gif"),
            new FormatPriority(MediaFormat.AVIF, 70, ".avif"),
            new FormatPriority(MediaFormat.MOBI, 85, ".mobi"),
            new FormatPriority(MediaFormat.AZW3, 85, ".azw3"),
            new FormatPriority(MediaFormat.CB7, 85, ".cb7")
        };

        _settings.PreferredFormats = defaultFormats;
        _formatDetector.SetFormatPriorities(defaultFormats);

        _logger.LogInformation("Loaded {Count} preferred file formats", defaultFormats.Count);
    }

    /// <summary>
    /// Starts monitoring the import folder for changes
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the monitoring operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_isMonitoring)
        {
            _logger.LogInformation("Import folder monitoring is already active");
            return;
        }

        try
        {
            _logger.LogInformation("Starting import folder monitoring...");

            _folderWatcher.FileCreated += OnFileCreated;
            _folderWatcher.FileChanged += OnFileChanged;
            _folderWatcher.FileDeleted += OnFileDeleted;
            _folderWatcher.DownloadComplete += OnDownloadComplete;

            await _folderWatcher.StartWatchingAsync(_settings.ImportFolderPath, _settings.PollingIntervalSeconds, cancellationToken);

            _isMonitoring = true;
            _logger.LogInformation("Import folder monitoring started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting import folder monitoring");
            throw;
        }
    }

    /// <summary>
    /// Stops monitoring the import folder
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (!_isMonitoring)
        {
            _logger.LogInformation("Import folder monitoring is already stopped");
            return;
        }

        try
        {
            _logger.LogInformation("Stopping import folder monitoring...");

            await _folderWatcher.StopWatchingAsync(cancellationToken);

            _folderWatcher.FileCreated -= OnFileCreated;
            _folderWatcher.FileChanged -= OnFileChanged;
            _folderWatcher.FileDeleted -= OnFileDeleted;
            _folderWatcher.DownloadComplete -= OnDownloadComplete;

            _isMonitoring = false;
            _logger.LogInformation("Import folder monitoring stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping import folder monitoring");
            throw;
        }
    }

    /// <summary>
    /// Imports a file or folder into the Kavita library
    /// </summary>
    /// <param name="sourcePath">The path of the file or folder to import</param>
    /// <param name="cancellationToken">A token to cancel the import operation</param>
    /// <returns>An ImportResult containing the import operation details</returns>
    public async Task<ImportResult> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
        {
            return ImportResult.CreateFailure($"Source path does not exist: {sourcePath}");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var importId = Guid.NewGuid();

        try
        {
            _logger.LogInformation("Starting import operation for: {SourcePath}", sourcePath);

            var importFileStatus = await ProcessSourceAsync(sourcePath, importId, cancellationToken);

            if (importFileStatus == null)
            {
                return ImportResult.CreateFailure($"Failed to process source: {sourcePath}");
            }

            // Organize the imported file
            var targetPath = await _directoryBuilder.OrganizeFileAsync(
                importFileStatus,
                _settings.ImportFolderPath,
                _settings.TargetLibraryPath,
                cancellationToken);

            importFileStatus.TargetFolder = targetPath;
            importFileStatus.Status = ImportStatus.Completed;
            importFileStatus.Actions.Add($"Organized to: {targetPath}");

            // Track the imported file
            _importedFiles[importFileStatus.FilePath] = importFileStatus;

            var result = ImportResult.CreateSuccess(
                totalFiles: 1,
                importedFiles: 1,
                failedFiles: 0,
                skippedFiles: 0,
                $"Successfully imported {importFileStatus.FileName} to {targetPath}");

            result.ImportedFilesList.Add(importFileStatus);
            result.ExecutionTime = stopwatch.Elapsed;

            _logger.LogInformation("Import operation completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during import operation for: {SourcePath}", sourcePath);
            return ImportResult.CreateFailure(ex.Message);
        }
    }

    /// <summary>
    /// Processes a source file or folder and returns its import status
    /// </summary>
    /// <param name="sourcePath">The path of the source to process</param>
    /// <param name="importId">The unique identifier for the import operation</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The import status of the processed source</returns>
    private async Task<ImportFileStatus?> ProcessSourceAsync(
        string sourcePath,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(sourcePath);

        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Source file does not exist: {FilePath}", sourcePath);
            return null;
        }

        // Detect the file format
        var format = _formatDetector.DetectFormat(sourcePath);
        var priority = _formatDetector.GetFormatPriority(format);

        // Check if the file is a download file
        var isDownloadFile = BlacklistConfiguration.IsDownloadFile(sourcePath);
        var isComplete = BlacklistConfiguration.IsFileComplete(sourcePath);

        var importStatus = new ImportFileStatus
        {
            ImportId = importId,
            FilePath = sourcePath,
            FileName = fileInfo.Name,
            Format = format,
            FileSize = fileInfo.Length,
            CreatedAt = fileInfo.CreationTime,
            LastModified = fileInfo.LastWriteTime,
            Status = isComplete ? ImportStatus.Completed : ImportStatus.Downloading,
            Priority = priority,
            IsDownloadFile = isDownloadFile,
            IsComplete = isComplete,
            SourceFolder = Path.GetDirectoryName(sourcePath) ?? string.Empty,
            Actions = new List<string> { "File detected and validated" }
        };

        // Check if the file is blacklisted
        if (BlacklistConfiguration.IsFileBlacklisted(sourcePath))
        {
            _logger.LogDebug("File is blacklisted: {FileName}", fileInfo.Name);
            importStatus.Actions.Add("File is blacklisted");
        }

        // Process download files
        if (isDownloadFile && !isComplete)
        {
            importStatus.Status = ImportStatus.Downloading;
            importStatus.Actions.Add("File is being downloaded");
            await WaitForDownloadCompletionAsync(importStatus, cancellationToken);
        }

        return importStatus;
    }

    /// <summary>
    /// Waits for a download file to complete
    /// </summary>
    /// <param name="importStatus">The import status to wait for</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task WaitForDownloadCompletionAsync(
        ImportFileStatus importStatus,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Waiting for download completion: {FileName}", importStatus.FileName);

        var maxWaitTime = TimeSpan.FromMinutes(10);
        var startTime = DateTime.Now;

        while (!importStatus.IsComplete && (DateTime.Now - startTime) < maxWaitTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            importStatus.IsComplete = BlacklistConfiguration.IsFileComplete(importStatus.FilePath);

            if (!importStatus.IsComplete)
            {
                importStatus.Status = ImportStatus.Downloading;
                importStatus.Actions.Add("Download in progress...");
            }
        }

        if (importStatus.IsComplete)
        {
            importStatus.Status = ImportStatus.Completed;
            importStatus.Actions.Add("Download completed successfully");
            _logger.LogDebug("Download completed for: {FileName}", importStatus.FileName);
        }
        else
        {
            importStatus.Status = ImportStatus.Partial;
            importStatus.ErrorMessage = "Download file did not complete within the expected timeframe";
            _logger.LogWarning("Download partially completed for: {FileName}", importStatus.FileName);
        }
    }

    /// <summary>
    /// Handles file creation events from the import folder watcher
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="filePath">The path of the created file</param>
    private async Task OnFileCreated(object sender, string filePath)
    {
        _logger.LogTrace("File created event: {FilePath}", filePath);

        if (BlacklistConfiguration.IsFolderBlacklisted(filePath) ||
            BlacklistConfiguration.IsFileBlacklisted(filePath))
        {
            _logger.LogDebug("Blacklisted file created: {FilePath}", filePath);
            return;
        }

        var importId = Guid.NewGuid();
        var importStatus = await ProcessSourceAsync(filePath, importId, _cancellationTokenSource.Token);

        if (importStatus != null)
        {
            importStatus.Status = ImportStatus.Processing;
            importStatus.Actions.Add("File created and queued for processing");

            if (BlacklistConfiguration.IsDownloadFile(filePath))
            {
                importStatus.Status = ImportStatus.Downloading;
                importStatus.Actions.Add("File is a download file");
            }

            _importedFiles[filePath] = importStatus;
            _logger.LogInformation("Tracked new file: {FileName}", importStatus.FileName);
        }
    }

    /// <summary>
    /// Handles file change events from the import folder watcher
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="filePath">The path of the changed file</param>
    private async Task OnFileChanged(object sender, string filePath)
    {
        _logger.LogTrace("File changed event: {FilePath}", filePath);

        if (_importedFiles.TryGetValue(filePath, out var existingStatus))
        {
            var fileInfo = new FileInfo(filePath);
            existingStatus.LastModified = fileInfo.LastWriteTime;
            existingStatus.FileSize = fileInfo.Length;

            if (BlacklistConfiguration.IsDownloadFile(filePath))
            {
                existingStatus.Status = BlacklistConfiguration.IsFileComplete(filePath)
                    ? ImportStatus.Completed
                    : ImportStatus.Downloading;
            }

            _logger.LogDebug("Updated file status: {FileName}, Status: {Status}",
                existingStatus.FileName, existingStatus.Status);
        }
    }

    /// <summary>
    /// Handles file deletion events from the import folder watcher
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="filePath">The path of the deleted file</param>
    private async Task OnFileDeleted(object sender, string filePath)
    {
        _logger.LogTrace("File deleted event: {FilePath}", filePath);

        if (_importedFiles.Remove(filePath))
        {
            _logger.LogInformation("Removed file from tracking: {FileName}", filePath);
        }
    }

    /// <summary>
    /// Handles download completion events from the import folder watcher
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="filePath">The path of the completed download</param>
    private async Task OnDownloadComplete(object sender, string filePath)
    {
        _logger.LogInformation("Download completed: {FilePath}", filePath);

        if (_importedFiles.TryGetValue(filePath, out var downloadStatus))
        {
            downloadStatus.IsComplete = true;
            downloadStatus.Status = ImportStatus.Completed;
            downloadStatus.Actions.Add("Download completed");

            // Process the completed download
            var targetPath = await _directoryBuilder.OrganizeFileAsync(
                downloadStatus,
                _settings.ImportFolderPath,
                _settings.TargetLibraryPath,
                _cancellationTokenSource.Token);

            downloadStatus.TargetFolder = targetPath;
            _logger.LogInformation("Download organized to: {TargetPath}", targetPath);
        }
    }

    /// <summary>
    /// Gets the import statistics for the current session
    /// </summary>
    /// <returns>A dictionary containing import statistics</returns>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            { "TotalImportedFiles", _importedFiles.Count },
            { "IsMonitoring", _isMonitoring },
            { "IsInitialized", _isInitialized },
            { "ImportFolderPath", _settings.ImportFolderPath },
            { "TargetLibraryPath", _settings.TargetLibraryPath },
            { "PollingIntervalSeconds", _settings.PollingIntervalSeconds },
            { "MaxFileSizeMB", _settings.MaxFileSizeMB },
            { "PreferredFormatsCount", _settings.PreferredFormats.Count },
            { "BlacklistedFoldersCount", BlacklistConfiguration.GetFolderPatterns().Count },
            { "BlacklistedPatternsCount", BlacklistConfiguration.GetFilePatterns().Count },
            { "DownloadExtensionsCount", BlacklistConfiguration.GetDownloadExtensions().Count },
            { "ImportedFiles", _importedFiles.Values.Select(f => f.ToDictionary()).ToList() }
        };
    }

    /// <summary>
    /// Updates the import settings with new configuration values
    /// </summary>
    /// <param name="newSettings">The new settings to apply</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task UpdateSettingsAsync(ImportSettings newSettings, CancellationToken cancellationToken = default)
    {
        _settings.ImportFolderPath = newSettings.ImportFolderPath ?? _settings.ImportFolderPath;
        _settings.TargetLibraryPath = newSettings.TargetLibraryPath ?? _settings.TargetLibraryPath;
        _settings.EnableMonitoring = newSettings.EnableMonitoring;
        _settings.EnableOrganization = newSettings.EnableOrganization;
        _settings.EnableDuplicateDetection = newSettings.EnableDuplicateDetection;
        _settings.MaxFileSizeMB = newSettings.MaxFileSizeMB;
        _settings.PollingIntervalSeconds = newSettings.PollingIntervalSeconds;
        _settings.PreferredFormats = newSettings.PreferredFormats ?? _settings.PreferredFormats;
        _settings.BlacklistedFolders = newSettings.BlacklistedFolders ?? _settings.BlacklistedFolders;
        _settings.BlacklistedPatterns = newSettings.BlacklistedPatterns ?? _settings.BlacklistedPatterns;
        _settings.DownloadExtensions = newSettings.DownloadExtensions ?? _settings.DownloadExtensions;

        _logger.LogInformation("Import settings updated successfully");

        // Restart monitoring if settings have changed
        if (_isMonitoring && (newSettings.PollingIntervalSeconds != _settings.PollingIntervalSeconds ||
                              newSettings.ImportFolderPath != _settings.ImportFolderPath))
        {
            await StopMonitoringAsync(cancellationToken);
            await StartMonitoringAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        StopMonitoringAsync(CancellationToken.None).Wait();
        _logger.LogInformation("Kavita Importer service disposed");
    }
}

/// <summary>
/// Interface for the Kavita Importer service
/// </summary>
public interface IKavitaImporterService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the service is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets a value indicating whether the service is currently monitoring the import folder
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the current import settings
    /// </summary>
    ImportSettings Settings { get; }

    /// <summary>
    /// Gets the list of imported files
    /// </summary>
    IReadOnlyDictionary<string, ImportFileStatus> ImportedFiles { get; }

    /// <summary>
    /// Initializes the Kavita Importer service
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the initialization operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts monitoring the import folder for changes
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the monitoring operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring the import folder
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a file or folder into the Kavita library
    /// </summary>
    /// <param name="sourcePath">The path of the file or folder to import</param>
    /// <param name="cancellationToken">A token to cancel the import operation</param>
    /// <returns>An ImportResult containing the import operation details</returns>
    Task<ImportResult> ImportAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the import settings with new configuration values
    /// </summary>
    /// <param name="newSettings">The new settings to apply</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UpdateSettingsAsync(ImportSettings newSettings, CancellationToken cancellationToken = default);
}
