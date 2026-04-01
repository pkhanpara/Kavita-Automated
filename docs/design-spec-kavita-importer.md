# Design Specification - Kavita Importer

## Problem Statement

Kavita Reader requires files to be organized in a specific folder structure for optimal library management. Users often struggle with:
- Importing diverse eBook formats (EPUB, PDF, CBZ, CBR, images) from various sources
- Organizing files into Kavita's expected directory structure
- Handling multiple files with identical names but different formats
- Manually categorizing and renaming files for seamless import

## Solution Overview

The Kavita Importer provides a setting option to directory nameso that we can watch this directory to import the books from. 
And another setting to specify library name where the book should be imported to. Once it finds file that are not blacklisted determine weather it is a EPUB, PDF, CBZ, CBR then create that book file directory struture mentioned below in the library dir.

### Core Objectives

1. **Multi-Format Support**: Handle a comprehensive range of eBook and image formats including EPUB, PDF, CBZ, CBR, and various image types (PNG, JPEG, WebP, GIF, AVIF)

2. **Intelligent File Selection**: When multiple files share the same name, the tool should:
   - Automatically detect format variants
   - Apply a ranked format preference system
   - Allow users to customize format priorities

3. **Flexible Export Options**: Support both direct folder approach and optional direct library support

### Functional Requirements

#### File Input
- Drag-and-drop file selection interface
- Traditional file browser dialog
- Support for batch file imports
- Automatic file format detection

#### Processing Logic
- Extract metadata from imported files
- Rename files according to Kavita conventions
- Organize files into hierarchical folder structures
- Resolve naming conflicts intelligently

#### Output Structure
Generate folder structures following Kavita's scanner requirements:
```
output-folder/
├── BookTitle/
│   └── BookTitle.ext
├── MangaSeries/
│   ├── MangaSeries-v01-c001.ext
│   └── MangaSeries-v01-c002.ext
└── ...
```


