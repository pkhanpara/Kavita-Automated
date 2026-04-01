# Kavita Importer - Comprehensive Implementation Plan

## Executive Summary

The Kavita Importer is a strategic enhancement that automates the import of diverse eBook formats into Kavita's library. This feature addresses user challenges in managing files from various sources by providing intelligent file detection, format-based organization, and seamless integration with existing library structures.

**Key Value Proposition:**
- Eliminates manual file organization efforts
- Supports multi-format imports (EPUB, PDF, CBZ, CBR, images)
- Provides automated directory structure creation
- Enables intelligent format preference management
- Offers real-time monitoring of import directories

---

## 1. Problem Analysis

### 1.1 Current Challenges

Based on user feedback and system analysis, Kavita users face several challenges:

1. **Format Diversity**: Users import files from multiple sources (downloads, cloud storage, external devices) in various formats
2. **Directory Organization**: Manual organization of files into Kavita's expected structure is time-consuming
3. **Naming Conflicts**: Multiple files with identical names but different formats require intelligent handling
4. **Metadata Management**: Extracting and applying metadata during import is often overlooked
5. **Scalability**: Large import operations require efficient processing to maintain performance

### 1.2 User Pain Points

- **Time Investment**: Manual file organization can take hours for large collections
- **Error Prevention**: Risk of misplacing files or creating incorrect directory structures
- **Format Compatibility**: Ensuring all file types are properly recognized and processed
- **Consistency**: Maintaining uniform naming conventions across the library

---

## 2. Solution Architecture

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Kavita Importer System                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐     ┌─────────────────┐                   │
│  │  Import         │     │  Monitoring     │                   │
│  │  Configuration  │     │  Service        │                   │
│  │                 │     │                 │                   │
│  │ • Import Folder │────▶│ • File System   │                   │
│  │ • Target Library│     │   Watchers      │                   │
│  │ • Format Rules  │     │ • Event         │                   │
│  │ • Blacklist     │     │   Processing    │                   │
│  └─────────────────┘     └────────┬────────┘                   │
│                                    │                            │
│                                    ▼                            │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Core Processing Engine                      │   │
│  │                                                          │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │   │
│  │  │  Format      │  │  Directory    │  │  Metadata    │  │   │
│  │  │  Detector    │  │  Builder      │  │  Extractor   │  │   │
│  │  │              │  │              │  │              │  │   │
│  │  │ • Multi-     │  │ • Book       │  │ • File       │  │   │
│  │  │   Format     │  │   Structure  │  │   Metadata   │  │   │
│  │  │   Detection  │  │ • Manga      │  │ • Cover      │  │   │
│  │  │ • Priority   │  │   Structure  │  │   Images     │  │   │
│  │  │   Ranking    │  │ • Rename     │  │ • Pattern    │  │   │
│  │  │              │  │   Rules      │  │   Extraction │  │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  │   │
│  │                                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                    │                            │
│                                    ▼                            │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Output & Integration                        │   │
│  │                                                          │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │   │
│  │  │  Library     │  │  User        │  │  Analytics   │  │   │
│  │  │  Integration │  │  Interface   │  │  & Reporting │  │   │
│  │  │              │  │              │  │              │  │   │
│  │  │ • Library     │  │ • Settings   │  │ • Import     │  │   │
│  │  │   Assignment │  │   Dashboard  │  │   Statistics │  │   │
│  │  │ • Scanner    │  │ • File       │  │ • Performance│  │   │
│  │  │   Integration│  │   Browser    │  │   Metrics    │  │   │
│  │  │ • Metadata   │  │ • Notifications│ │ • Logging   │  │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Component Relationships

The Kavita Importer integrates with existing Kavita services:

1. **LibraryWatcher Integration**: Leverages existing file system monitoring capabilities
2. **Scanner Service**: Utilizes existing parsing and content type classification
3. **MetadataService**: Extends metadata extraction for import-specific data
4. **SettingsService**: Provides configuration management for import settings
5. **TaskScheduler**: Coordinates background import processing

---

## 3. Core Components

### 3.1 Data Models

#### 3.1.1 ImportSettings Entity

```csharp
/// <summary>
/// Represents import configuration settings for the Kavita Importer
/// </summary>
public class ImportSettings : IEntityDate
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique identifier for the import configuration
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the import folder being monitored
    /// </summary>
    public string ImportFolderPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Target library ID where files will be imported
    /// </summary>
    public int TargetLibraryId { get; set; }
    
    /// <summary>
    /// Whether folder watching is enabled for this import configuration
    /// </summary>
    public bool EnableFolderWatching { get; set; } = true;
    
    /// <summary>
    /// List of supported file formats (EPUB, PDF, CBZ, CBR, etc.)
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new();
    
    /// <summary>
    /// Format priority ranking (higher value = higher priority)
    /// </summary>
    public Dictionary<string, int> FormatPriority { get; set; } = new();
    
    /// <summary>
    /// Blacklist patterns for excluding files/folders from import
    /// </summary>
    public List<string> BlacklistPatterns { get; set; } = new();
    
    /// <summary>
    /// Whether to automatically move files after processing
    /// </summary>
    public bool AutoImport { get; set; } = true;
    
    /// <summary>
    /// Whether to create directory structure for imported files
    /// </summary>
    public bool CreateDirectoryStructure { get; set; } = true;
    
    /// <summary>
    /// Naming pattern template for imported files
    /// </summary>
    public string NamingPattern { get; set; } = string.Empty;
    
    /// <summary>
    /// Last scan timestamp
    /// </summary>
    public DateTime LastScanDate { get; set; }
    
    /// <summary>
    /// Number of files successfully imported
    /// </summary>
    public int TotalFilesImported { get; set; }
    
    /// <summary>
    /// Number of files encountered during import
    /// </summary>
    public int TotalFilesProcessed { get; set; }
    
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
```

