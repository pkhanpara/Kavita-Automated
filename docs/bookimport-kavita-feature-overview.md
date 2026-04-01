# Kavita BookImport Functionality - Technical Overview

## Executive Summary

BookImport is a zero-effort import feature that enables users to import book files into a watched folder, where Kavita automatically detects  and queues them for import. This functionality provides a seamless workflow for adding new books to the library without manual intervention.

---

## Core Workflow

```
┌─────────────────┐
│   User Action   │
│  Import Files     │
│  into Folder    │
└────────┬────────┘
         │
         ▼
┌──────────────────────────────────────────────────────────────┐
│                    BookImport Workflow                         │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  1. WATCH     ──▶  Monitors the BookImport folder 24/7         │
│                   using OS-level file system events          │
│                                                              │
│  2. DETECT    ──▶  Automatically identifies new files        │
│                   and filters supported formats              │
│                   (EPUB, PDF, CBZ, CBR, etc.)                │
│                                                              │
│  3. IMPORT    ──▶  Moves files to library storage and        │
│                   integrates them into the catalog           │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. File System Monitoring

**Component**: `BookimportMonitoringService`

**Responsibilities**:
- Monitors the BookImport folder 
- Detects file creation, modification, and deletion events
- Filters supported file formats (EPUB, PDF, CBZ, CBR, etc.)
- Performs recursive scanning for new directories
- Handles file stability checks to prevent processing incomplete downloads


### 2. Event Processing Pipeline

**Component**: `BookimportEventHandlerService`

**Responsibilities**:
- Manages a buffered queue of file events
- Processes files asynchronously to prevent blocking
- Coordinates metadata extraction and enrichment
- Sends notifications for processing status

**Processing Flow**:
1. **Event Detection**: File system events trigger enqueue operations
2. **Queue Processing**: Background worker thread processes queued events
3. **Metadata Attachment**: Initial metadata extracted from file properties
4. **Importing**: According to the book type, create appropriate directory structure and rename the file according to Scanner expectations. 
5. **Notification**: User notifications for processing milestones

### 3. Metadata Management

**Component**: `BookimportMetadataService`

**Metadata Sources**:
- **Internal Extraction**: File-based metadata (title, authors, format)

**Data Model**:
```
BookimportFileEntity
├── id: Long (primary key)
├── filePath: String (absolute path)
├── fileName: String
├── fileSize: Long
├── status: Enum (PENDING_REVIEW | FINALIZED)
├── originalMetadata: JSON (extracted from file)
├── createdAt: Timestamp
└── updatedAt: Timestamp
```

**Metadata Extraction Process**:
1. **Initial Extraction**: Extracts basic metadata from file properties and content
2. **Cover Image**: Extracts and saves cover images for preview

### 4. File Pattern Recognition

**Component**: `FilenamePatternExtractor`

**Capabilities**:
- Parses filenames using customizable regex patterns
- Extracts metadata from filename structures
- Supports placeholders for common metadata fields
- Provides preview and full extraction modes

**Supported Placeholders**:
| Placeholder | Format | Description |
|-------------|--------|-------------|
| `{Title}` | `(.+?)` | Book title |
| `{Authors}` | `(.+?)` | Author names (comma/semicolon/ampersand separated) |
| `{SeriesName}` | `(.+?)` | Series name |
| `{SeriesNumber}` | `(\d+(\.\d+)?)` | Series volume/number |
| `{Published}` | `(.+?)` | Publication date |
| `{Publisher}` | `(.+?)` | Publisher name |
| `{Language}` | `([a-zA-Z]+)` | Language code |
| `{SeriesTotal}` | `(\d+)` | Total books in series |
| `{ISBN10}` | `(\d{9}[0-9Xx])` | ISBN-10 identifier |
| `{ISBN13}` | `([0-9]{13})` | ISBN-13 identifier |
| `{ASIN}` | `(B[A-Za-z0-9]{9}|\d{9}[0-9Xx])` | Amazon identifier |

**Pattern Configuration**:
- User-defined patterns via UI
- Template patterns for common naming conventions
- Real-time preview with sample files

### 5. Bulk Operations

**Components**:
- `BookimportBulkService`: Metadata bulk updates
- `BookimportNotificationService`: User notifications

**Operations**:
- **Pattern-Based Extraction**: Extract metadata from filenames
- **Mass Deletion**: Remove files with cleanup

---

## API Endpoints

### BookImport REST API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/bookimport/notification` | GET | Retrieve notification summary |
| `/api/v1/bookimport/files` | GET | List files with pagination and filtering |
| `/api/v1/bookimport/files/discard` | POST | Discard selected files |
| `/api/v1/bookimport/rescan` | POST | Trigger manual rescan |
| `/api/v1/bookimport/files/extract-pattern` | POST | Extract metadata from filename patterns |


