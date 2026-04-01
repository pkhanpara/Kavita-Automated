# BookImport Feature - Implementation Guide

## Overview

The BookImport feature provides a zero-effort solution for automatic book file imports into Kavita's library. It enables seamless file detection, metadata extraction, and organization according to the Scanner's directory structure requirements.

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     BookImport System                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐    ┌──────────────────┐              │
│  │  Monitoring      │    │   Metadata       │              │
│  │  Service         │◄──►│   Service        │              │
│  │                  │    │                  │              │
│  │ • File System    │    │ • Metadata       │              │
│  │   Events         │    │   Extraction     │              │
│  │ • Stability      │    │ • Cover          │              │
│  │   Checks         │    │   Generation     │              │
│  │ • Format         │    │ • Pattern        │              │
│  │   Filtering      │    │   Application    │              │
│  └────────┬─────────┘    └────────┬─────────┘              │
│           │                       │                        │
│           ▼                       ▼                        │
│  ┌──────────────────────────────────────────┐              │
│  │        Filename Pattern Extractor        │              │
│  │                                          │              │
│  │ • Template-Based Naming                   │              │
│  │ • Pattern Extraction                     │              │
│  │ • Placeholder Management                 │              │
│  └────────────────┬─────────────────────────┘              │
│                   │                                         │
│                   ▼                                         │
│  ┌──────────────────────────────────────────┐              │
│  │         Bulk Operations Service          │              │
│  │                                          │              │
│  │ • Library Assignment                     │              │
│  │ • File Movement                          │              │
│  │ • Mass Processing                        │              │
│  └────────────────┬─────────────────────────┘              │
│                   │                                         │
└───────────────────┼─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│                    File System Structure                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  /bookimport/                                              │
│  ├── uploaded/         # New files awaiting processing      │
│  │   ├── epub/          # EPUB files                        │
│  │   ├── pdf/           # PDF files                         │
│  │   └── comics/        # Comic files (CBZ/CBR)             │
│  ├── temp-downloads/   # Temporary processing files         │
│  ├── cover-images/     # Extracted cover images             │
│  └── processed/        # Finalized files ready for library  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. BookImportFile Entity

The `BookImportFile` entity serves as the central data model for tracking files through their lifecycle:

```csharp
public class BookImportFile : IEntityDate
{
    public int Id { get; set; }
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public long FileSize { get; set; }
    public BookImportFileStatus Status { get; set; }
    public string? OriginalMetadata { get; set; }
    public int? LibraryId { get; set; }
    public string? TargetPath { get; set; }
    public string? NamingPattern { get; set; }
    public FileFormat FileFormat { get; set; }
    public ContentType ContentType { get; set; }
    public string? CoverImagePath { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ProcessingNotes { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int? UploadedBy { get; set; }
    public bool IsImported { get; set; }
    public string? BatchId { get; set; }
    public string? TypeMetadata { get; set; }
}
```

### 2. BookImportFileStatus Enum

```csharp
public enum BookImportFileStatus
{
    PendingReview = 0,    // File awaiting user review
    Processing = 1,       // File being processed
    Finalized = 2,        // File finalized and imported
    PendingDeletion = 3,  // File pending deletion
    Deleted = 4,          // File deleted
    Error = 5             // File encountered an error
}
```

### 3. Service Layer

#### IBookImportMonitoringService

Handles file system monitoring and event processing:

```csharp
public interface IBookImportMonitoringService
{
    string UploadedFolderPath { get; }
    string TempDownloadsFolderPath { get; }
    string CoverImagesFolderPath { get; }
    string ProcessedFolderPath { get; }
    
    Task InitializeAsync();
    Task<List<BookImportFile>> GetPendingFilesAsync(int? limit = null);
    Task ProcessPendingFilesAsync();
    Task ScanUploadedFolderAsync();
    Task RescanFoldersAsync(List<string> folderPaths);
}
```

#### IBookImportMetadataService

Manages metadata extraction and enrichment:

```csharp
public interface IBookImportMetadataService
{
    Task<BookImportMetadata> ExtractFileMetadataAsync(BookImportFile file);
    Task ProcessFileAsync(BookImportFile file);
    Task<string> GenerateCoverImageAsync(BookImportFile file);
}
```