#### 3.1.2 ImportedFile Entity

```csharp
/// <summary>
/// Represents a file during the import process
/// </summary>
public class ImportedFile : IEntityDate
{
    public int Id { get; set; }
    
    /// <summary>
    /// Full path of the file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Detected file format
    /// </summary>
    public FileFormat FileFormat { get; set; }
    
    /// <summary>
    /// Current import status
    /// </summary>
    public ImportStatus Status { get; set; }
    
    /// <summary>
    /// Source folder path
    /// </summary>
    public string SourceFolder { get; set; } = string.Empty;
    
    /// <summary>
    /// Target library path for the file
    /// </summary>
    public string TargetLibraryPath { get; set; } = string.Empty;
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Original metadata extracted from the file
    /// </summary>
    public string? OriginalMetadata { get; set; }
    
    /// <summary>
    /// Path to extracted cover image
    /// </summary>
    public string? CoverImagePath { get; set; }
    
    /// <summary>
    /// Processing notes and logs
    /// </summary>
    public string? ProcessingNotes { get; set; }
    
    /// <summary>
    /// Batch identifier for grouped imports
    /// </summary>
    public string? BatchId { get; set; }
    
    /// <summary>
    /// Whether the file has been successfully imported
    /// </summary>
    public bool IsImported { get; set; }
    
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
```

#### 3.1.3 ImportStatus Enum

```csharp
/// <summary>
/// Defines the lifecycle states of files during import
/// </summary>
public enum ImportStatus
{
    /// <summary>
    /// File detected but not yet processed
    /// </summary>
    PENDING = 0,
    
    /// <summary>
    /// File awaiting user review and confirmation
    /// </summary>
    IN_REVIEW = 1,
    
    /// <summary>
    /// File currently being processed
    /// </summary>
    PROCESSING = 2,
    
    /// <summary>
    /// File successfully imported to library
    /// </summary>
    COMPLETED = 3,
    
    /// <summary>
    /// Import operation encountered an error
    /// </summary>
    FAILED = 4,
    
    /// <summary>
    /// File excluded from import via blacklist
    /// </summary>
    BLACKLISTED = 5,
    
    /// <summary>
    /// File marked for deletion
    /// </summary>
    PENDING_DELETION = 6
}
```

#### 3.1.4 FileFormat Enum

```csharp
/// <summary>
/// Supported file formats for import
/// </summary>
public enum FileFormat
{
    /// <summary>
    /// Electronic Publication format
    /// </summary>
    EPUB,
    
    /// <summary>
    /// Portable Document Format
    /// </summary>
    PDF,
    
    /// <summary>
    /// Comic Book Zip format
    /// </summary>
    CBZ,
    
    /// <summary>
    /// Comic Book Rar format
    /// </summary>
    CBR,
    
    /// <summary>
    /// Portable Network Graphics
    /// </summary>
    PNG,
    
    /// <summary>
    /// Joint Photographic Experts Group
    /// </summary>
    JPEG,
    
    /// <summary>
    /// Web Picture format
    /// </summary>
    WebP,
    
    /// <summary>
    /// Graphics Interchange Format
    /// </summary>
    GIF,
    
    /// <summary>
    /// AV1 Image File Format
    /// </summary>
    AVIF,
    
    /// <summary>
    /// Unknown or unsupported format
    /// </summary>
    UNKNOWN
}
```

### 3.2 Service Interfaces

#### 3.2.1 IKavitaImporterService

```csharp
/// <summary>
/// Core service interface for Kavita Importer functionality
/// </summary>
public interface IKavitaImporterService
{
    /// <summary>
    /// Gets the current import configuration
    /// </summary>
    Task<ImportSettingsDto?> GetImportSettingsAsync();
    
    /// <summary>
    /// Updates the import configuration
    /// </summary>
    Task<ImportSettingsDto> UpdateImportSettingsAsync(ImportSettingsDto settings);
    
    /// <summary>
    /// Scans the import folder for new files
    /// </summary>
    Task<ImportScanResult> ScanImportFolderAsync(bool forceScan = false);
    
    /// <summary>
    /// Processes pending files for import
    /// </summary>
    Task<ImportProcessResult> ProcessPendingFilesAsync(
        List<int> fileIds,
        ProcessOptions options);
    
    /// <summary>
    /// Imports selected files to the target library
    /// </summary>
    Task<ImportResult> ImportFilesAsync(
        List<int> fileIds,
        ImportOptions options);
    
    /// <summary>
    /// Manages blacklist patterns
    /// </summary>
    Task<BlacklistResult> ManageBlacklistAsync(BlacklistRequest request);
    
    /// <summary>
    /// Retrieves import statistics and analytics
    /// </summary>
    Task<ImportStatistics> GetImportStatisticsAsync(DateTime? startDate = null);
}
```

#### 3.2.2 IImportFolderWatcher

```csharp
/// <summary>
/// Service for monitoring the import folder and processing file events
/// </summary>
public interface IImportFolderWatcher : IHostedService
{
    /// <summary>
    /// Gets the current monitoring status
    /// </summary>
    Task<MonitoringStatus> GetMonitoringStatusAsync();
    
    /// <summary>
    /// Starts or resumes folder monitoring
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Pauses folder monitoring
    /// </summary>
    Task PauseMonitoringAsync();
    
    /// <summary>
    /// Stops folder monitoring
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Handles file system events
    /// </summary>
    Task HandleFileEventAsync(FileSystemEventArgs eventArgs);
}
```

#### 3.2.3 IFormatDetector