## Configuration
### Docker Volume Mounting

```yaml
volumes:
  - ./bookimport:/bookimport
```

**Directory Structure** (Following Scanner Conventions):

The BookImport folder follows Kavita's [Scanner directory structure guidelines](https://wiki.kavitareader.com/guides/scanner/managefiles/), ensuring:
- **Series Isolation**: Each series resides in dedicated folders
- **No Root Files**: All files nested within series folders
- **Special Folders**: Specials grouped with SP markers

```
/bookimport/
├── [uploaded files]                    # Files awaiting processing
│   ├── [Series Name]/                  # Series-specific folders
│   │   ├── [Series Name] v01 c001.cbz  # Volume and chapter markers
│   │   ├── [Series Name] SP01.cbz      # Special episodes (SP marker)
│   │   └── [Specials]/                  # Nested specials folder
│   │       └── [Series Name] SP02.cbz
│   └── series/
│       ├── volume1.epub
│       ├── volume2.pdf
│       └── volume3.cbz
└── [temporary files]
    ├── temp-downloads/                 # In-progress downloads
    └── cover-images/                   # Extracted covers
```

**Key Conventions Applied**:
- **Parentheses `()`**: Content removed during parsing (e.g., `Series (2024)`)
- **Curly Brackets `{}`**: Distinguish series with similar names
- **Special Markers**: `SP01`, `SP02` for special episodes
- **Volume Formats**: `v1`, `vol. 1`, `volume 01`, `tome 2`

---

## User Interface

### BookImport Review Page

**Features**:
- **File Listing**: Paginated display of pending files
- **Library Assignment**: Select destination library and subpath for each file
- **Real-time Status**: Progress indicators and notifications


## Processing Workflow Details

### File Lifecycle States

1. **PENDING_REVIEW**: New files awaiting user review
   - Initial metadata extracted
   - Awaiting library/path assignment

2. **FINALIZED**: Files ready for or completed import
   - Library and path assignments confirmed
   - File moved to library storage
   - Integrated into book catalog

### Import Process

**Step 1: File Detection**
- File system watcher detects new file
- Stability check ensures complete file transfer
- Duplicate detection prevents reprocessing

**Step 2: Metadata Processing**
- Extract file-based metadata (title, authors, format)
- Extract cover image if available

**Step 3: User Review**
- Present files in UI for review
- Assign library and storage path
- Enable bulk operations

**Step 4: Finalization**
- Move files to target library location
- Apply naming conventions
- Register in book catalog
- Cleanup source files

**Step 5: Integration**
- Update library indexing
- Notify users of completion

---

## Performance Considerations

### Scalability Features

**File Processing**:
- Asynchronous processing with event-driven architecture
- Buffered queue for handling high-volume file imports
- Parallel metadata fetching for multiple sources
- Efficient file stability monitoring

**Database Optimization**:
- Indexed file path lookups for duplicate detection
- Connection pooling for database operations
- Batch operations for bulk processing

### Monitoring and Logging

**Key Metrics**:
- File system events (create, modify, delete)
- Queue depth and processing throughput
- Metadata extraction success rates
- Import completion statistics

**Logging**:
- Structured logging for event tracking
- Error handling with detailed diagnostics
- User notification integration

---

## Best Practices
**User Workflow**:
- Regular review of pending files
- Establish default library and path assignments
- Utilize bulk operations for efficient management

---

## Troubleshooting

### Common Issues

**File Not Detected**:
- Verify volume mounting in Docker configuration
- Check file system permissions
- Confirm WatchService is active

**Metadata Extraction Failures**:
- Review network connectivity for external services
- Validate file format compatibility
- Check metadata source configurations

**Processing Delays**:
- Monitor queue depth and processing rates
- Adjust timeout settings for large files
- Review resource utilization

### Diagnostic Tools

**Monitoring Commands**:
```bash
# Check BookImport folder status
ls -la /bookimport

# Review file events in logs
docker logs --tail 100 kavita | grep -i bookimport

# Verify database records
SELECT * FROM bookimport_file WHERE status = 'PENDING_REVIEW';
```

---

## Future Enhancements

### Potential Improvements

1. **Advanced Pattern Matching**: Enhanced regex capabilities for complex naming schemes
2. **Smart Categorization**: file organization based on content analysis
3. **Incremental Updates**: Support for file version tracking and updates
4. **Enhanced Notifications**: Real-time alerts for processing events

---

## Summary

BookImport provides a robust, user-friendly mechanism for seamless book import in Kavita. By leveraging file system monitoring, metadata extraction, and intelligent processing workflows, it eliminates manual intervention while ensuring high-quality library integration. The architecture supports scalability, maintainability, and extensibility, making it a cornerstone feature for digital library management.

---

