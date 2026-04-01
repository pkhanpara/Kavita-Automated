using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kavita.Models.Entities.Enums;

namespace Kavita.Services.Import;

/// <summary>
/// Service for building and managing directory structures for the Kavita Importer.
/// Handles file organization, folder creation, and directory optimization.
/// </summary>
public class DirectoryStructureBuilder : IDirectoryStructureBuilder
{
    private readonly ILogger<DirectoryStructureBuilder> _logger;
    private readonly IDirectoryService _directoryService;
    private readonly Dictionary<string, DirectoryStructure> _directoryStructures;
    private readonly Dictionary<string, FileOrganizationRule> _organizationRules;

    /// <summary>
    /// Initializes a new instance of the DirectoryStructureBuilder class
    /// </summary>
    /// <param name="logger">The logger instance for logging operations</param>
    /// <param name="directoryService">The directory service for file system operations</param>
    public DirectoryStructureBuilder(
        ILogger<DirectoryStructureBuilder> logger,
        IDirectoryService directoryService)
    {
        _logger = logger;
        _directoryService = directoryService;
        _directoryStructures = new Dictionary<string, DirectoryStructure>(StringComparer.OrdinalIgnoreCase);
        _organizationRules = new Dictionary<string, FileOrganizationRule>(StringComparer.OrdinalIgnoreCase);

        InitializeOrganizationRules();
    }

    /// <summary>
    /// Initializes the organization rules for file management
    /// </summary>
    private void InitializeOrganizationRules()
    {
        // Define organization rules for different file types
        _organizationRules["eBook"] = new FileOrganizationRule
        {
            Name = "eBook",
            Description = "Organizes eBook files (EPUB, PDF, MOBI, AZW3)",
            SourcePatterns = new[] { "*.epub", "*.pdf", "*.mobi", "*.azw3", "*.azw" },
            TargetFolderPattern = "Books/{Format}/{FileName}",
            MoveOnComplete = true,
            CreateSubdirectories = true
        };

        _organizationRules["comic"] = new FileOrganizationRule
        {
            Name = "Comic",
            Description = "Organizes comic book files (CBZ, CBR, CB7)",
            SourcePatterns = new[] { "*.cbz", "*.cbr", "*.cb7" },
            TargetFolderPattern = "Comics/{Format}/{Series}/{Volume}",
            MoveOnComplete = true,
            CreateSubdirectories = true
        };

        _organizationRules["images"] = new FileOrganizationRule
        {
            Name = "Images",
            Description = "Organizes image files (PNG, JPEG, WebP, GIF, AVIF)",
            SourcePatterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.avif", "*.bmp", "*.svg" },
            TargetFolderPattern = "Images/{Category}/{Date}/{FileName}",
            MoveOnComplete = true,
            CreateSubdirectories = true
        };

        _organizationRules["downloads"] = new FileOrganizationRule
        {
            Name = "Downloads",
            Description = "Manages download files with partial and download extensions",
            SourcePatterns = new[] { "*.partial", "*.download", "*.incomplete", "*.part" },
            TargetFolderPattern = "Downloads/{Status}/{FileName}",
            MoveOnComplete = false,
            CreateSubdirectories = true
        };

        _organizationRules["temp"] = new FileOrganizationRule
        {
            Name = "Temporary",
            Description = "Handles temporary and cache files",
            SourcePatterns = new[] { "*.tmp", "*.temp", "*.cache", "*.log" },
            TargetFolderPattern = "Temp/{Type}/{Date}",
            MoveOnComplete = false,
            CreateSubdirectories = true
        };
    }

