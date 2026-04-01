# BookImport Module

## Overview

The BookImport module provides automatic book file import capabilities for Kavita, enabling zero-effort file management and library organization. This module monitors file system events, extracts metadata, and organizes files according to configurable naming patterns.

## Features

- **Automatic File Monitoring**: Real-time detection of file system events for seamless file processing
- **Metadata Extraction**: Automatic extraction of metadata from uploaded files including title, authors, publisher, and publication date
- **Pattern-Based Naming**: Intelligent filename generation using configurable templates and placeholders
- **Cover Image Generation**: Automatic creation of cover images for supported file formats
- **Bulk Operations**: Comprehensive support for mass file operations including import, discard, and reprocessing
- **Library Integration**: Seamless integration with existing Kavita library structure and services

## Project Structure

```
Kavita/
├── docs/
│   ├── BookImport-Implementation.md    # Comprehensive implementation guide
│   ├── BookImport-QuickStart.md        # Quick start guide
│   └── automatic-book-import.md         # Original feature specification
├── Kavita.API/
│   ├── Database/
│   │   ├── IDataContext.cs              # Updated with BookImportFiles DbSet
│   │   └── IUnitOfWork.cs               # Updated with BookImportRepository
│   └── Repositories/
│       └── BookImportRepository.cs      # Repository implementation
├── Kavita.Models/
│   ├── Entities/
│   │   └── BookImportFile.cs            # Core entity definition
│   └── DTOs/
│       └── BookImport/
│           ├── BookImportFileDto.cs     # File DTO
│           ├── BookImportDto.cs         # Main DTO
│           └── BookImportRequestDto.cs  # Request/Response DTOs
├── Kavita.Services/
│   ├── BookImport/
│   │   ├── BookImportMonitoringService.cs
│   │   ├── BookImportMetadataService.cs
│   │   ├── FilenamePatternExtractor.cs
│   │   ├── BookImportBulkService.cs
│   │   ├── BookImportInterfaces.cs
│   │   └── BookImportServiceCollectionExtensions.cs
│   └── Kavita.Services.csproj
├── Kavita.Services.Tests/
│   └── BookImport/
│       └── BookImportServiceTests.cs    # Service tests
├── Kavita.Server/
│   └── Controllers/
│       └── BookImportController.cs      # REST API controller
└── docker-bookimport.yml                # Docker configuration
```

## Core Components

### 1. BookImportFile Entity

The central data model for tracking files through their lifecycle:

- **Id**: Unique identifier
- **FilePath**: Full path in the file system
- **FileName**: Name of the file
- **FileSize**: Size in bytes
- **Status**: Processing status (PendingReview, Processing, Finalized, etc.)
- **OriginalMetadata**: JSON-serialized metadata
- **LibraryId**: Associated library identifier
- **TargetPath**: Target path within the library
- **NamingPattern**: Applied naming pattern
- **FileFormat**: File format type (Epub, Pdf, Cbz, Cbr)
- **ContentType**: Content type classification
- **CoverImagePath**: Path to cover image
- **Created/LastModified**: Timestamps for lifecycle tracking

### 2. Services

#### IBookImportMonitoringService

Manages file system monitoring and event processing:

- File system event detection
- Stability checks for complete file transfers
- Format filtering for supported file types
- Background processing and monitoring

#### IBookImportMetadataService

Handles metadata extraction and enrichment:

- Metadata extraction from file content
- Cover image generation
- Pattern-based filename application
- Metadata persistence and updates

#### IFilenamePatternExtractor

Provides pattern-based filename management:

- Template-based naming
- Pattern extraction and application
- Placeholder management
- Pattern recommendation and optimization

#### IBookImportBulkService

Manages bulk operations and library integration:

- File finalization and import
- File discard and cleanup
- File reprocessing and updates
- Library assignment and management

### 3. API Endpoints