```csharp
/// <summary>
/// Service for detecting and classifying file formats
/// </summary>
public interface IFormatDetector
{
    /// <summary>
    /// Detects the format of a file based on its extension and content
    /// </summary>
    Task<FileFormat> DetectFormatAsync(string filePath);
    
    /// <summary>
    /// Determines if a file format is supported for import
    /// </summary>
    bool IsSupportedFormat(FileFormat format);
    
    /// <summary>
    /// Gets the list of all supported formats
    /// </summary>
    List<FileFormat> GetSupportedFormats();
    
    /// <summary>
    /// Applies format priority ranking to resolve format conflicts
    /// </summary>
    FileFormat ResolveFormatConflict(
        List<FileFormat> candidateFormats,
        Dictionary<string, int> formatPriority);
}
```

#### 3.2.4 IDirectoryStructureBuilder

```csharp
/// <summary>
/// Service for creating and managing directory structures
/// </summary>
public interface IDirectoryStructureBuilder
{
    /// <summary>
    /// Creates the appropriate directory structure for a file
    /// </summary>
    Task<DirectoryStructureResult> CreateDirectoryStructureAsync(
        ImportedFile file,
        DirectoryStructureOptions options);
    
    /// <summary>
    /// Applies naming conventions to files
    /// </summary>
    Task<RenamedFileResult> ApplyNamingConventionsAsync(
        ImportedFile file,
        string namingPattern);
    
    /// <summary>
    /// Generates book or manga directory structures
    /// </summary>
    Task<GeneratedStructure> GenerateLibraryStructureAsync(
        LibraryType libraryType,
        List<ImportedFile> files);
}
```

### 3.3 Implementation Classes

#### 3.3.1 KavitaImporterService

**Location**: `Kavita.Services/KavitaImporterService.cs`

**Responsibilities**:
- Manages import configuration and settings
- Coordinates import operations across services
- Handles import workflow orchestration
- Provides import status and reporting

**Key Methods**:
```csharp
public class KavitaImporterService(
    IUnitOfWork unitOfWork,
    IDirectoryService directoryService,
    IImportFolderWatcher folderWatcher,
    IFormatDetector formatDetector,
    IDirectoryStructureBuilder structureBuilder,
    ILogger<KavitaImporterService> logger)
    : IKavitaImporterService
{
    // Implementation details...
}
```

#### 3.3.2 ImportFolderWatcher

**Location**: `Kavita.Services/Scanner/ImportFolderWatcher.cs`

**Responsibilities**:
- Extends LibraryWatcher for import-specific monitoring
- Processes file system events for import
- Manages import queue and processing state
- Coordinates with TaskScheduler for background processing

**Key Features**:
- File system event handling
- Import queue management
- Processing state tracking
- Error handling and recovery

#### 3.3.3 FormatDetectorService

**Location**: `Kavita.Services/Import/FormatDetectorService.cs`

**Responsibilities**:
- Detects file formats using extension and content analysis
- Applies format priority ranking
- Resolves format conflicts for files with multiple extensions
- Provides format information for import decisions

#### 3.3.4 DirectoryStructureBuilder

**Location**: `Kavita.Services/Import/DirectoryStructureBuilder.cs`

**Responsibilities**:
- Creates directory structures following Kavita conventions
- Applies naming conventions to imported files
- Generates book and manga folder hierarchies
- Manages file organization and movement

---

## 4. Configuration Management

### 4.1 Server Settings Extension

#### 4.1.1 Extended ServerSettingKey Enum

Add new setting keys to `Kavita.Models/Entities/Enums/ServerSettingKey.cs`:

```csharp
public enum ServerSettingKey
{
    // Existing settings...
    
    /// <summary>
    /// Path to the import folder for automatic file import
    /// </summary>
    [Description("ImportFolderPath")]
    ImportFolderPath = 43,
    
    /// <summary>
    /// Import format preferences and priorities
    /// </summary>
    [Description("ImportFormatPreferences")]
    ImportFormatPreferences = 44,
    
    /// <summary>
    /// Blacklist patterns for import exclusion
    /// </summary>
    [Description("ImportBlacklistPatterns")]
    ImportBlacklistPatterns = 45,
    
    /// <summary>
    /// Import configuration settings
    /// </summary>
    [Description("ImportConfiguration")]
    ImportConfiguration = 46,
    
    /// <summary>
    /// Import statistics and analytics data
    /// </summary>
    [Description("ImportStatistics")]
    ImportStatistics = 47
}
```

#### 4.1.2 ServerSettingDto Extension

Extend `Kavita.Models/DTOs/Settings/ServerSettingDTO.cs` with import-related properties:

```csharp
public sealed record ServerSettingDto
{
    // Existing properties...
    
    /// <summary>
    /// Path to the import folder
    /// </summary>
    public string ImportFolderPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether import folder monitoring is enabled
    /// </summary>
    public bool EnableImportMonitoring { get; set; } = true;
    
    /// <summary>
    /// Format preferences for import (JSON serialized)
    /// </summary>
    public string ImportFormatPreferences { get; set; } = string.Empty;
    
    /// <summary>
    /// Blacklist patterns for import (JSON serialized)
    /// </summary>
    public string ImportBlacklistPatterns { get; set; } = string.Empty;
    
    /// <summary>
    /// Import configuration settings (JSON serialized)
    /// </summary>
    public string ImportConfiguration { get; set; } = string.Empty;
    
    /// <summary>
    /// Import statistics data (JSON serialized)
    /// </summary>
    public string ImportStatistics { get; set; } = string.Empty;
}
```

### 4.2 Settings UI Configuration

#### 4.2.1 Import Settings Panel

The settings UI should include:

1. **Import Folder Configuration**
   - Path selection for import directory
   - Folder watching enable/disable toggle
   - Real-time path validation

2. **Target Library Assignment**
   - Library selection dropdown
   - Library capacity and usage display
   - Automatic library recommendation

3. **Format Priority Management**
   - Format preference ranking interface
   - Priority level configuration
   - Format support status indicators

4. **Blacklist Configuration**
   - Pattern-based exclusion rules
   - Folder and file exclusion options
   - Blacklist management interface