    /// <summary>
    /// Initializes the directory structure builder
    /// </summary>
    /// <param name="importFolderPath">The path of the import folder</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InitializeAsync(
        string importFolderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(importFolderPath))
        {
            throw new ArgumentException("Import folder path cannot be null or empty", nameof(importFolderPath));
        }

        try
        {
            _logger.LogDebug("Initializing directory structure for: {FolderPath}", importFolderPath);

            // Ensure the import folder exists
            if (!_directoryService.Exists(importFolderPath))
            {
                Directory.CreateDirectory(importFolderPath);
                _logger.LogInformation("Created import folder: {FolderPath}", importFolderPath);
            }

            // Create standard subdirectories
            await CreateStandardSubdirectoriesAsync(importFolderPath, cancellationToken);

            // Initialize directory structures for each organization rule
            foreach (var rule in _organizationRules.Values)
            {
                var structure = await BuildDirectoryStructureAsync(importFolderPath, rule, cancellationToken);
                _directoryStructures[rule.Name] = structure;
            }

            _logger.LogInformation("Directory structure initialized successfully for: {FolderPath}", importFolderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing directory structure for: {FolderPath}", importFolderPath);
            throw;
        }
    }

    /// <summary>
    /// Creates standard subdirectories within the import folder
    /// </summary>
    /// <param name="importFolderPath">The path of the import folder</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task CreateStandardSubdirectoriesAsync(
        string importFolderPath,
        CancellationToken cancellationToken)
    {
        var subdirectories = new[]
        {
            "Pending",
            "Processing",
            "Completed",
            "Archives",
            "Logs"
        };

        foreach (var subdirectory in subdirectories)
        {
            var subdirectoryPath = Path.Combine(importFolderPath, subdirectory);

            if (!_directoryService.Exists(subdirectoryPath))
            {
                Directory.CreateDirectory(subdirectoryPath);
                _logger.LogDebug("Created subdirectory: {SubdirectoryPath}", subdirectoryPath);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        _logger.LogDebug("Standard subdirectories created in: {FolderPath}", importFolderPath);
    }

    /// <summary>
    /// Builds a directory structure for a specific organization rule
    /// </summary>
    /// <param name="basePath">The base path for the directory structure</param>
    /// <param name="rule">The organization rule to build the structure for</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation that returns the directory structure</returns>
    private async Task<DirectoryStructure> BuildDirectoryStructureAsync(
        string basePath,
        FileOrganizationRule rule,
        CancellationToken cancellationToken)
    {
        var structure = new DirectoryStructure
        {
            Name = rule.Name,
            BasePath = basePath,
            Rules = new List<DirectoryRule>(),
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        // Create target directories based on the rule's pattern
        var targetBasePath = Path.Combine(basePath, "Organized", rule.Name);
        if (!_directoryService.Exists(targetBasePath))
        {
            Directory.CreateDirectory(targetBasePath);
            _logger.LogDebug("Created target directory for {RuleName}: {TargetPath}", rule.Name, targetBasePath);
        }

        structure.TargetBasePath = targetBasePath;

        // Create directory rules based on the organization pattern
        var rules = ParseOrganizationPattern(rule.TargetFolderPattern);
        structure.Rules.AddRange(rules);

        await SaveDirectoryStructureAsync(structure, cancellationToken);

        return structure;
    }

    /// <summary>
    /// Parses an organization pattern into directory rules
    /// </summary>
    /// <param name="pattern">The organization pattern to parse</param>
    /// <returns>A list of directory rules</returns>
    private List<DirectoryRule> ParseOrganizationPattern(string pattern)
    {
        var rules = new List<DirectoryRule>();
        var segments = pattern.Split('/');

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var propertyName = segment.Trim('{', '}');
                rules.Add(new DirectoryRule
                {
                    Placeholder = segment,
                    PropertyName = propertyName,
                    IsDynamic = true
                });
            }
            else
            {
                rules.Add(new DirectoryRule
                {
                    Name = segment,
                    IsDynamic = false
                });
            }
        }

        return rules;
    }

    /// <summary>
    /// Organizes a file within the directory structure
    /// </summary>
    /// <param name="importStatus">The import status of the file to organize</param>
    /// <param name="sourcePath">The source path of the file</param>
    /// <param name="targetLibraryPath">The target library path for organization</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation that returns the target path</returns>
    public async Task<string> OrganizeFileAsync(
        ImportFileStatus importStatus,
        string sourcePath,
        string targetLibraryPath,
        CancellationToken cancellationToken)
    {
        if (importStatus == null)
        {
            throw new ArgumentNullException(nameof(importStatus));
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path cannot be null or empty", nameof(sourcePath));
        }

        try
        {
            _logger.LogDebug("Organizing file: {FileName} (Format: {Format})",
                importStatus.FileName, importStatus.Format);

            // Determine the appropriate organization rule based on the file format
            var rule = GetOrganizationRuleForFormat(importStatus.Format);

            if (rule == null)
            {
                rule = _organizationRules.Values.First();
                _logger.LogDebug("Using default organization rule: {RuleName}", rule.Name);
            }

            // Build the target path based on the organization rule
            var targetPath = await BuildTargetPathAsync(
                importStatus,
                rule,
                sourcePath,
                targetLibraryPath,
                cancellationToken);

            // Move or copy the file to the target location
            var finalPath = await MoveFileToTargetAsync(
                sourcePath,
                targetPath,
                importStatus,
                cancellationToken);

            importStatus.TargetFolder = finalPath;
            _logger.LogDebug("File organized to: {TargetPath}", finalPath);

            return finalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error organizing file: {FileName}", importStatus?.FileName);
            throw;
        }
    }

    /// <summary>
    /// Gets the organization rule for a specific media format
    /// </summary>
    /// <param name="format">The media format to get the rule for</param>
    /// <returns>The organization rule, or null if not found</returns>
    private FileOrganizationRule? GetOrganizationRuleForFormat(MediaFormat format)
    {
        return _organizationRules.Values.FirstOrDefault(rule =>
            rule.SourcePatterns.Any(pattern =>
                pattern.StartsWith("*") && IsPatternMatchForFormat(pattern, format)) ||
            rule.Name.Equals(GetRuleNameForFormat(format), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a pattern matches a specific format
    /// </summary>
    /// <param name="pattern">The pattern to check</param>
    /// <param name="format">The format to match against</param>
    /// <returns>True if the pattern matches the format; otherwise, false</returns>
    private bool IsPatternMatchForFormat(string pattern, MediaFormat format)
    {
        var extension = GetExtensionForFormat(format);
        return pattern.Contains(extension);
    }

    /// <summary>
    /// Gets the file extension for a specific media format
    /// </summary>
    /// <param name="format">The media format to get the extension for</param>
    /// <returns>The file extension</returns>
    private string GetExtensionForFormat(MediaFormat format)
    {
        return format switch
        {
            MediaFormat.EPUB => ".epub",
            MediaFormat.PDF => ".pdf",
            MediaFormat.CBZ => ".cbz",
            MediaFormat.CBR => ".cbr",
            MediaFormat.MOBI => ".mobi",
            MediaFormat.AZW3 => ".azw3",
            MediaFormat.CB7 => ".cb7",
            MediaFormat.PNG => ".png",
            MediaFormat.JPEG => ".jpg",
            MediaFormat.WebP => ".webp",
            MediaFormat.GIF => ".gif",
            MediaFormat.BMP => ".bmp",
            MediaFormat.SVG => ".svg",
            MediaFormat.TIFF => ".tiff",
            MediaFormat.AVIF => ".avif",
            _ => ".epub"
        };
    }

    /// <summary>
    /// Gets the rule name for a specific media format
    /// </summary>
    /// <param name="format">The media format to get the rule name for</param>
    /// <returns>The rule name</returns>
    private string GetRuleNameForFormat(MediaFormat format)
    {
        return format switch
        {
            MediaFormat.EPUB or MediaFormat.PDF or MediaFormat.MOBI or MediaFormat.AZW3 => "eBook",
            MediaFormat.CBZ or MediaFormat.CBR or MediaFormat.CB7 => "comic",
            MediaFormat.PNG or MediaFormat.JPEG or MediaFormat.WebP or MediaFormat.GIF
                or MediaFormat.BMP or MediaFormat.SVG or MediaFormat.TIFF or MediaFormat.AVIF => "images",
            _ => "downloads"
        };
    }

    /// <summary>
    /// Builds the target path for a file based on the organization rule
    /// </summary>
    /// <param name="importStatus">The import status of the file</param>
    /// <param name="rule">The organization rule to use</param>
    /// <param name="sourcePath">The source path of the file</param>
    /// <param name="targetLibraryPath">The target library path</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation that returns the target path</returns>
    private async Task<string> BuildTargetPathAsync(
        ImportFileStatus importStatus,
        FileOrganizationRule rule,
        string sourcePath,
        string targetLibraryPath,
        CancellationToken cancellationToken)
    {
        var targetBasePath = Path.Combine(targetLibraryPath, "Organized", rule.Name);

        // Build the target directory path based on the organization pattern
        var targetDirectory = await BuildTargetDirectoryAsync(
            targetBasePath,
            importStatus,
            rule,
            cancellationToken);

        // Construct the full target path
        var targetPath = Path.Combine(
            targetDirectory,
            importStatus.FileName);

        return targetPath;
    }

    /// <summary>
    /// Builds the target directory for a file
    /// </summary>
    /// <param name="baseDirectory">The base directory for the target</param>
    /// <param name="importStatus">The import status of the file</param>
    /// <param name="rule">The organization rule to use</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation that returns the target directory</returns>
    private async Task<string> BuildTargetDirectoryAsync(
        string baseDirectory,
        ImportFileStatus importStatus,
        FileOrganizationRule rule,
        CancellationToken cancellationToken)
    {
        var directoryPath = baseDirectory;

        // Apply directory rules to build the path
        foreach (var directoryRule in rule.TargetDirectoryRules)
        {
            if (directoryRule.IsDynamic)
            {
                var subdirectory = await GetDynamicSubdirectoryAsync(
                    directoryRule.PropertyName,
                    importStatus,
                    directoryPath,
                    cancellationToken);

                directoryPath = Path.Combine(directoryPath, subdirectory);
            }
            else
            {
                directoryPath = Path.Combine(directoryPath, directoryRule.Name);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        // Ensure the target directory exists
        if (!_directoryService.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            _logger.LogDebug("Created target directory: {DirectoryPath}", directoryPath);
        }

        return directoryPath;
    }

    /// <summary>
    /// Gets the dynamic subdirectory based on the property name
    /// </summary>
    /// <param name="propertyName">The name of the property to use for the subdirectory</param>
    /// <param name="importStatus">The import status of the file</param>
    /// <param name="baseDirectory">The base directory</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation that returns the subdirectory name</returns>
    private async Task<string> GetDynamicSubdirectoryAsync(
        string propertyName,
        ImportFileStatus importStatus,
        string baseDirectory,
        CancellationToken cancellationToken)
    {
        return propertyName.ToLowerInvariant() switch
        {
            "format" => importStatus.Format.ToString(),
            "date" => DateTime.UtcNow.ToString("yyyy-MM"),
            "category" => GetCategoryForFormat(importStatus.Format),
            "status" => importStatus.Status.ToString(),
            "series" => GetSeriesName(importStatus),
            "volume" => GetVolumeName(importStatus),
            _ => importStatus.FileName
        };
    }

    /// <summary>
    /// Gets the category for a specific media format
    /// </summary>
    /// <param name="format">The media format to get the category for</param>
    /// <returns>The category name</returns>
    private string GetCategoryForFormat(MediaFormat format)
    {
        return format switch
        {
            MediaFormat.EPUB or MediaFormat.PDF or MediaFormat.MOBI or MediaFormat.AZW3 => "Books",
            MediaFormat.CBZ or MediaFormat.CBR or MediaFormat.CB7 => "Comics",
            MediaFormat.PNG or MediaFormat.JPEG or MediaFormat.WebP or MediaFormat.GIF
                or MediaFormat.BMP or MediaFormat.SVG or MediaFormat.TIFF or MediaFormat.AVIF => "Images",
            _ => "General"
        };
    }

    /// <summary>
    /// Gets the series name for an import status
    /// </summary>
    /// <param name="importStatus">The import status to get the series name for</param>
    /// <returns>The series name</returns>
    private string GetSeriesName(ImportFileStatus importStatus)
    {
        return !string.IsNullOrWhiteSpace(importStatus.SourceFolder)
            ? Path.GetFileName(importStatus.SourceFolder)
            : importStatus.Format.ToString();
    }

    /// <summary>
    /// Gets the volume name for an import status
    /// </summary>
    /// <param name="importStatus">The import status to get the volume name for</param>
    /// <returns>The volume name</returns>
    private string GetVolumeName(ImportFileStatus importStatus)
    {
        return importStatus.Priority switch
        {
            >= 95 => "Premium",
            >= 85 => "Standard",
            >= 70 => "Basic",
            _ => "General"
        };
    }

    /// <summary>
    /// Moves or copies a file to the target location
    /// </summary>
    /// <param name="sourcePath">The source path of the file</param>
    /// <param name="targetPath">The target path for the file</param>
    /// <param name="importStatus">The import status of the file</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation that returns the final path</returns>
    private async Task<string> MoveFileToTargetAsync(
        string sourcePath,
        string targetPath,
        ImportFileStatus importStatus,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(sourcePath);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        }

        // Ensure the target directory exists
        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory) && !_directoryService.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        // Move or copy the file based on the organization rule
        if (fileInfo.FullName != targetPath && _directoryService.Exists(targetPath))
        {
            var targetFileInfo = new FileInfo(targetPath);

            // Handle potential file name conflicts
            if (fileInfo.Name.Equals(targetFileInfo.Name, StringComparison.OrdinalIgnoreCase))
            {
                targetPath = ResolveFileNameConflict(targetPath);
            }
        }

        // Move the file to the target location
        if (fileInfo.FullName != targetPath)
        {
            await Task.Run(() =>
            {
                fileInfo.CopyTo(targetPath, overwrite: true);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
            }, cancellationToken);

            _logger.LogDebug("File moved to: {TargetPath}", targetPath);
        }

        return targetPath;
    }

    /// <summary>
    /// Resolves file name conflicts by appending a timestamp or counter
    /// </summary>
    /// <param name="existingPath">The existing file path</param>
    /// <returns>The resolved file path with a unique name</returns>
    private string ResolveFileNameConflict(string existingPath)
    {
        var directory = Path.GetDirectoryName(existingPath);
        var fileName = Path.GetFileNameWithoutExtension(existingPath);
        var extension = Path.GetExtension(existingPath);
        var counter = 1;

        while (File.Exists(existingPath))
        {
            var newFileName = $"{fileName}_{counter}{extension}";
            existingPath = Path.Combine(directory ?? string.Empty, newFileName);
            counter++;
        }

        return existingPath;
    }

    /// <summary>
    /// Saves the directory structure to storage
    /// </summary>
    /// <param name="structure">The directory structure to save</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task SaveDirectoryStructureAsync(
        DirectoryStructure structure,
        CancellationToken cancellationToken)
    {
        try
        {
            // Serialize and save the directory structure
            var structureFile = Path.Combine(
                structure.TargetBasePath,
                $"{structure.Name.ToLowerInvariant()}_structure.json");

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(
                structure,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            await File.WriteAllTextAsync(structureFile, jsonContent, cancellationToken);

            _logger.LogDebug("Saved directory structure: {StructureFile}", structureFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving directory structure: {StructureName}", structure.Name);
            throw;
        }
    }
}

/// <summary>
/// Interface for the Directory Structure Builder
/// </summary>
public interface IDirectoryStructureBuilder
{
    /// <summary>
    /// Initializes the directory structure builder
    /// </summary>
    /// <param name="importFolderPath">The path of the import folder</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeAsync(string importFolderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Organizes a file within the directory structure
    /// </summary>
    /// <param name="importStatus">The import status of the file to organize</param>
    /// <param name="sourcePath">The source path of the file</param>
    /// <param name="targetLibraryPath">The target library path for organization</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation that returns the target path</returns>
    Task<string> OrganizeFileAsync(
        ImportFileStatus importStatus,
        string sourcePath,
        string targetLibraryPath,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents a directory structure for file organization
/// </summary>
public class DirectoryStructure
{
    /// <summary>
    /// Gets or sets the name of the directory structure
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base path of the directory structure
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target base path of the directory structure
    /// </summary>
    public string TargetBasePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of directory rules
    /// </summary>
    public List<DirectoryRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the structure was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the structure was last modified
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets the total number of files in the directory structure
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Gets the total size of the directory structure in bytes
    /// </summary>
    public long TotalSize { get; set; }
}

/// <summary>
/// Represents a directory rule within a directory structure
/// </summary>
public class DirectoryRule
{
    /// <summary>
    /// Gets or sets the name of the directory rule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the placeholder pattern for dynamic directories
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name for dynamic directories
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the directory rule is dynamic
    /// </summary>
    public bool IsDynamic { get; set; }

    /// <summary>
    /// Gets or sets the target directory path
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;
}

/// <summary>
/// Represents an organization rule for file management
/// </summary>
public class FileOrganizationRule
{
    /// <summary>
    /// Gets or sets the name of the organization rule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the organization rule
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source file patterns
    /// </summary>
    public string[] SourcePatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the target folder pattern
    /// </summary>
    public string TargetFolderPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to move files on completion
    /// </summary>
    public bool MoveOnComplete { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create subdirectories
    /// </summary>
    public bool CreateSubdirectories { get; set; }

    /// <summary>
    /// Gets or sets the list of target directory rules
    /// </summary>
    public List<DirectoryRule> TargetDirectoryRules { get; set; } = new();
}
