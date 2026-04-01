using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Kavita.API.Controllers.Import.Import;

namespace Kavita.API.Services;

/// <summary>
/// Service for managing file system operations including directory and file management
/// </summary>
public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the DirectoryService class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="fileSystem">The file system abstraction</param>
    public DirectoryService(ILogger<DirectoryService> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Gets the underlying file system
    /// </summary>
    public IFileSystem FileSystem => _fileSystem;

    /// <summary>
    /// Gets or sets the configuration directory path
    /// </summary>
    public string ConfigDirectory { get; set; } = "config";

    /// <summary>
    /// Gets or sets the temporary directory path
    /// </summary>
    public string TempDirectory { get; set; } = "temp";

    /// <summary>
    /// Creates a directory if it doesn't exist
    /// </summary>
    /// <param name="path">The directory path to create</param>
    /// <returns>True if the directory was created or already exists</returns>
    public bool ExistOrCreate(string path)
    {
        try
        {
            if (!_fileSystem.Directory.Exists(path))
            {
                _fileSystem.Directory.CreateDirectory(path);
                _logger.LogInformation("Created directory: {Path}", path);
                return true;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating directory: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Copies a file to a destination directory
    /// </summary>
    /// <param name="sourceFilePath">The source file path</param>
    /// <param name="destinationDirectory">The destination directory path</param>
    /// <returns>True if the file was copied successfully</returns>
    public bool CopyFileToDirectory(string sourceFilePath, string destinationDirectory)
    {
        try
        {
            if (!_fileSystem.File.Exists(sourceFilePath))
            {
                _logger.LogWarning("Source file not found: {FilePath}", sourceFilePath);
                return false;
            }

            ExistOrCreate(destinationDirectory);

            var fileName = _fileSystem.Path.GetFileName(sourceFilePath);
            var destinationPath = _fileSystem.Path.Combine(destinationDirectory, fileName);

            _fileSystem.File.Copy(sourceFilePath, destinationPath, true);
            _logger.LogInformation("Copied file: {FileName} to {DestinationDirectory}", fileName, destinationDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file: {FilePath}", sourceFilePath);
            return false;
        }
    }

    /// <summary>
    /// Deletes a directory and all its contents
    /// </summary>
    /// <param name="directoryPath">The directory path to delete</param>
    /// <param name="recursive">Whether to delete recursively</param>
    /// <returns>True if the directory was deleted successfully</returns>
    public bool DeleteDirectory(string directoryPath, bool recursive = true)
    {
        try
        {
            if (_fileSystem.Directory.Exists(directoryPath))
            {
                _fileSystem.Directory.Delete(directoryPath, recursive);
                _logger.LogInformation("Deleted directory: {DirectoryPath}", directoryPath);
                return true;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting directory: {DirectoryPath}", directoryPath);
            return false;
        }
    }

    /// <summary>
    /// Gets a list of files in a directory matching a pattern
    /// </summary>
    /// <param name="directoryPath">The directory path</param>
    /// <param name="searchPattern">The file search pattern</param>
    /// <param name="searchOption">The search option</param>
    /// <returns>A collection of file paths</returns>
    public IReadOnlyCollection<string> GetFiles(string directoryPath, string searchPattern, SearchOption searchOption)
    {
        try
        {
            if (!_fileSystem.Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory not found: {DirectoryPath}", directoryPath);
                return Array.Empty<string>();
            }

            var files = _fileSystem.Directory.GetFiles(directoryPath, searchPattern, searchOption).ToList();
            _logger.LogDebug("Found {Count} files in {DirectoryPath}", files.Count, directoryPath);
            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files from directory: {DirectoryPath}", directoryPath);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Moves a file to a new location
    /// </summary>
    /// <param name="sourceFilePath">The source file path</param>
    /// <param name="destinationFilePath">The destination file path</param>
    /// <returns>True if the file was moved successfully</returns>
    public bool MoveFile(string sourceFilePath, string destinationFilePath)
    {
        try
        {
            if (!_fileSystem.File.Exists(sourceFilePath))
            {
                _logger.LogWarning("Source file not found: {FilePath}", sourceFilePath);
                return false;
            }

            ExistOrCreate(_fileSystem.Path.GetDirectoryName(destinationFilePath));

            _fileSystem.File.Move(sourceFilePath, destinationFilePath);
            _logger.LogInformation("Moved file: {SourceFilePath} to {DestinationFilePath}", sourceFilePath, destinationFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving file: {SourceFilePath}", sourceFilePath);
            return false;
        }
    }

    /// <summary>
    /// Updates the last write time of a file
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <returns>True if the file was updated successfully</returns>
    public bool UpdateFileTimestamp(string filePath)
    {
        try
        {
            if (_fileSystem.File.Exists(filePath))
            {
                _fileSystem.File.SetLastWriteTime(filePath, DateTime.UtcNow);
                _logger.LogDebug("Updated timestamp for file: {FilePath}", filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file timestamp: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Gets the file size of a file
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <returns>The file size in bytes</returns>
    public long GetFileSize(string filePath)
    {
        try
        {
            if (_fileSystem.File.Exists(filePath))
            {
                var fileInfo = _fileSystem.FileInfo.New(filePath);
                return fileInfo.Length;
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file size: {FilePath}", filePath);
            return 0;
        }
    }

    /// <summary>
    /// Reads the contents of a file as a string
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <returns>The file contents as a string</returns>
    public async Task<string> ReadFileAsStringAsync(string filePath)
    {
        try
        {
            if (_fileSystem.File.Exists(filePath))
            {
                var content = await _fileSystem.File.ReadAllTextAsync(filePath);
                _logger.LogDebug("Read file: {FilePath}", filePath);
                return content;
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Writes content to a file
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <param name="content">The content to write</param>
    /// <returns>True if the file was written successfully</returns>
    public async Task<bool> WriteFileAsStringAsync(string filePath, string content)
    {
        try
        {
            ExistOrCreate(_fileSystem.Path.GetDirectoryName(filePath));
            await _fileSystem.File.WriteAllTextAsync(filePath, content);
            _logger.LogDebug("Written file: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing file: {FilePath}", filePath);
            return false;
        }
    }
}