#### IFilenamePatternExtractor

Provides pattern-based filename management:

```csharp
public interface IFilenamePatternExtractor
{
    IReadOnlyDictionary<string, NamingTemplate> Templates { get; }
    NamingTemplate GenerateNamingPattern(BookImportFile file, BookImportMetadata metadata);
    List<PatternExtractionResult> ExtractPatternsFromFiles(List<BookImportFile> files);
    List<PlaceholderInfo> GetAvailablePlaceholders();
}
```

#### IBookImportBulkService

Handles bulk operations and library integration:

```csharp
public interface IBookImportBulkService
{
    Task<FinalizeResponse> FinalizeAndImportFilesAsync(
        List<int> fileIds,
        int? libraryId = null,
        string? targetPath = null,
        string? namingPattern = null,
        bool extractCovers = true,
        bool extractMetadata = true,
        bool moveFiles = true);
    
    Task<bool> DiscardFilesAsync(List<int> fileIds, bool deletePhysicalFiles = false, string? reason = null);
    Task<bool> ReprocessFilesAsync(List<int> fileIds, bool reextractMetadata = true, bool recalculateCovers = true);
    Task<bool> AssignFilesToLibraryAsync(List<int> fileIds, int libraryId, string? targetPath = null);
    Task<BulkOperationResult> ExecuteBulkOperationAsync(BulkOperationRequest request);
    Task<List<LibraryInfo>> GetAvailableLibrariesAsync();
}
```

## API Endpoints

### 1. List Files

**Endpoint:** `GET /api/bookimport/files`

**Request Parameters:**
- `page`: Page number (default: 1)
- `pageSize`: Number of items per page (default: 20)
- `searchText`: Search query string
- `formats`: Filter by file format
- `contentTypes`: Filter by content type
- `statuses`: Filter by file status
- `startDate`: Filter by creation date (start)
- `endDate`: Filter by creation date (end)
- `libraryIds`: Filter by library ID
- `showPendingOnly`: Show only pending files
- `showImportedOnly`: Show only imported files
- `sortBy`: Field to sort by
- `sortAscending`: Sort order

**Response:**
```json
{
  "files": [...],
  "totalFiles": 100,
  "selectedFileCount": 50,
  "currentPage": 1,
  "totalPages": 5,
  "pageSize": 20,
  "statusCounts": {
    "PendingReview": 20,
    "Processing": 15,
    "Finalized": 65
  },
  "availableLibraries": [...],
  "filters": {...},
  "sorting": {...}
}
```

### 2. Get File by ID

**Endpoint:** `GET /api/bookimport/files/{id}`

**Response:**
```json
{
  "id": 1,
  "fileName": "Harry-Potter-Vol.01.epub",
  "filePath": "/bookimport/uploaded/epub/Harry-Potter-Vol.01.epub",
  "fileSize": 1048576,
  "status": "PendingReview",
  "originalMetadata": "{...}",
  "libraryId": 1,
  "targetPath": "Books/Light-Novels/Harry-Potter",
  "namingPattern": "{Title}-Vol.{SeriesNumber}.{Extension}",
  "fileFormat": "Epub",
  "contentType": "Book",
  "coverImagePath": "/bookimport/cover-images/cover_1_Harry-Potter-Vol.01.epub",
  "originalFileName": "Harry-Potter.epub",
  "processingNotes": "File imported successfully",
  "created": "2024-01-15T10:30:00Z",
  "lastModified": "2024-01-15T10:30:00Z",
  "createdUtc": "2024-01-15T10:30:00Z",
  "lastModifiedUtc": "2024-01-15T10:30:00Z",
  "finalizedAt": "2024-01-15T10:30:00Z",
  "uploadedBy": 1,
  "isImported": true,
  "batchId": "Batch-20240115-103000",
  "metadataDetails": {
    "title": "Harry Potter and the Philosopher's Stone",
    "authors": ["J.K. Rowling"],
    "publisher": "Bloomsbury",
    "publicationDate": "2024-01-15",
    "isbn": "9780747532699",
    "description": "The first book in the Harry Potter series...",
    "genres": ["Fantasy", "Young Adult"],
    "language": "English",
    "pageCount": 309
  }
}
```

### 3. Finalize Files

