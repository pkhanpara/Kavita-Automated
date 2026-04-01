# Kavita Scanner Service: Directory Structure Guide

This document provides a comprehensive overview of how the **Scanner service** in Kavita handles directory structures for different types of books and reading materials.

## Overview

The Kavita Scanner service is responsible for monitoring library directories, discovering new content, categorizing materials by type, and maintaining metadata. It uses a modular architecture with specialized parsers to handle various content formats including Manga, Comics, Books, Light Novels, Images, and Webtoons.

## Scanner Service Architecture

### Core Components

```
ScannerService
├── Parser Service (Parser.cs)
│   ├── Regex-based filename parsing
│   ├── Content type classification
│   └── Special chapter/volume detection
├── Library Watcher (LibraryWatcher.cs)
│   ├── File system event monitoring
│   ├── Real-time change detection
│   └── Automatic scan scheduling
├── Series Processing (ProcessSeries.cs)
│   ├── Metadata updates
│   ├── Volume/chapter organization
│   └── External metadata integration
└── ReadingItemService (ReadingItemService.cs)
    ├── File parsing and format detection
    ├── Metadata extraction
    └── Cover image generation
```

### Main Service File

**Location**: `Kavita.Services/Scanner/ScannerService.cs`

**Key Responsibilities**:
- Scans library directories for new and updated files
- Categorizes content by type (Manga, Comic, Book, Light Novel, Image)
- Manages series lifecycle (creation, updates, deletion)
- Coordinates with metadata services for cover generation
- Handles background scanning via Hangfire jobs

**Core Methods**:
```csharp
// Core scanning methods
public async Task ScanSeries(int seriesId, bool bypassFolderOptimizationChecks = true)
public async Task ScanLibrary(int libraryId, bool forceUpdate = false, bool isSingleScan = true)
public async Task ScanFolder(string folder, string originalPath, bool abortOnNoSeriesMatch = false)
```

### Supporting Components

| Component | File Path | Purpose |
|-----------|-----------|---------|
| **Parser Service** | `Kavita.Services/Scanner/Parser.cs` | Comprehensive regex patterns for filename parsing |
| **Library Watcher** | `Kavita.Services/Scanner/LibraryWatcher.cs` | File system event monitoring |
| **Book Parser** | `Kavita.Services/Scanner/BookParser.cs` | Handles Epub and PDF files |
| **Basic Parser** | `Kavita.Services/Scanner/BasicParser.cs` | Core parser for Manga, Comic, and Book libraries |
| **Image Parser** | `Kavita.Services/Scanner/ImageParser.cs` | Manages image-only libraries |
| **ComicVine Parser** | `Kavita.Services/Scanner/ComicVineParser.cs` | ComicVine-specific parsing |
| **PDF Parser** | `Kavita.Services/Scanner/PdfParser.cs` | PDF-specific processing |

---

## Supported Content Types

Kavita supports six main library types, each with specific parsing strategies:

### LibraryType Enumeration

**Location**: `Kavita.Models/Entities/Enums/LibraryType.cs`

| Type | Value | Description | Parsing Strategy |
|------|-------|-------------|------------------|
| **Manga** | 0 | Manga with regex-based parsing | Uses Manga-specific regex patterns |
| **Comic** | 1 | Flexible Comic library | Original flexible parsing approach |
| **Book** | 2 | Epub/PDF books with metadata | Leverages Epub metadata |
| **Image** | 3 | Image-based content | Special grouping mechanism |
| **LightNovel** | 4 | Books with scrobbling support | Enhanced with AniList integration |
| **ComicVine** | 5 | ComicVine-style parsing | ComicVine-specific patterns |

### Content Type Characteristics

#### Manga Libraries
- **Primary Format**: CBZ/CBR archives
- **Naming Convention**: Series Name - Vol. XX Ch. XXX.cbz
- **Special Features**: Volume and chapter tracking, scan group identification
- **Regex Patterns**: `MangaSeriesRegex`, `MangaVolumeRegex`, `MangaChapterRegex`

#### Comic Libraries
- **Primary Format**: CBZ/CBR archives
- **Naming Convention**: Series Name #XXX (Year).cbz
- **Special Features**: Issue tracking, volume management, TPB/Omnibus support
- **Regex Patterns**: `ComicSeriesRegex`, `ComicVolumeRegex`, `ComicChapterRegex`

#### Book Libraries
- **Primary Formats**: EPUB, PDF
- **Naming Convention**: Series Name - Volume Title.epub
- **Special Features**: Metadata extraction, scrobbling support, reading progress
- **Parser**: BookParser with metadata extraction