5. **Import Monitoring**
   - Real-time import status dashboard
   - Processing activity logs
   - Import history and analytics

### 4.3 Configuration Examples

#### 4.3.1 Sample Import Settings

```json
{
  "enableImport": true,
  "importFolderPath": "/data/import",
  "targetLibraryId": 1,
  "folderWatching": {
    "enabled": true,
    "watchInterval": 5000,
    "eventTypes": ["Created", "Changed", "Deleted"]
  },
  "formatPreferences": {
    "primaryFormats": ["EPUB", "PDF"],
    "secondaryFormats": ["CBZ", "CBR"],
    "tertiaryFormats": ["PNG", "JPEG", "WebP", "GIF", "AVIF"],
    "priorityRanking": {
      "EPUB": 100,
      "PDF": 95,
      "CBZ": 90,
      "CBR": 85,
      "WebP": 80,
      "PNG": 75,
      "JPEG": 70,
      "GIF": 65,
      "AVIF": 60
    }
  },
  "blacklistPatterns": {
    "folders": [
      ".DS_Store",
      "Thumbs.db",
      "@eaDir",
      "__MACOSX",
      ".cache",
      ".temp"
    ],
    "files": [
      "*.tmp",
      "*.log",
      "*.cache",
      "exclude.txt",
      "blacklist.txt"
    ],
    "extensions": [
      ".git",
      ".svn",
      ".hg"
    ]
  },
  "importRules": {
    "autoImport": true,
    "createDirectoryStructure": true,
    "extractMetadata": true,
    "generateCovers": true,
    "moveSourceFiles": true,
    "namingPattern": "{Title}-v{SeriesNumber}.{Extension}"
  },
  "processingOptions": {
    "maxConcurrentOperations": 4,
    "retryFailedImports": true,
    "maxRetries": 3,
    "retryDelaySeconds": 30,
    "enableNotifications": true
  }
}
```

---

## 5. Processing Workflow

### 5.1 Import Lifecycle

#### 5.1.1 File Lifecycle States

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   PENDING    │────▶│  IN_REVIEW   │────▶│ PROCESSING   │
│              │     │              │     │              │
│ • Detected   │     │ • User       │     │ • Metadata   │
│ • Queued     │     │   Review     │     │   Extraction │
│ • Awaiting   │     │ • Validation │     │ • Structure  │
│   Action     │     │ • Confirmation│    │   Creation   │
└──────────────┘     └──────────────┘     └──────────────┘
                               │
                               ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  BLACKLISTED │◀────│   COMPLETED  │◀────│  FAILED      │
│              │     │              │     │              │
│ • Excluded   │     │ • Imported   │     │ • Error      │
│ • Pattern    │     │ • Integrated │     │   Handling   │
│   Matched    │     │ • Indexed    │     │ • Retry      │
│              │     │ • Finalized  │     │   Mechanism  │
└──────────────┘     └──────────────┘     └──────────────┘
```

#### 5.1.2 Processing Pipeline

```
File Detection
    │
    ▼
┌─────────────────────────────────────┐
│  Step 1: Detection & Validation     │
│  - File system event triggered      │
│  - File format detection            │
│  - Blacklist pattern matching       │
│  - Duplicate detection               │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  Step 2: Metadata Extraction        │
│  - Extract file metadata            │
│  - Generate cover images            │
│  - Analyze file content             │
│  - Apply metadata to file           │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  Step 3: Directory Structure        │
│  - Determine target location        │
│  - Create directory hierarchy       │
│  - Apply naming conventions         │
│  - Organize file placement          │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  Step 4: Import & Integration       │
│  - Move file to target library      │
│  - Update library catalog           │
│  - Index file for search            │
│  - Generate import reports          │
└─────────────────────────────────────┘
    │
    ▼
File Integration Complete
```

### 5.2 Event Processing

#### 5.2.1 File System Events

The ImportFolderWatcher handles three primary file system events:

1. **File Created**
   - Triggered when new files are added to the import folder
   - Initiates import processing pipeline
   - Validates file format and completeness

2. **File Changed**
   - Monitors file modifications during download or editing
   - Ensures file stability before processing
   - Updates file metadata as needed

3. **File Deleted**
   - Handles file removal from import folder
   - Processes deletion of imported files
   - Maintains import history and records

#### 5.2.2 Event Processing Flow

```csharp
public class ImportFolderWatcher
{
    // Event handlers
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // Process newly created files
        ProcessNewFileAsync(e.FullPath);
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Handle file modifications
        ProcessFileChangeAsync(e.FullPath);
    }
    
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        // Manage file deletions
        ProcessFileDeletionAsync(e.FullPath);
    }
}
```

---

## 6. Directory Structure Implementation

### 6.1 Book Directory Structure

#### 6.1.1 Standard Book Structure

```
output-folder/
├── BookTitle/
│   └── BookTitle.ext
├── AnotherBook/
│   └── AnotherBook.epub
└── ThirdBook/
    └── ThirdBook.pdf
```

#### 6.1.2 Book with Multiple Formats

```
Library/
├── Fiction/
│   ├── Harry-Potter/
│   │   ├── Harry-Potter-Vol.01.epub
│   │   ├── Harry-Potter-Vol.02.epub
│   │   └── Harry-Potter-Vol.01.pdf
│   └── The-Lord-of-the-Rings/
│       ├── The-Lord-of-the-Rings-Vol.01.epub
│       └── The-Lord-of-the-Rings-Vol.02.epub
├── Non-Fiction/
│   ├── Science-Fiction/
│   │   └── Dune-Vol.01.epub
│   └── Technology/
│       └── Clean-Code.pdf
└── Specials/
    └── Anthology/
        └──Sci-Fi-Anthology-2024.epub
