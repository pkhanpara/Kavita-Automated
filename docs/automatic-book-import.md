# BookImport: Automatic Book Import Feature

---

## Summary

BookImport enables zero-effort book file imports into a library folder. Kavita automatically detects, processes, and organizes files according to the Scanner's directory structure requirements, eliminating manual intervention except for choosing library when adding new books.

---

## Problem & Solution

**Challenges:**
- Manual file organization into appropriate directory structures
- Consistent file naming across diverse formats (EPUB, PDF, CBZ, CBR)
- Time-consuming metadata extraction

**Solution:**
An automated workflow that monitors an import folder, extracts metadata, creates Scanner-compatible directory structures, and integrates files into the library catalog.

---

## Workflow Architecture

```
User Action → Monitor Files → Extract Metadata → Organize & Rename → Finalize Import
     ↓              ↓                ↓                 ↓                  ↓
  Upload      File System       Pattern-based    Directory           Library
  Files       Events            Naming           Creation            Catalog
```

**Watched Folder Structure:**
```
/bookimport/
├── uploaded/              # New files awaiting processing
│   ├── epub/
│   ├── pdf/
│   └── comics/
├── temp-downloads/        # Temporary processing files
├── cover-images/          # Extracted covers
└── processed/             # Finalized files ready for library
```

---

## Core Components

### 1. BookimportMonitoringService
- **File System Monitoring**: OS-level event detection for file creation, modification, deletion
- **Stability Checks**: Validates complete file transfers before processing
- **Format Filtering**: Supports EPUB, PDF, CBZ, CBR, and image formats

### 2. BookimportMetadataService
- **Data Model**:
  ```
  BookimportFileEntity
  ├── id, filePath, fileName, fileSize
  ├── status: PENDING_REVIEW | FINALIZED
  ├── originalMetadata: {title, authors, format, publicationDate, coverImage}
  └── assignedLibrary, targetPath, timestamps
  ```

### 3. FilenamePatternExtractor
- **Supported Placeholders**:

| Placeholder | Pattern | Example |
|-------------|---------|---------|
| `{Title}` | `(.+?)` | Harry Potter |
| `{SeriesNumber}` | `(\d+(\.\d+)?)` | Vol.01 |
| `{Published}` | `(.+?)` | 2024 |
| `{ISBN13}` | `([0-9]{13})` | 9780747532699 |

- **Naming Templates**:
  - **Book**: `{Title} - {SeriesName} - Vol.{SeriesNumber}.{Extension}`
  - **Manga**: `[{Publisher}] {Title} - Vol.{SeriesNumber} Ch.{SeriesNumber}.{Extension}`
  - **Light Novel**: `{Title} - {Authors} - {SeriesName} - {SeriesNumber}.epub`

### 4. BookimportBulkService
- Library assignment and path optimization
- Bulk operations for mass file processing
- File movement and catalog integration

---

## Directory Structure (Scanner-Compatible)

**Target Library Layout:**
```
/library/
│── {Series}/
│       ├── {Title}-v{Vol}-c{Chap}.cbz
│       └── [Specials]/
├──{Title}/
│      ├── {Title}-v{Vol}.epub
└── {Series}/
│      ├── {Chapter}
│            ├──{image}.png
```

**Content Type Classification:**

| Library Type | Formats | Pattern |
|--------------|---------|---------|
| Manga | CBZ, CBR | `{Title}-v{Vol}-c{Chap}.cbz` |
| Book | EPUB, PDF | `{Title}-Vol.{Number}.{ext}` |
| LightNovel | EPUB, PDF | `{Title}-v{Number}.{ext}` |
| Image | PNG, JPG, WEBP | `{Title}-S{Season}-c{Chap}.ext` |

---

## API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/v1/bookimport/files` | GET | List files with pagination |
| `/api/v1/bookimport/files/discard` | POST | Discard selected files |
| `/api/v1/bookimport/rescan` | POST | Trigger manual rescan |
| `/api/v1/bookimport/files/extract-pattern` | POST | Extract metadata from patterns |
| `/api/v1/bookimport/files/finalize` | POST | Finalize and move files to library |

**Example API Call:**
```json
POST /api/v1/bookimport/files/finalize
{
  "fileIds": [1, 2, 3],
  "libraryId": 1,
  "targetPath": "Books/Light-Novels/Overlord",
  "namingPattern": "{Title}-Vol.{SeriesNumber}.{Extension}"
}
```

---

## Docker Configuration

```yaml
volumes:
  - ./bookimport:/bookimport
  - ./library:/books
  - ./config:/data
```

---

## Implementation Phases

**Phase 1: Core Foundation**
- File system monitoring and event processing
- Metadata extraction services
- Core REST API implementation
- Database schema setup

**Phase 2: Organization & Naming**
- Pattern-based filename extraction
- Directory structure automation
- Content type classification
- Bulk operation workflows

**Phase 3: User Interface**
- BookImport Review Page
- File listing and filtering
- Library assignment interface
- Real-time status monitoring

**Phase 4: Enhancement**
- Performance optimization
- Advanced pattern matching
- Enhanced notifications
- Documentation

---

## Benefits

**User Benefits:**
- Zero-effort file import and organization
- Consistent naming and metadata across library
- Reduced manual intervention and errors
- Scalable solution for growing collections

**Technical Benefits:**
- Modular, extensible architecture
- Seamless Scanner service integration
- Asynchronous processing for performance
- RESTful API for external integrations

