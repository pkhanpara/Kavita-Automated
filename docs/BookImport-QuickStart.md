# BookImport Quick Start Guide

## Getting Started with Automatic Book Import

This guide provides a quick overview of how to use the BookImport feature in Kavita for automatic file management and library organization.

## Prerequisites

- Kavita server (v0.7.0 or later)
- Docker (optional, for containerized deployment)
- Access to file system or network storage

## Installation

### Option 1: Docker Deployment (Recommended)

Using the provided Docker Compose configuration:

```bash
# Clone the repository
git clone https://github.com/kavitabook/kavita.git
cd kavita

# Start with BookImport volumes
docker-compose -f docker-bookimport.yml up -d
```

### Option 2: Manual Setup

1. **Create Directory Structure**

```bash
mkdir -p /bookimport/{uploaded/{epub,pdf,comics},temp-downloads,cover-images,processed}
mkdir -p /library/{Books,Manga,Light-Novels}
mkdir -p /config/{logs,plugins}
```

2. **Configure Environment Variables**

```bash
export KAVITA_BOOKIMPORT_ENABLED=true
export KAVITA_BOOKIMPORT_ROOT_PATH=/bookimport
export KAVITA_BOOKIMPORT_MONITORING_INTERVAL=5000
export KAVITA_BOOKIMPORT_MAX_FILE_SIZE=524288000
export KAVITA_BOOKIMPORT_OPERATION_TIMEOUT=300
```

## Usage

### 1. Uploading Files

Files can be uploaded to the BookImport system through multiple methods:

**Method A: Direct File System Upload**

```bash
# Copy files to the uploaded folder
cp your-book.epub /bookimport/uploaded/epub/
cp your-comic.cbz /bookimport/uploaded/comics/
```

**Method B: API Upload**

```bash
# Using curl to upload a file
curl -X POST http://localhost:5000/api/bookimport/files/finalize \
  -H "Content-Type: application/json" \
  -d '{
    "fileIds": [1, 2, 3],
    "libraryId": 1,
    "targetPath": "Books/Light-Novels",
    "namingPattern": "{Title}-Vol.{SeriesNumber}.{Extension}"
  }'
```

**Method C: Web Interface**

Navigate to the BookImport section in the Kavita web interface to upload files via drag-and-drop.

### 2. Monitoring File Processing

The BookImport system automatically monitors the uploaded folders for new files:

```bash
# Check monitoring status
curl http://localhost:5000/api/bookimport/statistics
```

### 3. Processing Workflows

**Automatic Processing:**
- File system events trigger processing when new files are detected
- Metadata extraction occurs automatically
- Cover images are generated for supported file types
- Files are renamed according to configured patterns

**Manual Processing:**
- Use the API to trigger manual rescans
- Review and finalize files through the web interface
- Apply custom naming patterns as needed

## Configuration

### BookImport Configuration Options

| Setting | Description | Default | Recommended |
|---------|-------------|---------|-------------|
| `ImportRootPath` | Root directory for book import operations | `/bookimport` | `/bookimport` |
| `MonitoringInterval` | File system event polling interval (ms) | 5000 | 5000 |
| `MaxFileSize` | Maximum file size for processing (bytes) | 524288000 | 524288000 |
| `OperationTimeout` | Timeout for file operations (seconds) | 300 | 300 |
| `SupportedExtensions` | File extensions to process | `.epub,.pdf,.cbz,.cbr` | `.epub,.pdf,.cbz,.cbr,.png,.jpg` |

### Naming Pattern Configuration

Configure naming patterns through the web interface or API:

```json
{
  "pattern": "{Title}-Vol.{SeriesNumber}-Ch.{ChapterNumber}-{Published}.{Extension}",
  "placeholders": [
    {"name": "Title", "description": "Main book or series title"},
    {"name": "SeriesNumber", "description": "Volume or series number"},
    {"name": "ChapterNumber", "description": "Chapter number"},
    {"name": "Published", "description": "Publication date"},
    {"name": "Extension", "description": "File extension"}
  ]
}
```

## Workflow

### File Processing Pipeline

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Upload    │ ──► │  Monitor    │ ──► │  Process    │ ──► │   Finalize  │
│   Files     │     │  Events     │     │  Metadata   │     │  Import     │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
```

### Step-by-Step Workflow

1. **Upload Phase**
   - Files are placed in the uploaded folder
   - Supported formats: EPUB, PDF, CBZ, CBR, PNG, JPG
   - File system events trigger processing

2. **Monitoring Phase**
   - File system watcher detects new files
   - Stability checks ensure complete file transfers
   - Format filtering identifies supported file types

3. **Processing Phase**
   - Metadata extraction from file content
   - Cover image generation for visual representation
   - Pattern-based filename application

4. **Finalization Phase**
   - Files are moved to appropriate library locations
   - Database records are updated
   - Integration with existing library structure

## API Usage

### Common API Operations

**List Files:**
```bash
GET /api/bookimport/files?page=1&pageSize=20&searchText=Harry
```

**Finalize Files:**
```bash
POST /api/bookimport/files/finalize
Content-Type: application/json

{
  "fileIds": [1, 2, 3],
  "libraryId": 1,
  "targetPath": "Books/Light-Novels",
  "namingPattern": "{Title}-Vol.{SeriesNumber}.{Extension}"
}
```

**Extract Patterns:**
```bash
POST /api/bookimport/files/extract-pattern
Content-Type: application/json

{
  "fileIds": [1, 2, 3],
  "patternType": "All"
}
```

**Rescan Folders:**
```bash
POST /api/bookimport/rescan
Content-Type: application/json

{
  "scanAll": true,
  "processNewFilesOnly": false
}
```

## Best Practices

### File Organization

- Maintain consistent folder structure for different file types
- Use descriptive file names with relevant metadata
- Organize files by content type (Books, Manga, Light Novels)

### Performance Optimization

- Monitor file system resources during processing
- Implement regular maintenance schedules
- Utilize caching for improved performance

### User Experience

- Provide clear visual feedback during operations
- Enable easy file selection and management
- Offer comprehensive import and export capabilities

## Troubleshooting

### Common Issues and Solutions

**Issue: Files not being detected**
- Solution: Verify file system watcher is running and folder paths are correctly configured

**Issue: Metadata extraction failures**
- Solution: Check file format compatibility and review extraction logs

**Issue: Import process interruptions**
- Solution: Monitor system resources and implement retry mechanisms

### Diagnostic Commands

```bash
# Check service status
docker ps | grep kavita

# View logs
docker logs -f kavita-bookimport

# Monitor file system events
watch -n 5 ls -lh /bookimport/uploaded/

# Test API connectivity
curl -I http://localhost:5000/api/bookimport/statistics
```

## Next Steps

After completing the initial setup:

1. Explore the BookImport web interface for advanced features
2. Customize naming patterns to match your library structure
3. Implement automated backup strategies for imported files
4. Monitor system performance and optimize as needed
5. Review documentation for additional configuration options

## Support

For additional assistance:
- Review the comprehensive [BookImport Implementation Guide](./BookImport-Implementation.md)
- Access the API documentation for detailed endpoint specifications
- Join the Kavita community for user discussions and support

---

*This Quick Start Guide provides an overview of the BookImport feature. For detailed implementation information, please refer to the main documentation.*