```

### 6.2 Manga Directory Structure

#### 6.2.1 Standard Manga Structure

```
output-folder/
├── MangaSeries/
│   ├── MangaSeries-v01-c001.ext
│   ├── MangaSeries-v01-c002.ext
│   └── MangaSeries-v02-c050.ext
├── MangaSeries-SP/
│   └── MangaSeries-SP01.ext
└── MangaSeries-Annual/
    └── MangaSeries-Annual-2024.ext
```

#### 6.2.2 Manga with Volumes and Chapters

```
Library/
├── Manga/
│   ├── Naruto/
│   │   ├── Naruto-v001-c001-025.cbz
│   │   ├── Naruto-v001-c026-050.cbz
│   │   ├── Naruto-v010-c171-180.cbz
│   │   └── [Specials]/
│   │       ├── Naruto-SP01 - Omake.cbz
│   │       └── Naruto-SP02 - Extra.cbz
│   ├── One-Piece/
│   │   ├── One-Piece-v1000-c1000-1020.cbz
│   │   └── Annual/
│   │       └── One-Piece-Annual-2024.cbz
│   └── Attack-on-Titan/
│       ├── Attack-on-Titan-v01-c001.cbz
│       └── TPB/
│           └── Attack-on-Titan-TPB-01.cbr
└── Specials/
    ├── Anthology/
    │   └── Manga-Anthology-2024.cbz
    └── Omnibus/
        └── Manga-Omnibus-Complete.cbz
```

### 6.3 Image Directory Structure

#### 6.3.1 Image Library Organization

```
Library/
├── Images/
│   ├── Solo-Leveling/
│   │   ├── Solo-Leveling-S01-001.jpg
│   │   ├── Solo-Leveling-S01-002.jpg
│   │   └── Solo-Leveling-S02-050.webp
│   ├── Tower-of-God/
│   │   ├── Tower-of-God-T01-001.webp
│   │   └── Tower-of-God-T01-050.webp
│   └── Specials/
│       └── Omake/
│           └── Character-Guides.jpg
```

---

## 7. API Design

### 7.1 REST API Endpoints

#### 7.1.1 Import Configuration Endpoints

**1. Get Import Settings**
```http
GET /api/v1/import/settings
```

**Response:**
```json
{
  "enabled": true,
  "importFolderPath": "/data/import",
  "targetLibraryId": 1,
  "folderWatching": {
    "enabled": true,
    "watchInterval": 5000
  },
  "formatPreferences": {
    "primaryFormats": ["EPUB", "PDF"],
    "priorityRanking": {
      "EPUB": 100,
      "PDF": 95
    }
  },
  "blacklistPatterns": {
    "folders": [".DS_Store", "Thumbs.db"],
    "files": ["*.tmp", "*.log"]
  }
}
```

**2. Update Import Settings**
```http
POST /api/v1/import/settings
Content-Type: application/json

{
  "enableImport": true,
  "importFolderPath": "/data/import",
  "targetLibraryId": 1,
  "formatPreferences": {...},
  "blacklistPatterns": {...}
}
```

**3. Enable/Disable Import Monitoring**
```http
POST /api/v1/import/monitor
Content-Type: application/json

{
  "enableMonitoring": true
}
```

#### 7.1.2 File Management Endpoints

**4. Scan Import Folder**
```http
POST /api/v1/import/scan
Content-Type: application/json

{
  "forceScan": false,
  "processNewFilesOnly": true
}
```

**5. Get Imported Files**
```http
GET /api/v1/import/files
Query Parameters:
  - page: number
  - pageSize: number
  - status: string
  - format: string
  - startDate: datetime
  - endDate: datetime
```

**6. Process Files**
```http
POST /api/v1/import/files/process
Content-Type: application/json

{
  "fileIds": [1, 2, 3],
  "options": {
    "extractMetadata": true,
    "createDirectoryStructure": true,
    "generateCovers": true
  }
}
```

**7. Import Files**
```http
POST /api/v1/import/import
Content-Type: application/json

{
  "fileIds": [1, 2, 3],
  "options": {
    "moveFiles": true,
    "targetLibraryId": 1,
    "namingPattern": "{Title}-v{SeriesNumber}.{Extension}"
  }
}
```

**8. Manage Blacklist**
```http
POST /api/v1/import/blacklist
Content-Type: application/json

{
  "action": "Add",
  "patterns": {
    "folders": [".cache"],
    "files": ["*.log"],
    "extensions": [".tmp"]
  }
}
```

#### 7.1.3 Statistics and Reporting Endpoints

**9. Get Import Statistics**
```http
GET /api/v1/import/statistics
Query Parameters:
  - startDate: datetime
  - endDate: datetime
```

**10. Get Import History**
```http
GET /api/v1/import/history
Query Parameters:
  - page: number
  - pageSize: number
  - sortBy: string
  - sortOrder: string
```

### 7.2 API Response Models

#### 7.2.1 ImportSettingsDto

```csharp
public class ImportSettingsDto
{
    /// <summary>
    /// Whether import functionality is enabled
    /// </summary>
    public bool EnableImport { get; set; }
    
    /// <summary>
    /// Path to the import folder
    /// </summary>
    public string ImportFolderPath { get; set; }
    
    /// <summary>
    /// Target library ID
    /// </summary>
    public int TargetLibraryId { get; set; }
    
    /// <summary>
    /// Folder watching configuration
    /// </summary>
    public FolderWatchingConfig FolderWatching { get; set; }
    
    /// <summary>
    /// Format preferences and priorities
    /// </summary>
    public FormatPreferences FormatPreferences { get; set; }
    
    /// <summary>
    /// Blacklist patterns
    /// </summary>
    public BlacklistPatterns BlacklistPatterns { get; set; }
    
    /// <summary>
    /// Import processing options
    /// </summary>
    public ImportOptions ImportOptions { get; set; }
    
    /// <summary>
    /// Whether to automatically import files
    /// </summary>
    public bool AutoImport { get; set; }
    
