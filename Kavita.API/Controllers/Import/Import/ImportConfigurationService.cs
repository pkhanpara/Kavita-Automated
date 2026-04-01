using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kavita.Models.Entities.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kavita.Services.Import;

/// <summary>
/// Service for managing import configuration and settings integration
/// </summary>
public class ImportConfigurationService : IImportConfigurationService, IDisposable
{
    private readonly ILogger<ImportConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IKavitaImporterService _importerService;
    private readonly ImportSettings _settings;
    private ServerSettingKey _settingKey;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the ImportConfigurationService class
    /// </summary>
    /// <param name="logger">The logger instance for logging operations</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="importerService">The Kavita importer service</param>
    public ImportConfigurationService(
        ILogger<ImportConfigurationService> logger,
        IConfiguration configuration,
        IKavitaImporterService importerService)
    {
        _logger = logger;
        _configuration = configuration;
        _importerService = importerService;
        _settings = new ImportSettings();
        _settingKey = ServerSettingKey.ImportConfiguration;
    }

    /// <summary>
    /// Gets a value indicating whether the service is initialized
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the current import settings
    /// </summary>
    public ImportSettings Settings => _settings;

    /// <summary>
    /// Initializes the import configuration service
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogInformation("Import configuration service is already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing import configuration service...");

            // Load configuration from appsettings.json
            await LoadConfigurationAsync(cancellationToken);

            // Initialize the importer service
            await _importerService.InitializeAsync(cancellationToken);