**Endpoint:** `POST /api/bookimport/files/finalize`

**Request Body:**
```json
{
  "fileIds": [1, 2, 3],
  "libraryId": 1,
  "targetPath": "Books/Light-Novels/Overlord",
  "namingPattern": "{Title}-Vol.{SeriesNumber}.{Extension}",
  "extractCovers": true,
  "extractMetadata": true,
  "moveFiles": true
}
```

**Response:**
```json
{
  "successCount": 3,
  "errorCount": 0,
  "finalizedFileIds": [1, 2, 3],
  "errors": [],
  "batchId": "Batch-20240115-103000",
  "libraryInfo": {
    "id": 1,
    "name": "Main Library",
    "rootPath": "/library/books",
    "supportsFolderWatching": true
  },
  "processingTimeMs": 1500
}
```

### 4. Extract Patterns

**Endpoint:** `POST /api/bookimport/files/extract-pattern`

**Request Body:**
```json
{
  "fileIds": [1, 2, 3],
  "patternType": "All",
  "libraryId": 1,
  "customTemplates": [...]
}
```

**Response:**
```json
{
  "results": [
    {
      "fileId": 1,
      "originalFileName": "Harry-Potter.epub",
      "suggestedFileName": "Harry-Potter-Vol.01-Ch.001-2024.epub",
      "extractedValues": {
        "Title": "Harry Potter",
        "SeriesNumber": "01",
        "ChapterNumber": "001",
        "Published": "2024"
      },
      "confidenceScore": 95
    }
  ],
  "suggestedTemplate": "{Title}-Vol.{SeriesNumber}-Ch.{ChapterNumber}-{Published}.{Extension}",
  "availablePlaceholders": [
    {
      "name": "Title",
      "description": "The main title of the book or series",
      "pattern": "(.+?)",
      "exampleValue": "Harry Potter"
    }
  ],
  "recommendations": "Consider applying the suggested naming pattern for consistent file organization."
}
```

### 5. Discard Files

**Endpoint:** `POST /api/bookimport/files/discard`

**Request Body:**
```json
{
  "fileIds": [1, 2, 3],
  "deletePhysicalFiles": true,
  "reason": "File quality review completed"
}
```

### 6. Rescan Folders

**Endpoint:** `POST /api/bookimport/rescan`

**Request Body:**
```json
{
  "scanAll": true,
  "folderPaths": [
    "/bookimport/uploaded",
    "/bookimport/temp-downloads"
  ],
  "processNewFilesOnly": false,
  "reprocessExisting": true
}
```

**Response:**
```json
{
  "success": true,
  "scannedFolders": 4,
  "totalFiles": 100,
  "pendingFiles": 20,
  "message": "Folder rescan completed successfully"
}
```

### 7. Get Statistics

**Endpoint:** `GET /api/bookimport/statistics`

**Response:**
```json
{
  "statistics": {
    "totalFiles": 100,
    "totalFileSize": 524288000,
    "pendingReviewCount": 20,
    "processingCount": 15,
    "finalizedCount": 65,
    "pendingDeletionCount": 5,
    "errorCount": 3,
    "importedCount": 85,
    "averageFileSize": 5242880,
    "batchCount": 10,
    "libraryCount": 3,
    "lastUpdated": "2024-01-15T10:30:00Z"
  },
  "statusCounts": {
    "PendingReview": 20,
    "Processing": 15,
    "Finalized": 65,
    "PendingDeletion": 5,
    "Error": 3
  },
  "formatCounts": {
    "Epub": 40,
    "Pdf": 30,
    "Cbz": 20,
    "Cbr": 10
  }
}
```

### 8. Update File Status

**Endpoint:** `PATCH /api/bookimport/files/{id}/status`

**Request Body:**
```json
{
  "status": "Processing"
}
```

### 9. Update File Metadata

**Endpoint:** `PATCH /api/bookimport/files/{id}/metadata`

**Request Body:**
```json
{
  "title": "Harry Potter and the Philosopher's Stone",
  "authors": ["J.K. Rowling"],
  "publisher": "Bloomsbury",
  "publicationDate": "2024-01-15",
  "isbn": "9780747532699",
  "description": "The first book in the Harry Potter series...",
  "genres": ["Fantasy", "Young Adult"],
  "language": "English",
  "pageCount": 309
}
```