    /// <summary>
    /// Whether to create directory structure
    /// </summary>
    public bool CreateDirectoryStructure { get; set; }
    
    /// <summary>
    /// Naming pattern template
    /// </summary>
    public string NamingPattern { get; set; }
}

public class FolderWatchingConfig
{
    public bool Enabled { get; set; }
    public int WatchInterval { get; set; }
    public List<WatcherEventType> EventTypes { get; set; }
}

public class FormatPreferences
{
    public List<FileFormat> PrimaryFormats { get; set; }
    public List<FileFormat> SecondaryFormats { get; set; }
    public List<FileFormat> TertiaryFormats { get; set; }
    public Dictionary<string, int> PriorityRanking { get; set; }
}

public class BlacklistPatterns
{
    public List<string> Folders { get; set; }
    public List<string> Files { get; set; }
    public List<string> Extensions { get; set; }
}

public class ImportOptions
{
    public int MaxConcurrentOperations { get; set; }
    public bool RetryFailedImports { get; set; }
    public int MaxRetries { get; set; }
    public int RetryDelaySeconds { get; set; }
    public bool EnableNotifications { get; set; }
}
```

#### 7.2.2 ImportStatistics

```csharp
public class ImportStatistics
{
    /// <summary>
    /// Total number of files processed
    /// </summary>
    public long TotalFilesProcessed { get; set; }
    
    /// <summary>
    /// Total number of files successfully imported
    /// </summary>
    public long TotalFilesImported { get; set; }
    
    /// <summary>
    /// Total file size in bytes
    /// </summary>
    public long TotalFileSize { get; set; }
    
    /// <summary>
    /// Number of files in pending state
    /// </summary>
    public long PendingFilesCount { get; set; }
    
    /// <summary>
    /// Number of files currently processing
    /// </summary>
    public long ProcessingFilesCount { get; set; }
    
    /// <summary>
    /// Number of completed files
    /// </summary>
    public long CompletedFilesCount { get; set; }
    
    /// <summary>
    /// Number of failed imports
    /// </summary>
    public long FailedFilesCount { get; set; }
    
    /// <summary>
    /// Average processing time per file (milliseconds)
    /// </summary>
    public double AverageProcessingTime { get; set; }
    
    /// <summary>
    /// Import success rate (percentage)
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// Format distribution statistics
    /// </summary>
    public Dictionary<string, long> FormatDistribution { get; set; }
    
    /// <summary>
    /// Time range of statistics
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Time range of statistics
    /// </summary>
    public DateTime EndDate { get; set; }
}
```

---

## 8. Implementation Roadmap

### 8.1 Phase 1: Foundation (Weeks 1-2)

**Objectives:**
- Establish core infrastructure and data models
- Implement basic service components
- Configure database schema

**Deliverables:**

1. **Data Models**
   - [ ] Implement `ImportSettings` entity
   - [ ] Implement `ImportedFile` entity
   - [ ] Create repository interfaces and implementations
   - [ ] Update `UnitOfWork` with new repositories

2. **Core Services**
   - [ ] Implement `KavitaImporterService`
   - [ ] Create `ImportFolderWatcher` service
   - [ ] Develop `FormatDetectorService`
   - [ ] Build `DirectoryStructureBuilder`

3. **Database Migration**
   - [ ] Create migration scripts for new entities
   - [ ] Add indexes for performance optimization
   - [ ] Populate initial configuration data

**Success Criteria:**
- All core entities and services implemented
- Database schema successfully migrated
- Basic import functionality operational

### 8.2 Phase 2: Processing Logic (Weeks 3-4)

**Objectives:**
- Implement comprehensive file processing pipeline
- Develop format detection and priority ranking
- Create directory structure generation

**Deliverables:**

1. **File Processing Pipeline**
   - [ ] Implement file detection and validation
   - [ ] Develop metadata extraction logic
   - [ ] Create directory structure generation
   - [ ] Implement file movement and organization

2. **Format Management**
   - [ ] Develop format detection algorithms
   - [ ] Implement format priority ranking
   - [ ] Create format conflict resolution
   - [ ] Build format preference management

3. **Blacklist System**
   - [ ] Implement blacklist pattern matching
   - [ ] Create blacklist management interface
   - [ ] Develop exclusion rules engine
   - [ ] Build blacklist configuration UI

**Success Criteria:**
- Complete file processing pipeline operational
- Format detection and ranking functioning
- Blacklist system fully implemented

### 8.3 Phase 3: User Interface (Weeks 5-6)

**Objectives:**
- Develop REST API endpoints
- Create settings configuration interface
- Implement monitoring and reporting dashboard

**Deliverables:**

1. **API Development**
   - [ ] Implement REST API endpoints
   - [ ] Create API documentation
   - [ ] Develop API clients and integration
   - [ ] Test API functionality and performance

2. **Settings Interface**
   - [ ] Design settings configuration panel
   - [ ] Implement import folder selection
   - [ ] Create format priority configuration
   - [ ] Build blacklist management interface

3. **Dashboard & Reporting**
   - [ ] Develop import status dashboard
   - [ ] Implement real-time monitoring
   - [ ] Create import history views
   - [ ] Build analytics and reporting

**Success Criteria:**
- REST API fully implemented and tested
- Settings interface user-friendly and functional
- Dashboard providing comprehensive visibility

### 8.4 Phase 4: Testing & Optimization (Weeks 7-8)

**Objectives:**
- Conduct comprehensive testing
- Optimize performance and scalability
- Complete documentation and deployment

**Deliverables:**

1. **Testing**
   - [ ] Develop unit test suite
   - [ ] Implement integration tests
   - [ ] Conduct end-to-end testing
   - [ ] Perform performance testing

2. **Optimization**
   - [ ] Optimize file processing performance
   - [ ] Implement caching strategies
   - [ ] Enhance error handling and recovery
   - [ ] Improve scalability for large imports

3. **Documentation & Deployment**
   - [ ] Create comprehensive documentation
   - [ ] Develop user guides and tutorials
   - [ ] Prepare deployment procedures
   - [ ] Conduct user acceptance testing

**Success Criteria:**
- All tests passing with high coverage
- Performance metrics meeting requirements
- Documentation complete and accessible

---

## 9. Technical Specifications

### 9.1 File Format Detection

#### 9.1.1 Detection Strategy

```csharp
public class FormatDetectorService : IFormatDetector
{
    public async Task<FileFormat> DetectFormatAsync(string filePath)
    {
        // Step 1: Detect format from file extension
        var format = DetectFromExtension(filePath);
        
        // Step 2: Validate format with content analysis
        if (format == FileFormat.UNKNOWN)
        {
            format = await AnalyzeFileContentAsync(filePath);
        }
        
        // Step 3: Apply format priority ranking
        return format;
    }
    