            // Register the configuration setting
            await RegisterConfigurationSettingAsync(cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("Import configuration service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing import configuration service");
            throw;
        }
    }

    /// <summary>
    /// Loads the import configuration from the application configuration
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Loading import configuration from application settings...");

            // Load import settings section
            var importSection = _configuration.GetSection("Import");

            if (importSection.Exists())
            {
                // Load import folder settings
                _settings.ImportFolderPath = importSection.GetValue<string>("ImportFolderPath")
                    ?? _settings.ImportFolderPath;

                _settings.TargetLibraryPath = importSection.GetValue<string>("TargetLibraryPath")
                    ?? _settings.TargetLibraryPath;

                // Load general settings
                _settings.EnableMonitoring = importSection.GetValue("EnableMonitoring", _settings.EnableMonitoring);
                _settings.EnableOrganization = importSection.GetValue("EnableOrganization", _settings.EnableOrganization);
                _settings.EnableDuplicateDetection = importSection.GetValue("EnableDuplicateDetection", _settings.EnableDuplicateDetection);
                _settings.MaxFileSizeMB = importSection.GetValue("MaxFileSizeMB", _settings.MaxFileSizeMB);
                _settings.PollingIntervalSeconds = importSection.GetValue("PollingIntervalSeconds", _settings.PollingIntervalSeconds);
                _settings.ProcessNewFilesOnly = importSection.GetValue("ProcessNewFilesOnly", _settings.ProcessNewFilesOnly);
                _settings.EnableDetailedLogging = importSection.GetValue("EnableDetailedLogging", _settings.EnableDetailedLogging);

                // Load preferred formats
                var preferredFormatsSection = importSection.GetSection("PreferredFormats");
                if (preferredFormatsSection.Exists())
                {
                    await LoadPreferredFormatsAsync(preferredFormatsSection, cancellationToken);
                }

                // Load blacklisted folders
                var blacklistedFoldersSection = importSection.GetSection("BlacklistedFolders");
                if (blacklistedFoldersSection.Exists())
                {
                    _settings.BlacklistedFolders = blacklistedFoldersSection.GetChildren()
                        .Select(section => section.GetValue<string>("FolderName"))
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToList();
                }

                // Load blacklisted patterns
                var blacklistedPatternsSection = importSection.GetSection("BlacklistedPatterns");
                if (blacklistedPatternsSection.Exists())
                {
                    _settings.BlacklistedPatterns = blacklistedPatternsSection.GetChildren()
                        .Select(section => section.GetValue<string>("Pattern"))
                        .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                        .ToList();
                }

                // Load download extensions
                var downloadExtensionsSection = importSection.GetSection("DownloadExtensions");
                if (downloadExtensionsSection.Exists())
                {
                    _settings.DownloadExtensions = downloadExtensionsSection.GetChildren()
                        .Select(section => section.GetValue<string>("Extension"))
                        .Where(extension => !string.IsNullOrWhiteSpace(extension))
                        .ToList();
                }
            }
            else
            {
                _logger.LogWarning("Import configuration section not found in application settings. Using default configuration.");
                await LoadDefaultConfigurationAsync(cancellationToken);
            }

            _logger.LogInformation("Import configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading import configuration");
            throw;
        }
    }

    /// <summary>
    /// Loads preferred formats from the configuration section
    /// </summary>
    /// <param name="preferredFormatsSection">The configuration section containing preferred formats</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task LoadPreferredFormatsAsync(
        Microsoft.Extensions.Configuration.IConfigurationSection preferredFormatsSection,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading preferred formats from configuration...");

        var formats = new List<FormatPriority>();

        foreach (var formatSection in preferredFormatsSection.GetChildren())
        {
            var formatName = formatSection.GetValue<string>("Format");
            if (!Enum.TryParse<MediaFormat>(formatName, true, out var format))
            {
                continue;
            }

            var priority = formatSection.GetValue("Priority", 80);
            var extensions = formatSection.GetSection("Extensions")
                .GetChildren()
                .Select(section => section.GetValue<string>("Extension"))
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .ToList();

            formats.Add(new FormatPriority
            {
                Format = format,
                Priority = priority,
                Extensions = extensions,
                IsPreferred = priority >= 85
            });
        }

        _settings.PreferredFormats = formats;
        _logger.LogInformation("Loaded {Count} preferred formats", formats.Count);
    }

    /// <summary>
    /// Loads the default configuration when no configuration section is found
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task LoadDefaultConfigurationAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading default import configuration...");

        // Define default import folder path
        var defaultImportFolderPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Import");

        var defaultLibraryPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Library");

        _settings.ImportFolderPath = defaultImportFolderPath;
        _settings.TargetLibraryPath = defaultLibraryPath;

        // Load default preferred formats
        var defaultFormats = new List<FormatPriority>
        {
            new FormatPriority(MediaFormat.EPUB, 100, ".epub"),
            new FormatPriority(MediaFormat.PDF, 100, ".pdf"),
            new FormatPriority(MediaFormat.CBZ, 90, ".cbz"),
            new FormatPriority(MediaFormat.CBR, 90, ".cbr"),
            new FormatPriority(MediaFormat.MOBI, 85, ".mobi"),
            new FormatPriority(MediaFormat.AZW3, 85, ".azw3"),
            new FormatPriority(MediaFormat.PNG, 80, ".png"),
            new FormatPriority(MediaFormat.JPEG, 80, ".jpg", ".jpeg"),
            new FormatPriority(MediaFormat.WebP, 75, ".webp"),
            new FormatPriority(MediaFormat.GIF, 70, ".gif"),
            new FormatPriority(MediaFormat.AVIF, 70, ".avif")
        };

        _settings.PreferredFormats = defaultFormats;

        // Load default download extensions
        _settings.DownloadExtensions = new List<string>
        {
            ".partial",
            ".download",
            ".incomplete",
            ".part"
        };

        _logger.LogInformation("Default configuration loaded successfully");
    }

    /// <summary>
    /// Registers the configuration setting in the server settings
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task RegisterConfigurationSettingAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Registering import configuration setting...");

            // Create a setting key for import configuration
            var settingKey = ServerSettingKey.ImportConfiguration;

            // Serialize the settings to JSON
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(
                _settings,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            // Create or update the setting
            var settingValue = new ServerSetting
            {
                Key = settingKey.ToString(),
                Value = jsonContent,
                Description = "Import configuration settings for Kavita Importer",
                LastModified = DateTime.UtcNow
            };

            // Store the setting (in a real implementation, this would be persisted to a database)
            await StoreSettingAsync(settingValue, cancellationToken);

            _logger.LogInformation("Import configuration setting registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering import configuration setting");
            throw;
        }
    }

    /// <summary>
    /// Stores the setting asynchronously
    /// </summary>
    /// <param name="settingValue">The setting value to store</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task StoreSettingAsync(ServerSetting settingValue, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            // In a real implementation, this would persist the setting to a database
            // For now, we'll just log the setting
            _logger.LogInformation("Stored setting: {Key} = {Value}",
                settingValue.Key,
                settingValue.Value?.Length > 0 ? $"{settingValue.Value.Length} characters" : "null");
        }, cancellationToken);
    }

    /// <summary>
    /// Updates the import settings and persists them
    /// </summary>
    /// <param name="newSettings">The new settings to update</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task UpdateSettingsAsync(ImportSettings newSettings, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating import settings...");

            // Update the settings
            _settings.ImportFolderPath = newSettings.ImportFolderPath ?? _settings.ImportFolderPath;
            _settings.TargetLibraryPath = newSettings.TargetLibraryPath ?? _settings.TargetLibraryPath;
            _settings.EnableMonitoring = newSettings.EnableMonitoring;
            _settings.EnableOrganization = newSettings.EnableOrganization;
            _settings.EnableDuplicateDetection = newSettings.EnableDuplicateDetection;
            _settings.MaxFileSizeMB = newSettings.MaxFileSizeMB;
            _settings.PollingIntervalSeconds = newSettings.PollingIntervalSeconds;
            _settings.ProcessNewFilesOnly = newSettings.ProcessNewFilesOnly;
            _settings.PreferredFormats = newSettings.PreferredFormats ?? _settings.PreferredFormats;
            _settings.BlacklistedFolders = newSettings.BlacklistedFolders ?? _settings.BlacklistedFolders;
            _settings.BlacklistedPatterns = newSettings.BlacklistedPatterns ?? _settings.BlacklistedPatterns;
            _settings.DownloadExtensions = newSettings.DownloadExtensions ?? _settings.DownloadExtensions;
            _settings.EnableDetailedLogging = newSettings.EnableDetailedLogging;

            // Persist the updated settings
            await PersistSettingsAsync(cancellationToken);

            // Update the importer service with the new settings
            await _importerService.UpdateSettingsAsync(_settings, cancellationToken);

            _logger.LogInformation("Import settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating import settings");
            throw;
        }
    }

    /// <summary>
    /// Persists the current settings to storage
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PersistSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Persisting import settings...");

            // Serialize the settings to JSON
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(
                _settings,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            // Update the configuration
            _configuration["Import:Configuration"] = jsonContent;

            // Persist to file (in a real implementation, this would be persisted to a database)
            var settingsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config",
                "import-settings.json");

            await File.WriteAllTextAsync(
                settingsFilePath,
                jsonContent,
                System.Text.Encoding.UTF8,
                cancellationToken);

            _logger.LogInformation("Import settings persisted to: {SettingsFilePath}", settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting import settings");
            throw;
        }
    }

    /// <summary>
    /// Gets the import configuration as JSON
    /// </summary>
    /// <returns>The import configuration as a JSON string</returns>
    public string GetConfigurationJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(
            _settings,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("Import configuration service disposed");
    }
}

/// <summary>
/// Interface for the Import Configuration Service
/// </summary>
public interface IImportConfigurationService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the service is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the current import settings
    /// </summary>
    ImportSettings Settings { get; }

    /// <summary>
    /// Initializes the import configuration service
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the import settings and persists them
    /// </summary>
    /// <param name="newSettings">The new settings to update</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UpdateSettingsAsync(ImportSettings newSettings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a server setting for configuration management
/// </summary>
public class ServerSetting
{
    /// <summary>
    /// Gets or sets the key of the server setting
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the server setting
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the description of the server setting
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the last modified date of the server setting
    /// </summary>
    public DateTime LastModified { get; set; }
}

// Add the new setting key to ServerSettingKey enum
public static class ServerSettingKeyExtensions
{
    public static readonly ServerSettingKey ImportConfiguration = (ServerSettingKey)43;
}