### 10. Execute Bulk Operation

**Endpoint:** `POST /api/bookimport/bulk-operation`

**Request Body:**
```json
{
  "operationType": "Import",
  "fileIds": [1, 2, 3],
  "libraryId": 1,
  "parameters": {
    "targetPath": "Books/Light-Novels",
    "namingPattern": "{Title}-Vol.{SeriesNumber}.{Extension}",
    "deletePhysicalFiles": "true",
    "reextractMetadata": "true",
    "recalculateCovers": "true"
  }
}
```

## Directory Structure

### BookImport Folder Layout

```
/bookimport/
├── uploaded/                          # New files awaiting processing
│   ├── epub/                          # EPUB files
│   │   ├── Harry-Potter-Vol.01.epub
│   │   └── Overlord-Vol.01.epub
│   ├── pdf/                           # PDF files
│   │   ├── One-Piece-Ch.001.pdf
│   │   └── Demon-Slayer-Vol.01.pdf
│   └── comics/                        # Comic files (CBZ/CBR)
│       ├── Naruto-Vol.01.cbz
│       └── Bleach-Vol.01.cbr
├── temp-downloads/                    # Temporary processing files
│   ├── temp_1234567890.epub
│   └── temp_0987654321.pdf
├── cover-images/                      # Extracted cover images
│   ├── cover_1_Harry-Potter-Vol.01.epub/
│   │   ├── cover.json
│   │   └── thumbnail.png
│   └── cover_2_Overlord-Vol.01.epub/
│       ├── cover.json
│       └── thumbnail.png
└── processed/                         # Finalized files ready for library
    ├── finalized_1234567890.epub
    └── finalized_0987654321.pdf
```

### Library Integration Structure

```
/library/
├── Books/
│   └── {Series}/
│       ├── {Title}-v{Vol}-c{Chap}.epub
│       └── {Title}-v{Vol}-c{Chap}.pdf
├── Manga/
│   └── {Series}/
│       ├── {Title}-v{Vol}-c{Chap}.cbz
│       └── {Title}-v{Vol}-c{Chap}.cbr
└── Light-Novels/
    └── {Series}/
        ├── {Title}-v{Number}.epub
        └── {Title}-v{Number}.pdf
```

## Configuration

### BookImportOptions

```csharp
public class BookImportOptions
{
    /// <summary>
    /// Root path for book import operations
    /// </summary>
    public string ImportRootPath { get; set; } = "/bookimport";

    /// <summary>
    /// Interval for monitoring file system events (in milliseconds)
    /// </summary>
    public int MonitoringInterval { get; set; } = 5000;

    /// <summary>
    /// Maximum file size to process (in bytes)
    /// </summary>
    public long MaxFileSize { get; set; } = 500 * 1024 * 1024;

    /// <summary>
    /// Timeout duration for file operations (in seconds)
    /// </summary>
    public int OperationTimeout { get; set; } = 300;

    /// <summary>
    /// Supported file extensions for import
    /// </summary>
    public List<string> SupportedExtensions { get; set; } = new()
    {
        ".epub", ".pdf", ".cbz", ".cbr", ".png", ".jpg", ".jpeg", ".webp"
    };
}
```

### Docker Volume Configuration

```yaml
version: '3.8'

services:
  kavita:
    image: kavita/kavita:latest
    volumes:
      - ./bookimport:/bookimport
      - ./library:/books
      - ./config:/data
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=UTC
    ports:
      - 5000:5000

volumes:
  bookimport:
    driver: local
  library:
    driver: local
  config:
    driver: local
```

## Naming Templates

### 1. Book Template

**Pattern:** `{Title}-Vol.{SeriesNumber}.{Extension}`

**Example:** `Harry-Potter-Vol.01.epub`

**Placeholders:**
- `{Title}`: Main title of the book
- `{SeriesNumber}`: Volume or series number
- `{Extension}`: File extension

### 2. Manga Template

**Pattern:** `[{Publisher}] {Title}-Vol.{SeriesNumber}-Ch.{ChapterNumber}.{Extension}`

**Example:** `[Shueisha] One-Piece-Vol.01-Ch.001.cbz`