    private FileFormat DetectFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".epub" => FileFormat.EPUB,
            ".pdf" => FileFormat.PDF,
            ".cbz" => FileFormat.CBZ,
            ".cbr" => FileFormat.CBR,
            ".png" => FileFormat.PNG,
            ".jpg" or ".jpeg" => FileFormat.JPEG,
            ".webp" => FileFormat.WebP,
            ".gif" => FileFormat.GIF,
            ".avif" => FileFormat.AVIF,
            _ => FileFormat.UNKNOWN
        };
    }
}
```

#### 9.1.2 Format Priority Ranking

```csharp
public class FormatPriorityRanking
{
    public Dictionary<FileFormat, int> PriorityLevels { get; } = new()
    {
        { FileFormat.EPUB, 100 },      // Primary
        { FileFormat.PDF, 95 },        // Primary
        { FileFormat.CBZ, 90 },        // Secondary
        { FileFormat.CBR, 85 },        // Secondary
        { FileFormat.WebP, 80 },       // Tertiary
        { FileFormat.PNG, 75 },        // Tertiary
        { FileFormat.JPEG, 70 },       // Tertiary
        { FileFormat.GIF, 65 },        // Tertiary
        { FileFormat.AVIF, 60 }        // Tertiary
    };
    
    public FileFormat ResolveConflict(
        List<FileFormat> formats,
        Dictionary<string, int> customPriority)
    {
        return formats
            .OrderByDescending(f => 
                customPriority.GetValueOrDefault(f.ToString(), 
                    PriorityLevels[f]))
            .First();
    }
}
```

### 9.2 Directory Structure Generation

#### 9.2.1 Book Structure Generator

```csharp
public class BookStructureGenerator : IDirectoryStructureBuilder
{
    public async Task<GeneratedStructure> GenerateAsync(
        List<ImportedFile> files,
        string rootPath)
    {
        var structure = new GeneratedStructure
        {
            RootPath = rootPath,
            Books = new Dictionary<string, List<ImportedFile>>()
        };
        
        foreach (var file in files)
        {
            var bookFolder = Path.Combine(rootPath, file.FileName);
            
            if (!structure.Books.ContainsKey(bookFolder))
            {
                structure.Books[bookFolder] = new List<ImportedFile>();
            }
            
            structure.Books[bookFolder].Add(file);
        }
        
        return structure;
    }
}
```

#### 9.2.2 Manga Structure Generator

```csharp
public class MangaStructureGenerator : IDirectoryStructureBuilder
{
    public async Task<GeneratedStructure> GenerateAsync(
        List<ImportedFile> files,
        string rootPath)
    {
        var structure = new GeneratedStructure
        {
            RootPath = rootPath,
            Series = new Dictionary<string, List<ImportedFile>>()
        };
        
        foreach (var file in files)
        {
            var seriesFolder = Path.Combine(rootPath, file.FileName);
            
            // Apply naming convention
            var namedFile = await ApplyMangaNamingConvention(file);
            
            if (!structure.Series.ContainsKey(seriesFolder))
            {
                structure.Series[seriesFolder] = new List<ImportedFile>();
            }
            
            structure.Series[seriesFolder].Add(namedFile);
        }
        
        return structure;
    }
    
    private async Task<ImportedFile> ApplyMangaNamingConvention(
        ImportedFile file)
    {
        // Apply pattern: SeriesName-vVol-cChap.ext
        var namingPattern = "{Title}-v{SeriesNumber}-c{ChapterNumber}.{Extension}";
        
        // Extract and apply naming pattern
        file.FileName = ApplyNamingPattern(file.FileName, namingPattern);
        
        return file;
    }
}
```

### 9.3 Blacklist Management

#### 9.3.1 Blacklist Pattern Configuration

```csharp
public class BlacklistConfiguration
{
    public List<string> FolderPatterns { get; set; } = new()
    {
        ".DS_Store",
        "Thumbs.db",
        "@eaDir",
        "__MACOSX",
        ".cache",
        ".temp",
        ".git",
        ".svn"
    };
    
    public List<string> FilePatterns { get; set; } = new()
    {
        "*.tmp",
        "*.log",
        "*.cache",
        "exclude.txt",
        "blacklist.txt"
    };
    
    public List<string> ExtensionPatterns { get; set; } = new()
    {
        ".git",
        ".svn",
        ".hg",
        ".lock"
    };
    
    public bool ShouldExclude(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        
        return FolderPatterns.Contains(fileName) ||
               FilePatterns.Any(p => MatchesPattern(fileName, p)) ||
               ExtensionPatterns.Contains(extension);
    }
    