The BookImport module provides the following REST API endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/bookimport/files` | GET | List files with pagination |
| `/api/bookimport/files/{id}` | GET | Get file by ID |
| `/api/bookimport/files/finalize` | POST | Finalize and import files |
| `/api/bookimport/files/extract-pattern` | POST | Extract metadata patterns |
| `/api/bookimport/files/discard` | POST | Discard selected files |
| `/api/bookimport/rescan` | POST | Trigger manual rescan |
| `/api/bookimport/statistics` | GET | Get system statistics |
| `/api/bookimport/files/{id}/status` | PATCH | Update file status |
| `/api/bookimport/files/{id}/metadata` | PATCH | Update file metadata |
| `/api/bookimport/bulk-operation` | POST | Execute bulk operations |

## Configuration

### Environment Variables

```bash
# Enable BookImport
KAVITA_BOOKIMPORT_ENABLED=true

# Root path for book import
KAVITA_BOOKIMPORT_ROOT_PATH=/bookimport

# Monitoring interval (milliseconds)
KAVITA_BOOKIMPORT_MONITORING_INTERVAL=5000

# Maximum file size (bytes)
KAVITA_BOOKIMPORT_MAX_FILE_SIZE=524288000

# Operation timeout (seconds)
KAVITA_BOOKIMPORT_OPERATION_TIMEOUT=300
```

### Directory Structure

```
/bookimport/
├── uploaded/
│   ├── epub/          # EPUB files
│   ├── pdf/           # PDF files
│   └── comics/        # Comic files
├── temp-downloads/    # Temporary files
├── cover-images/      # Cover images
└── processed/         # Processed files
```

## Usage

### 1. File Upload

Upload files to the `/bookimport/uploaded` directory:

```bash
# Using file system
cp your-book.epub /bookimport/uploaded/epub/

# Or use the API
curl -X POST http://localhost:5000/api/bookimport/files/finalize \
  -H "Content-Type: application/json" \
  -d '{"fileIds":[1,2,3],"libraryId":1}'
```

### 2. Processing Workflow

The BookImport system automatically processes files through the following stages:

1. **Upload**: Files are placed in the uploaded folder
2. **Detection**: File system events trigger processing
3. **Extraction**: Metadata and covers are extracted
4. **Organization**: Files are renamed and organized
5. **Finalization**: Files are moved to the library
6. **Integration**: Files are indexed and made available

### 3. Bulk Operations

Execute bulk operations for mass file management:

```csharp
// Example: Import multiple files
var bulkService = serviceProvider.GetRequiredService<IBookImportBulkService>();

var result = await bulkService.FinalizeAndImportFilesAsync(
    fileIds: new List<int> { 1, 2, 3 },
    libraryId: 1,
    targetPath: "Books/Light-Novels",
    namingPattern: "{Title}-Vol.{SeriesNumber}.{Extension}",
    extractCovers: true,
    extractMetadata: true,
    moveFiles: true
);
```

## Testing

Run the BookImport service tests:

```bash
# Using .NET CLI
dotnet test Kavita.Services.Tests/Kavita.Services.Tests.csproj --filter "FullyQualifiedName~BookImport"
```

## Docker Deployment

Deploy using Docker Compose:

```bash
# Start with BookImport configuration
docker-compose -f docker-bookimport.yml up -d

# Monitor service logs
docker logs -f kavita-bookimport

# Check service status
docker ps | grep kavita
```

## Documentation

- [BookImport Implementation Guide](./BookImport-Implementation.md) - Comprehensive technical documentation
- [BookImport Quick Start Guide](./BookImport-QuickStart.md) - Getting started guide
- [Original Feature Specification](./automatic-book-import.md) - Feature requirements and architecture

## Contributing

Contributions are welcome! Please review the [CONTRIBUTING.md](../../CONTRIBUTING.md) guidelines for development practices.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

*For questions or support, please refer to the documentation or contact the development team.*