**Placeholders:**
- `{Publisher}`: Publisher name
- `{Title}`: Main title
- `{SeriesNumber}`: Volume number
- `{ChapterNumber}`: Chapter number
- `{Extension}`: File extension

### 3. Light Novel Template

**Pattern:** `{Title}-v{SeriesNumber}.{Extension}`

**Example:** `Overlord-v01.epub`

**Placeholders:**
- `{Title}`: Main title
- `{SeriesNumber}`: Version number
- `{Extension}`: File extension

### 4. Image Template

**Pattern:** `{Title}-S{Season}-Ch.{ChapterNumber}.{Extension}`

**Example:** `Seasonal-Greetings-S01-Ch.005.png`

**Placeholders:**
- `{Title}`: Main title
- `{Season}`: Season identifier
- `{ChapterNumber}`: Chapter number
- `{Extension}`: File extension

## Implementation Phases

### Phase 1: Core Foundation ✅

- [x] File system monitoring and event processing
- [x] Metadata extraction services
- [x] Core REST API implementation
- [x] Database schema setup

### Phase 2: Organization & Naming ✅

- [x] Pattern-based filename extraction
- [x] Directory structure automation
- [x] Content type classification
- [x] Bulk operation workflows

### Phase 3: User Interface (In Progress)

- [ ] BookImport Review Page
- [ ] File listing and filtering
- [ ] Library assignment interface
- [ ] Real-time status monitoring

### Phase 4: Enhancement (Planned)

- [ ] Performance optimization
- [ ] Advanced pattern matching
- [ ] Enhanced notifications
- [ ] Comprehensive documentation

## Usage Examples

### Uploading Files

Files can be uploaded to the `/bookimport/uploaded` directory through:
1. Direct file system copy
2. API upload endpoint
3. Web interface drag-and-drop

### Processing Workflow

1. **Upload**: Files are placed in the uploaded folder
2. **Detection**: File system events trigger processing
3. **Extraction**: Metadata and covers are extracted
4. **Organization**: Files are renamed and organized
5. **Finalization**: Files are moved to the library
6. **Integration**: Files are indexed and made available

### Bulk Operations

```csharp
// Example: Import multiple files to a library
var bulkService = serviceProvider.GetRequiredService<IBookImportBulkService>();

var result = await bulkService.FinalizeAndImportFilesAsync(
    fileIds: new List<int> { 1, 2, 3, 4, 5 },
    libraryId: 1,
    targetPath: "Books/Light-Novels",
    namingPattern: "{Title}-Vol.{SeriesNumber}.{Extension}",
    extractCovers: true,
    extractMetadata: true,
    moveFiles: true
);
```

## Best Practices

### File Naming

- Use consistent naming patterns across all files
- Include relevant metadata in filenames
- Maintain descriptive and readable file names

### Metadata Management

- Extract and preserve all available metadata
- Regularly update metadata as new information becomes available
- Use standardized metadata formats

### Performance Optimization

- Monitor file system events efficiently
- Implement caching for frequently accessed data
- Optimize file processing pipelines

### User Experience

- Provide clear visual feedback during processing
- Enable easy file selection and management
- Offer comprehensive import and export capabilities

## Troubleshooting

### Common Issues

1. **File Monitoring Not Triggering**
   - Check file system watcher configuration
   - Verify folder paths are correctly set
   - Ensure proper permissions for monitored directories

2. **Metadata Extraction Failures**
   - Validate file format compatibility
   - Check metadata file accessibility
   - Review extraction logs for errors

3. **Import Process Interruptions**
   - Monitor system resources during import
   - Implement retry mechanisms for failures
   - Maintain backup copies of critical files

### Diagnostic Tools

- Use the statistics endpoint to monitor system health
- Review processing logs for detailed insights
- Utilize the rescan functionality for comprehensive analysis

## Conclusion

The BookImport feature provides a robust and flexible solution for automatic book file imports in Kavita. By leveraging file system monitoring, metadata extraction, and pattern-based organization, it enables users to seamlessly integrate new content into their libraries with minimal manual intervention.

The modular architecture ensures scalability and extensibility, allowing for future enhancements and integrations. Through comprehensive API endpoints and intuitive services, the BookImport feature establishes a solid foundation for efficient content management and delivery.