    private bool MatchesPattern(string fileName, string pattern)
    {
        if (!pattern.Contains("*"))
        {
            return fileName == pattern;
        }
        
        var regex = new Regex("^" + 
            Regex.Escape(pattern).Replace("\\*", ".*") + "$");
        
        return regex.IsMatch(fileName);
    }
}
```

---

## 10. Deployment Considerations

### 10.1 Docker Configuration

#### 10.1.1 Docker Compose Example

```yaml
version: '3.8'

services:
  kavita:
    image: kavita/kavita:latest
    container_name: kavita
    ports:
      - "5000:5000"
    volumes:
      - ./config:/data
      - ./library:/books
      - ./import:/import
      - ./backups:/backups
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=UTC
      - KAVITA_IMPORT_ENABLED=true
      - KAVITA_IMPORT_PATH=/import
      - KAVITA_IMPORT_LIBRARY_ID=1
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  config:
  library:
  import:
  backups:
```

#### 10.1.2 Import Folder Structure

```
/docker/
├── kavita/
│   ├── config/              # Configuration files
│   │   ├── appsettings.json
│   │   ├── kavita.db
│   │   └── imports/
│   │       └── settings.json
│   ├── library/             # Library content
│   │   ├── Books/
│   │   ├── Manga/
│   │   └── Comics/
│   ├── import/              # Import folder
│   │   ├── uploaded/        # Files awaiting import
│   │   ├── temp-downloads/  # Temporary files
│   │   ├── cover-images/    # Extracted covers
│   │   └── processed/       # Processed files
│   └── backups/             # Backup data
```

### 10.2 Performance Optimization

#### 10.2.1 Caching Strategy

```csharp
public class ImportCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ImportCacheService> _logger;
    
    public async Task<ImportSettings?> GetCachedSettingsAsync()
    {
        var cacheKey = "import:settings";
        var settings = await _cache.GetAsync<ImportSettings>(cacheKey);
        
        if (settings == null)
        {
            settings = await LoadSettingsFromDatabase();
            await _cache.SetAsync(cacheKey, settings, 
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
        }
        
        return settings;
    }
    
    public async Task InvalidateSettingsCacheAsync()
    {
        var cacheKey = "import:settings";
        await _cache.RemoveAsync(cacheKey);
        _logger.LogInformation("Import settings cache invalidated");
    }
}
```

#### 10.2.2 Background Processing

```csharp
public class ImportBackgroundProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportBackgroundProcessor> _logger;
    
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessImportQueueAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background processing");
            }
        }
    }
    
    private async Task ProcessImportQueueAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var importerService = 
            scope.ServiceProvider.GetRequiredService<IKavitaImporterService>();
        
        var pendingFiles = await importerService.GetPendingFilesAsync();
        
        foreach (var file in pendingFiles)
        {
            await importerService.ProcessFileAsync(file, cancellationToken);
        }
    }
}
```

---

## 11. Success Metrics & KPIs

### 11.1 Performance Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Import Processing Time | < 5 seconds per file | End-to-end timing |
| File Detection Latency | < 2 seconds | Event to processing |
| Import Success Rate | > 95% | Success/Total ratio |
| Metadata Extraction Accuracy | > 90% | Validation checks |
| Directory Structure Compliance | 100% | Structure validation |
| User Configuration Time | < 10 minutes | User experience |

### 11.2 User Experience Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| User Adoption Rate | > 80% | Usage analytics |
| Configuration Simplicity | < 3 steps | User workflow |
| Notification Effectiveness | > 85% | User feedback |
| Error Resolution Time | < 24 hours | Issue tracking |
| Documentation Accessibility | < 2 clicks | Navigation analysis |

### 11.3 Business Value Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Manual Effort Reduction | > 60% | Time savings analysis |
| Library Growth Rate | > 20% annually | Library metrics |
| User Satisfaction Score | > 4.5/5.0 | User surveys |
| System Scalability | Support 10K+ files | Load testing |
| ROI on Implementation | Positive within 6 months | Cost-benefit analysis |

---

## 12. Risk Management

### 12.1 Risk Assessment

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| File System Compatibility | Medium | High | Cross-platform testing |
| Large-Scale Performance | Medium | High | Performance optimization |
| User Adoption Challenges | Medium | Medium | Comprehensive training |
| Integration Complexity | Low | High | Modular architecture |
| Data Migration Issues | Low | Medium | Robust migration planning |

### 12.2 Contingency Planning

1. **Performance Optimization**
   - Implement caching strategies
   - Optimize database queries
   - Scale infrastructure as needed

2. **User Support**
   - Provide comprehensive documentation
   - Establish support channels
   - Gather and act on user feedback

3. **Continuous Improvement**
   - Monitor system performance
   - Update features based on usage
   - Iterate on user experience

---

## 13. Conclusion

The Kavita Importer represents a strategic enhancement to the Kavita platform, addressing critical user needs for automated file import and organization. By leveraging existing infrastructure and implementing a comprehensive solution, this feature will significantly improve the user experience and library management capabilities.

**Key Benefits:**
- Streamlined import process reduces manual effort
- Intelligent format detection ensures compatibility
- Automated directory structure maintains organization
- Real-time monitoring provides visibility and control
- Scalable architecture supports future growth

**Implementation Approach:**
The phased implementation approach ensures methodical development, testing, and deployment, minimizing risks while maximizing value delivery. The comprehensive documentation and user-focused design will facilitate successful adoption and long-term sustainability.

---

## Appendix

### A. Glossary

- **Import Folder**: Designated directory for file collection and processing
- **Format Priority**: Ranking system for file format preferences
- **Blacklist**: Exclusion rules for filtering files and folders
- **Directory Structure**: Organized hierarchy of folders and files
- **Metadata**: Descriptive information about files and content

### B. References

1. Kavita Scanner Service Documentation
2. BookImport Implementation Guide
3. Directory Structure Guidelines
4. API Specification Documentation

### C. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2024 | Kavita Team | Initial implementation plan |

---

*Document prepared for Kavita Development Team*
*Last updated: April 2024*
