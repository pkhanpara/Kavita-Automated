# Kavita Importer - Quick Reference Guide

## Quick Start

### Configuration Settings

```json
{
  "enableImport": true,
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
      "PDF": 95,
      "CBZ": 90,
      "CBR": 85
    }
  },
  "blacklistPatterns": {
    "folders": [".DS_Store", "Thumbs.db"],
    "files": ["*.tmp", "*.log"]
  }
}
```

### Supported File Formats

| Format | Priority | Extension | Use Case |
|--------|----------|-----------|----------|
| **EPUB** | 100 | .epub | Electronic publications |
| **PDF** | 95 | .pdf | Portable documents |
| **CBZ** | 90 | .cbz | Comic archives (ZIP) |
| **CBR** | 85 | .cbr | Comic archives (RAR) |
| **WebP** | 80 | .webp | Web images |
| **PNG** | 75 | .png | Lossless images |
| **JPEG** | 70 | .jpg | Photographs |
| **GIF** | 65 | .gif | Animated images |
| **AVIF** | 60 | .avif | Modern images |

## Directory Structure

### Standard Book Structure

```
Library/
├── BookTitle/
│   └── BookTitle.ext
├── AnotherBook/
│   └── AnotherBook.epub
└── Specials/
    └── Anthology/
        └── Collection.epub
```

### Manga Structure

```
Library/
├── MangaSeries/
│   ├── MangaSeries-v01-c001.cbz
│   ├── MangaSeries-v01-c002.cbz
│   └── [Specials]/
│       └── MangaSeries-SP01.cbz
```

## API Endpoints

### Import Configuration

```
GET    /api/v1/import/settings
POST   /api/v1/import/settings
POST   /api/v1/import/monitor
```

### File Management

```
GET    /api/v1/import/files
POST   /api/v1/import/scan
POST   /api/v1/import/files/process
POST   /api/v1/import/import
```

### Management

```
POST   /api/v1/import/blacklist
GET    /api/v1/import/statistics
GET    /api/v1/import/history
```

## Service Components

### Core Services

| Service | File Path | Responsibility |
|---------|-----------|----------------|
| **KavitaImporterService** | `Kavita.Services/KavitaImporterService.cs` | Import orchestration |
| **ImportFolderWatcher** | `Kavita.Services/Scanner/ImportFolderWatcher.cs` | File monitoring |
| **FormatDetectorService** | `Kavita.Services/Import/FormatDetectorService.cs` | Format detection |
| **DirectoryStructureBuilder** | `Kavita.Services/Import/DirectoryStructureBuilder.cs` | Structure generation |

### Data Models

**ImportSettings**
- ImportFolderPath: Path to import directory
- TargetLibraryId: Destination library
- FormatPriority: Format ranking system
- BlacklistPatterns: Exclusion rules

**ImportedFile**
- FilePath: Complete file path
- FileFormat: Detected format type
- Status: Current state (PENDING, PROCESSING, COMPLETED)
- Metadata: Extracted information

## Implementation Phases

### Phase 1: Foundation (Weeks 1-2)
- Data models and entities
- Repository implementations
- Database migrations

### Phase 2: Processing (Weeks 3-4)
- File processing pipeline
- Format detection system
- Directory structure builder

### Phase 3: Interface (Weeks 5-6)
- REST API endpoints
- Settings configuration UI
- Monitoring dashboard

### Phase 4: Optimization (Weeks 7-8)
- Performance tuning
- Comprehensive testing
- Documentation

## Key Features

### Automated Import
- Real-time file monitoring
- Intelligent format detection
- Automatic directory organization
- Blacklist pattern enforcement

### Format Management
- Priority-based format ranking
- Conflict resolution
- Custom format preferences
- Multi-format support

### Directory Organization
- Book and manga structures
- Naming convention application
- Volume and chapter tracking
- Special content grouping

### Monitoring & Analytics
- Import status tracking
- Processing metrics
- History and reporting
- Performance optimization

## Configuration Options

### Folder Watching

```csharp
// Enable folder watching
settings.FolderWatching = new FolderWatchingConfig
{
    Enabled = true,
    WatchInterval = 5000, // milliseconds
    EventTypes = new[] {
        WatcherChangeTypes.Created,
        WatcherChangeTypes.Changed,
        WatcherChangeTypes.Deleted
    }
};
```

### Format Priority

```csharp
// Define format priorities
var formatPriority = new Dictionary<string, int>
{
    { "EPUB", 100 },
    { "PDF", 95 },
    { "CBZ", 90 },
    { "CBR", 85 }
};
```

### Blacklist Patterns

```csharp
// Configure blacklist
var blacklist = new BlacklistConfiguration
{
    FolderPatterns = new[] {
        ".DS_Store",
        "Thumbs.db",
        "@eaDir"
    },
    FilePatterns = new[] {
        "*.tmp",
        "*.log",
        "*.cache"
    }
};
```

## Performance Targets

| Metric | Target | Description |
|--------|--------|-------------|
| Processing Time | < 5s | Per file processing |
| Detection Latency | < 2s | Event to processing |
| Success Rate | > 95% | Import completion |
| Configuration Time | < 10min | User setup |

## Troubleshooting

### Common Issues

**Issue**: Files not detected
- **Solution**: Verify folder watching is enabled
- **Check**: Monitor file system events
- **Action**: Review blacklist patterns

**Issue**: Format conflicts
- **Solution**: Apply format priority ranking
- **Check**: Examine file extensions
- **Action**: Update format preferences

**Issue**: Performance degradation
- **Solution**: Optimize processing pipeline
- **Check**: Monitor system resources
- **Action**: Implement caching strategies

## Resources

### Documentation
- [Comprehensive Implementation Plan](Kavita-Importer-Implementation-Plan.md)
- [Implementation Summary](Kavita-Importer-Implementation-Summary.md)
- [Design Specification](design-spec-kavita-importer.md)
- [Scanner Directory Structure](Scanner-dir-structure.md)

### Code References
- [LibraryWatcher](Kavita.Services/Scanner/LibraryWatcher.cs)
- [DirectoryService](Kavita.Services/DirectoryService.cs)
- [SettingsService](Kavita.Services/SettingsService.cs)

---

*Quick Reference Guide - Kavita Importer*
*For detailed information, refer to the comprehensive implementation plan*