#### Image Libraries
- **Primary Formats**: PNG, JPG, JPEG, WEBP, GIF, AVIF
- **Naming Convention**: Series Name - Chapter XXX.jpg
- **Special Features**: Loose-leaf volumes, webtoon support, folder-based organization
- **Parser**: ImageParser with special grouping

#### Light Novel Libraries
- **Primary Formats**: EPUB, PDF
- **Naming Convention**: Series Name - Vol. XX.epub
- **Special Features**: AniList integration, reading status tracking, series relationships
- **Enhancement**: Scrobbling with progress tracking

#### ComicVine Libraries
- **Primary Format**: CBZ with external metadata
- **Naming Convention**: Series Name (Year) - Issue #XXX.cbz
- **Special Features**: ComicVine API integration, comprehensive metadata
- **Parser**: ComicVineParser with external service integration

---

## Directory Structure Patterns

### Root Directory Layout

```
Root Library
├── Series Name (Year)/
│   ├── [Specials]/
│   │   └── Series-SP01 - Title.cbz
│   ├── Volume 01/
│   │   └── Series-v01-c01.cbz
│   ├── Volume 02/
│   │   └── Series-v02-c50.cbz
│   └── Volume 03/
│       └── Series-v03-c100.cbz
├── Series Name 2/
│   └── ...
└── Specials/
    ├── Annual/
    │   └── Series-Annual.cbz
    └── TPB/
        └── Series-TPB-01.cbz
```


## Content Type Classification

### Classification Rules

**Location**: `Kavita.Services/Scanner/Parser.cs`

#### Manga Pattern Examples

```
Standard Format:
- Series Name - Vol. 01 Ch. 001.cbz
- Series Name (2020) - v10 c171-180.cbz
- [ScanGroup] Series Name - Chapter 025 - Volume 1.cbz

Advanced Format:
- [Group] Naruto - Vol. 01 Ch. 001-012.cbz
- Bleach - v25-c201-220 (English).cbz
- One-Piece - Specials - SP01 - Omake.cbz
```

#### Comic Pattern Examples

```
Standard Format:
- Series Name #001 (2020).cbz
- Series Name - Vol. 01 Issue #005.cbr
- 01 - Series Name Title (2020).cbz

Advanced Format:
- Batman - Vol. 03 TPB - Issues #50-60.cbr
- The-Amazing-Spider-Man-2020-v01-001-025.cbz
- Superman-Annual-2024-Complete.cbz
```

### Special Markers

| Marker | Description | Usage |
|--------|-------------|-------|
| **SP** | Special chapters/episodes | Series-SP01 - Title.cbz |
| **Annual** | Annual releases | Series-Annual-2024.cbz |
| **Omake** | Bonus content | Series-SP02 - Omake.cbz |
| **Extra** | Extra chapters | Series-Extra-01.cbz |
| **TPB** | Trade Paper Back | Series-TPB-01.cbz |
| **Omnibus** | Large collections | Series-Omnibus-Complete.cbz |
| **ScanGroup** | Scan group identifier | [Group] Series.cbz |

### Content Classification Logic

The Scanner uses a multi-tier classification approach:

1. **File Extension Detection**: Identifies file types (CBZ, CBR, EPUB, PDF, Images)
2. **Filename Pattern Matching**: Applies regex patterns to extract metadata
3. **Metadata Extraction**: Reads embedded metadata (ComicInfo.xml, EPUB metadata)
4. **Fallback Analysis**: Uses folder structure for files without metadata
5. **Content Type Assignment**: Classifies based on content characteristics

---

## Folder Hierarchy Examples

### Example 1: Manga Library Structure

```
/Library/
├── Naruto/
│   ├── Naruto-v001-c001.cbz
│   ├── Naruto-v001-c050.cbz
│   ├── Naruto-v010-c171-180.cbz
│   └── [Specials]/
│       ├── Naruto-SP01 - Omake.cbz
│       └── Naruto-SP02 - Extra.cbz
├── One Piece/
│   ├── One-Piece-v1000-c1000.cbz
│   └── Annual/
│       └── One-Piece-Annual-2024.cbz
└── Specials/
    ├── Anthology.cbz
    └── TPB.cbz
```

**Key Features**:
- Volume-based organization with nested special folders
- Special chapters grouped in [Specials] subdirectory
- Annual releases in dedicated Annual folder
- Trade Paper Back collections in TPB folder

### Example 2: Book Library Structure

```
/Library/
├── Light-Novels/
│   ├── Overlord/
│   │   ├── Overlord-v01.epub
│   │   └── Overlord-v02.epub
│   └── Re:Zero/
│       └── Re:Zero-v01.epub
├── Graphic-Novels/
│   └── Batman/
│       ├── Batman-TPB-01.pdf
│       └── Batman-TPB-02.pdf
└── Non-Fiction/
    └── Technology/
        └── Modern-Programming.epub
```

