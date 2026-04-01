using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kavita.API.Controllers;
using Kavita.Models.Entities.Enums;
using Kavita.Services.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kavita.API.Controllers;

/// <summary>
/// Controller for managing the Kavita Importer functionality.
/// Provides REST API endpoints for import configuration, file management, and statistics.
/// </summary>
[ApiController]
[Route("api/v1/import")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly IKavitaImporterService _importerService;
    private readonly IFormatDetectorService _formatDetector;
    private readonly IDirectoryStructureBuilder _directoryBuilder;
    private readonly ILogger<ImportController> _logger;

    /// <summary>
    /// Initializes a new instance of the ImportController class
    /// </summary>
    /// <param name="importerService">The Kavita importer service</param>
    /// <param name="formatDetector">The format detector service</param>
    /// <param name="directoryBuilder">The directory structure builder</param>
    /// <param name="logger">The logger instance</param>
    public ImportController(
        IKavitaImporterService importerService,
        IFormatDetectorService formatDetector,
        IDirectoryStructureBuilder directoryBuilder,
        ILogger<ImportController> logger)
    {
        _importerService = importerService;
        _formatDetector = formatDetector;
        _directoryBuilder = directoryBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current import configuration
    /// </summary>
    /// <returns>The import configuration as an OkObjectResult</returns>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(ImportConfigurationDto), StatusCodes.Status200OK)]
    public ActionResult<ImportConfigurationDto> GetConfiguration()
    {
        try
        {
            var settings = _importerService.Settings;

            var configuration = new ImportConfigurationDto
            {
                Id = Guid.NewGuid(),
                Name = "Kavita Importer Configuration",
                Description = "Configuration for automatic import and organization of media files",
                ImportFolderPath = settings.ImportFolderPath,
                TargetLibraryPath = settings.TargetLibraryPath,
                EnableMonitoring = settings.EnableMonitoring,
                EnableOrganization = settings.EnableOrganization,
                EnableDuplicateDetection = settings.EnableDuplicateDetection,
                MaxFileSizeMB = settings.MaxFileSizeMB,
                PollingIntervalSeconds = settings.PollingIntervalSeconds,
                ProcessNewFilesOnly = settings.ProcessNewFilesOnly,
                PreferredFormats = settings.PreferredFormats.Select(fp => new FormatPreferenceDto
                {
                    Format = fp.Format,
                    Priority = fp.Priority,
                    Extensions = fp.Extensions,
                    IsPreferred = fp.IsPreferred
                }).ToList(),
                BlacklistedFolders = settings.BlacklistedFolders,
                BlacklistedPatterns = settings.BlacklistedPatterns,
                DownloadExtensions = settings.DownloadExtensions,
                EnableDetailedLogging = settings.EnableDetailedLogging,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving import configuration");
            return StatusCode.Status500InternalServerError;
        }
    }

    /// <summary>
    /// Updates the import configuration
    /// </summary>
    /// <param name="configuration">The import configuration to update</param>
    /// <returns>The updated configuration as an OkObjectResult</returns>
    [HttpPut("configuration")]
    [ProducesResponseType(typeof(ImportConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportConfigurationDto>> UpdateConfiguration([FromBody] ImportConfigurationDto configuration)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newSettings = new ImportSettings
            {
                ImportFolderPath = configuration.ImportFolderPath,
                TargetLibraryPath = configuration.TargetLibraryPath,
                EnableMonitoring = configuration.EnableMonitoring,
                EnableOrganization = configuration.EnableOrganization,
                EnableDuplicateDetection = configuration.EnableDuplicateDetection,
                MaxFileSizeMB = configuration.MaxFileSizeMB,
                PollingIntervalSeconds = configuration.PollingIntervalSeconds,
                ProcessNewFilesOnly = configuration.ProcessNewFilesOnly,
                PreferredFormats = configuration.PreferredFormats.Select(fp => new FormatPriority
                {
                    Format = fp.Format,
                    Priority = fp.Priority,
                    Extensions = fp.Extensions,
                    IsPreferred = fp.IsPreferred
                }).ToList(),
                BlacklistedFolders = configuration.BlacklistedFolders,
                BlacklistedPatterns = configuration.BlacklistedPatterns,
                DownloadExtensions = configuration.DownloadExtensions,
                EnableDetailedLogging = configuration.EnableDetailedLogging
            };

            await _importerService.UpdateSettingsAsync(newSettings);

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating import configuration");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to update configuration" });
        }
    }

    /// <summary>
    /// Imports files or folders into the Kavita library
    /// </summary>
    /// <param name="request">The import request containing file paths and options</param>
    /// <returns>The import result as an OkObjectResult</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> ImportFiles([FromBody] ImportFilesRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid || request.FilePaths == null || request.FilePaths.Count == 0)
            {
                return BadRequest(new { message = "Invalid request. Please provide valid file paths." });
            }

            var importTasks = request.FilePaths.Select(path => _importerService.ImportAsync(path));
            var results = await Task.WhenAll(importTasks);

            var totalImported = results.Sum(r => r.ImportedFiles);
            var totalFailed = results.Sum(r => r.FailedFiles);
            var totalSkipped = results.Sum(r => r.SkippedFiles);

            var importResult = new ImportResultDto
            {
                Success = results.All(r => r.Success),
                TotalFiles = results.Sum(r => r.TotalFiles),
                ImportedFiles = totalImported,
                FailedFiles = totalFailed,
                SkippedFiles = totalSkipped,
                ExecutionTime = results.Sum(r => r.ExecutionTime).ToString(@"mm\:ss\.fff"),
                Summary = $"Successfully imported {totalImported} files with {totalFailed} failures",
                ImportedFilesList = results.SelectMany(r => r.ImportedFilesList).ToList(),
                ErrorMessages = results.SelectMany(r => r.ErrorMessages).ToList()
            };

            return Ok(importResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing files");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to import files", error = ex.Message });
        }
    }

    /// <summary>
    /// Imports a specific file or folder
    /// </summary>
    /// <param name="filePath">The path of the file or folder to import</param>
    /// <returns>The import result as an OkObjectResult</returns>
    [HttpPost("import/{filePath}")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportResultDto>> ImportFile(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest(new { message = "File path is required" });
            }

            var result = await _importerService.ImportAsync(filePath);

            if (!result.Success)
            {
                return NotFound(new { message = "File not found or invalid", error = result.ErrorMessages });
            }

            return Ok(new ImportResultDto
            {
                Success = result.Success,
                TotalFiles = result.TotalFiles,
                ImportedFiles = result.ImportedFiles,
                FailedFiles = result.FailedFiles,
                SkippedFiles = result.SkippedFiles,
                ExecutionTime = result.ExecutionTime.ToString(@"mm\:ss\.fff"),
                Summary = result.Summary,
                ImportedFilesList = result.ImportedFilesList,
                ErrorMessages = result.ErrorMessages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing file: {FilePath}", filePath);
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to import file", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the import statistics
    /// </summary>
    /// <returns>The import statistics as an OkObjectResult</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ImportStatisticsDto), StatusCodes.Status200OK)]
    public ActionResult<ImportStatisticsDto> GetStatistics()
    {
        try
        {
            var statistics = _importerService.GetStatistics();

            var statsDto = new ImportStatisticsDto
            {
                TotalImportedFiles = Convert.ToInt32(statistics["TotalImportedFiles"]),
                TotalFilesProcessed = statistics.TryGetValue("TotalFilesProcessed", out var processed) ? Convert.ToInt32(processed) : 0,
                TotalFilesImported = statistics.TryGetValue("TotalFilesImported", out var imported) ? Convert.ToInt32(imported) : 0,
                TotalFilesFailed = statistics.TryGetValue("TotalFilesFailed", out var failed) ? Convert.ToInt32(failed) : 0,
                TotalFilesSkipped = statistics.TryGetValue("TotalFilesSkipped", out var skipped) ? Convert.ToInt32(skipped) : 0,
                TotalImportedSize = statistics.TryGetValue("TotalImportedSize", out var size) ? Convert.ToInt64(size) : 0,
                AverageProcessingTimeMs = statistics.TryGetValue("AverageProcessingTimeMs", out var time) ? Convert.ToDouble(time) : 0,
                PreferredFormats = statistics.TryGetValue("PreferredFormats", out var formats)
                    ? formats as List<FormatPreferenceDto> ?? new List<FormatPreferenceDto>()
                    : new List<FormatPreferenceDto>(),
                IsMonitoring = Convert.ToBoolean(statistics["IsMonitoring"]),
                IsInitialized = Convert.ToBoolean(statistics["IsInitialized"]),
                ImportFolderPath = statistics.TryGetValue("ImportFolderPath", out var importPath) ? importPath?.ToString() ?? string.Empty : string.Empty,
                TargetLibraryPath = statistics.TryGetValue("TargetLibraryPath", out var libraryPath) ? libraryPath?.ToString() ?? string.Empty : string.Empty,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(statsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving import statistics");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to retrieve statistics" });
        }
    }

    /// <summary>
    /// Starts the import folder monitoring
    /// </summary>
    /// <returns>A success response as an OkObjectResult</returns>
    [HttpPost("monitoring/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> StartMonitoring()
    {
        try
        {
            await _importerService.StartMonitoringAsync();

            _logger.LogInformation("Import monitoring started successfully");

            return Ok(new { message = "Import monitoring started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting import monitoring");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to start monitoring", error = ex.Message });
        }
    }

    /// <summary>
    /// Stops the import folder monitoring
    /// </summary>
    /// <returns>A success response as an OkObjectResult</returns>
    [HttpPost("monitoring/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> StopMonitoring()
    {
        try
        {
            await _importerService.StopMonitoringAsync();

            _logger.LogInformation("Import monitoring stopped successfully");

            return Ok(new { message = "Import monitoring stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping import monitoring");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to stop monitoring", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the list of blacklisted folders
    /// </summary>
    /// <returns>The list of blacklisted folders as an OkObjectResult</returns>
    [HttpGet("blacklist/folders")]
    [ProducesResponseType(typeof(List<BlacklistedFolderDto>), StatusCodes.Status200OK)]
    public ActionResult<List<BlacklistedFolderDto>> GetBlacklistedFolders()
    {
        try
        {
            var folders = BlacklistConfiguration.GetFolderPatterns()
                .Select(folder => new BlacklistedFolderDto
                {
                    FolderName = folder,
                    FolderPath = Path.Combine(_importerService.Settings.ImportFolderPath, folder),
                    IsBlacklisted = true,
                    BlacklistPattern = $"*{folder}*",
                    AddedAt = DateTime.UtcNow
                }).ToList();

            return Ok(folders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blacklisted folders");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to retrieve blacklisted folders" });
        }
    }

    /// <summary>
    /// Gets the list of blacklisted file patterns
    /// </summary>
    /// <returns>The list of blacklisted file patterns as an OkObjectResult</returns>
    [HttpGet("blacklist/patterns")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public ActionResult<List<string>> GetBlacklistedPatterns()
    {
        try
        {
            var patterns = BlacklistConfiguration.GetFilePatterns().ToList();

            return Ok(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blacklisted patterns");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to retrieve blacklisted patterns" });
        }
    }

    /// <summary>
    /// Gets the list of download extensions
    /// </summary>
    /// <returns>The list of download extensions as an OkObjectResult</returns>
    [HttpGet("extensions/download")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public ActionResult<List<string>> GetDownloadExtensions()
    {
        try
        {
            var extensions = BlacklistConfiguration.GetDownloadExtensions().ToList();

            return Ok(extensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving download extensions");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to retrieve download extensions" });
        }
    }

    /// <summary>
    /// Gets the list of supported media formats
    /// </summary>
    /// <returns>The list of supported media formats as an OkObjectResult</returns>
    [HttpGet("formats")]
    [ProducesResponseType(typeof(List<FormatPreferenceDto>), StatusCodes.Status200OK)]
    public ActionResult<List<FormatPreferenceDto>> GetSupportedFormats()
    {
        try
        {
            var formats = _formatDetector.GetFormatPriorities()
                .Select(fp => new FormatPreferenceDto
                {
                    Format = fp.Format,
                    Priority = fp.Priority,
                    Extensions = fp.Extensions,
                    IsPreferred = fp.IsPreferred
                }).ToList();

            return Ok(formats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supported formats");
            return StatusCode(StatusCodes.StatusInternalServerError, new { message = "Failed to retrieve supported formats" });
        }
    }
}