**Key Features**:
- Light novels organized by series with volume-based EPUB files
- Graphic novels grouped in TPB collections
- Non-fiction content organized by subject area
- Metadata-rich EPUB and PDF formats

### Example 3: Webtoon Library Structure

```
/Library/
├── Solo-Leveling/
│   ├── Solo-Leveling-S01-001.jpg
│   ├── Solo-Leveling-S01-002.jpg
│   └── Solo-Leveling-S02-050.jpg
├── Tower-of-God/
│   ├── Tower-of-God-T01-001.webp
│   └── Tower-of-God-T01-050.webp
└── Specials/
    └── Omake/
        └── Character-Guides.jpg
```

**Key Features**:
- Sequential image files for chapter-by-chapter reading
- WebP format for optimized loading
- Special content in Omake subdirectory
- Chapter numbering in filenames for easy navigation

### Example 4: Comic Library Structure

```
/Library/
├── Marvel/
│   ├── Spider-Man/
│   │   ├── Spider-Man-2020-v01-001-025.cbz
│   │   └── Spider-Man-2021-v02-026-050.cbz
│   └── X-Men/
│       └── X-Men-Annual-2024.cbz
├── DC-Comics/
│   ├── Batman/
│   │   ├── Batman-TPB-01.cbr
│   │   └── Batman-TPB-02.cbr
│   └── Superman/
│       └── Superman-Omnibus-Complete.cbz
└── Specials/
    ├── Cross-Over/
    │   └── Marvel-vs-DC-Complete.cbr
    └── Anthology/
        └──Comic-Compilations-2024.cbz
```

**Key Features**:
- Publisher-based organization (Marvel, DC Comics)
- Character-specific subdirectories
- Trade Paper Back collections
- Annual and omnibus special releases
- Cross-over events in dedicated folders

---

## Metadata File Handling

### Supported Metadata Formats

**Location**: `Kavita.Services/Scanner/Parser.cs`

#### File Extensions

| Category | Extensions | Primary Use |
|----------|------------|-------------|
| **Archive** | .cbz, .cbr, .zip, .rar, .7z | Manga and Comics |
| **Books** | .epub, .pdf | Books and Light Novels |
| **Images** | .png, .jpg, .jpeg, .webp, .gif, .avif | Image Libraries |
| **Metadata** | ComicInfo.xml, .kavita-metadata.json | Embedded metadata |

---


## Best Practices

### Directory Organization

1. **Use Consistent Naming Conventions**
   - Follow standardized filename patterns for each content type
   - Include volume and chapter numbers in filenames
   - Use scan group identifiers for grouped releases

2. **Organize by Content Type**
   - Create separate library folders for different content types
   - Use subdirectories for series, volumes, and special content
   - Group related content in logical hierarchies

3. **Leverage Special Folders**
   - Use [Specials] folders for bonus content
   - Create Annual folders for yearly releases
   - Organize TPB and Omnibus collections separately


## Key File Locations Summary

| Component | File Path | Purpose |
|-----------|-----------|---------|
| **Main Scanner Service** | `Kavita.Services/Scanner/ScannerService.cs` | Core scanning logic |
| **Parser Service** | `Kavita.Services/Scanner/Parser.cs` | Content classification |
| **Library Watcher** | `Kavita.Services/Scanner/LibraryWatcher.cs` | File system monitoring |
| **Book Parser** | `Kavita.Services/Scanner/BookParser.cs` | Book content handling |
| **Basic Parser** | `Kavita.Services/Scanner/BasicParser.cs` | Core parsing |
| **Image Parser** | `Kavita.Services/Scanner/ImageParser.cs` | Image processing |
| **ComicVine Parser** | `Kavita.Services/Scanner/ComicVineParser.cs` | ComicVine integration |
| **Directory Service** | `Kavita.Services/DirectoryService.cs` | Directory management |
| **Library Entity** | `Kavita.Models/Entities/Library.cs` | Library configuration |
| **FolderPath Entity** | `Kavita.Models/Entities/FolderPath.cs` | Folder tracking |
| **Library Type Enum** | `Kavita.Models/Entities/Enums/LibraryType.cs` | Content type definitions |
| **Metadata Settings** | `Kavita.Models/Entities/MetadataMatching/MetadataSettings.cs` | Metadata configuration |
| **App Configuration** | `Kavita.Server/config/appsettings.json` | Application settings |
| **Configuration Manager** | `Kavita.Common/Configuration.cs` | Configuration management |

---

