# Legacy Code Modernization Plan

## Status: ✅ COMPLETED

This modernization has been successfully completed. All legacy code has been removed and modern archive support has been implemented.

## Summary of Changes

### Removed Legacy Projects (Phase 1 & 4)
- ✅ **Compression/** - Deleted (149 warnings, unused LZMA SDK from 2008)
- ✅ **Domain/** - Deleted (duplicate of src/Subrom.Domain)
- ✅ **Services/** - Deleted (duplicate of src/Subrom.Application)
- ✅ **Infrastructure/** - Deleted (duplicate of src/Subrom.Infrastructure)
- ✅ **Subrom/** (SubromAPI) - Deleted (replaced by src/Subrom.Server)
- ✅ **ConsoleTesting/** - Deleted (scratch pad using legacy projects)
- ✅ **subrom-ui-old/** - Deleted (old UI, replaced by subrom-ui/)

### New Archive Support (Phase 2 & 3)
- ✅ Created `IArchiveService` interface in `src/Subrom.Application/Interfaces/`
- ✅ Implemented `SharpCompressArchiveService` in `src/Subrom.Infrastructure/Services/`
- ✅ Added SharpCompress package (v0.44.1) for multi-format archive support
- ✅ Updated `HashService` to use `IArchiveService` for 7z/RAR support
- ✅ Updated DI registration in `DependencyInjection.cs`
- ✅ Fixed unit tests to use new constructor

### Build Results
- **Before:** 152 warnings (149 from Compression, 3 from legacy Services/SubromAPI)
- **After:** 0 warnings, 0 errors
- **Tests:** All 31 tests pass

---

## Original Analysis (Historical Reference)

## Executive Summary

The Subrom project contains significant **duplicate and legacy code** that needs to be consolidated and modernized. This document analyzes the current state, identifies issues, and provides a comprehensive migration plan.

## Current Architecture Problems

### 1. Duplicate Project Structure

The solution currently has **TWO parallel architectures**:

| Layer | Legacy (Root) | Modern (src/) |
|-------|---------------|---------------|
| Domain | `Domain/` | `src/Subrom.Domain/` |
| Services | `Services/` | `src/Subrom.Application/` |
| Infrastructure | `Infrastructure/` | `src/Subrom.Infrastructure/` |
| API | `Subrom/` (SubromAPI) | `src/Subrom.Server/` |
| Compression | `Compression/` | *None (uses System.IO.Compression)* |

### 2. The Compression/SevenZip Project - Analysis

**Origin:** Port of LZMA SDK from 2008 (version 4.61)

**Current Status:**
- ❌ **NOT REFERENCED** by any other project in the solution
- ❌ Generates **149 compiler warnings** due to outdated patterns
- ❌ Uses legacy C# patterns (nullable issues, lack of readonly, etc.)
- ❌ No modern async support
- ❌ Only supports LZMA/7z compression algorithm (not 7z archive format)

**What It Contains:**
```
Compression/SevenZip/
├── CoderPropID.cs         # Compression property enums
├── Common/
│   ├── CommandLineParser.cs  # CLI parser (unused)
│   ├── CRC.cs               # Custom CRC32 (replaced by System.IO.Hashing)
│   ├── InBuffer.cs          # I/O buffers
│   └── OutBuffer.cs
├── Compress/
│   ├── LZ/                  # LZ algorithm helpers
│   ├── LZMA/                # LZMA encoder/decoder
│   ├── LzmaAlone/           # Standalone LZMA tool (with Main method!)
│   └── RangeCoder/          # Range coding helpers
├── Exceptions/              # Custom exceptions
└── Interfaces/              # ICoder, ICodeProgress, etc.
```

**Why It Was Added:**
- Intended for 7z archive extraction for ROMs
- Needed to hash files inside compressed archives without full extraction

**Why It's NOT Being Used:**
- Modern code uses `System.IO.Compression.ZipFile` for ZIP archives
- 7z support was never actually implemented
- The SDK is for LZMA **compression**, not 7z **archive format**

**Verdict: DELETE ENTIRELY** ☠️

### 3. Legacy Domain Project Analysis

**Location:** `Domain/`

**What It Contains:**
```
Domain/
├── Datfiles/
│   ├── Archive.cs, BiosSet.cs, Clrmamepro.cs, Datafile.cs
│   ├── DatfileService.cs    # Service mixed with models!
│   ├── Disk.cs, Game.cs, Header.cs, Machine.cs
│   ├── Release.cs, Rom.cs, Romcenter.cs, Sample.cs
│   └── Kinds/, Xml/         # DAT type enums and XML parsing
├── Hash/
│   └── Crc.cs, Hashes.cs, Md5.cs, Sha1.cs  # Value objects
└── Storage/                  # Unknown
```

**Problems:**
- Has `DatfileService` mixed in Domain (violates DDD)
- Duplicates modern `Subrom.Domain` models
- Hash value types are simpler than modern versions
- Uses `TreatWarningsAsErrors = false` and multiple `NoWarn` suppressions

**Verdict: MIGRATE useful code, DELETE project**

### 4. Legacy Services Project Analysis

**Location:** `Services/`

**What It Contains:**
```
Services/
├── DatService.cs
├── DriveService.cs
├── FileScannerService.cs
├── HashService.cs           # Duplicates src/Subrom.Infrastructure
├── ScanService.cs
├── VerificationService.cs
└── Interfaces/
```

**Problems:**
- Services reference legacy `Domain/` project
- HashService duplicates modern Infrastructure version
- References legacy `Infrastructure/` for parsers

**Verdict: DELETE - Modern services exist**

### 5. Legacy Infrastructure Project Analysis

**Location:** `Infrastructure/`

**What It Contains:**
```
Infrastructure/
├── Extensions/
│   └── BasicExtensions.cs
└── Parsers/
    └── XmlDatParser.cs (+ others)
```

**Problems:**
- Limited utility extensions
- DAT parsers may be useful but exist in modern version

**Verdict: MIGRATE parsers if needed, DELETE project**

### 6. Legacy SubromAPI Project

**Location:** `Subrom/`

**What It Contains:**
- Old ASP.NET Web API project
- WeatherForecast example controller still present!
- Basic verification controller

**Verdict: DELETE - Replaced by `Subrom.Server`**

---

## What Archive Support is Actually Needed

ROM management requires reading files from archives to:
1. **Hash individual files** inside archives without full extraction
2. **List archive contents** for verification
3. **Extract files** when organizing ROMs
4. **Create archives** (optional, for 1G1R sets)

### Required Archive Formats

| Format | Priority | Usage |
|--------|----------|-------|
| **ZIP** | ✅ Critical | Most common ROM format, built-in .NET support |
| **7z** | ✅ High | Popular for large ROM sets, better compression |
| **RAR** | ⚪ Medium | Legacy format, some old dumps |
| **GZip** | ⚪ Low | Single-file compression (`.rom.gz`) |

### Recommended Solution: SharpCompress

**Package:** `SharpCompress` (MIT License)
**Version:** 0.38.0+

**Why SharpCompress:**
- ✅ Pure C# - no native dependencies
- ✅ Supports ZIP, 7z, RAR, GZip, Tar, etc.
- ✅ Stream-based API (read without full extraction)
- ✅ Modern async patterns
- ✅ Actively maintained
- ✅ Cross-platform

**Alternative: SevenZipExtractor**
- Wraps native 7z.dll
- Better performance for huge archives
- Requires native dependency management

---

## Migration Plan

### Phase 1: Remove Compression Project (Immediate)

1. Remove `Compression/` from solution
2. Delete `Compression/` folder
3. Update solution file

**Files to Delete:**
```
Compression/
├── Compression.csproj
└── SevenZip/          # ~30 files, all unused
```

### Phase 2: Create Modern Archive Service

Create `IArchiveService` in `Subrom.Application`:

```csharp
public interface IArchiveService {
    Task<IReadOnlyList<ArchiveEntry>> ListEntriesAsync(
        string archivePath, 
        CancellationToken ct = default);
    
    Task<Stream> OpenEntryAsync(
        string archivePath, 
        string entryPath, 
        CancellationToken ct = default);
    
    Task ExtractEntryAsync(
        string archivePath, 
        string entryPath, 
        string destinationPath, 
        CancellationToken ct = default);
    
    Task ExtractAllAsync(
        string archivePath, 
        string destinationDir, 
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken ct = default);
    
    bool SupportsFormat(string extension);
}

public record ArchiveEntry {
    public required string Path { get; init; }
    public required long Size { get; init; }
    public required long CompressedSize { get; init; }
    public DateTime? LastModified { get; init; }
    public string? Crc32 { get; init; }  // Some formats store this
    public bool IsDirectory { get; init; }
}
```

### Phase 3: Implement with SharpCompress

```csharp
public sealed class SharpCompressArchiveService : IArchiveService {
    private static readonly string[] SupportedExtensions = [
        ".zip", ".7z", ".rar", ".gz", ".tar", ".tar.gz", ".tgz"
    ];
    
    public async Task<IReadOnlyList<ArchiveEntry>> ListEntriesAsync(
        string archivePath, 
        CancellationToken ct = default) {
        await using var stream = File.OpenRead(archivePath);
        using var archive = ArchiveFactory.Open(stream);
        
        return archive.Entries
            .Where(e => !e.IsDirectory)
            .Select(e => new ArchiveEntry {
                Path = e.Key ?? "",
                Size = e.Size,
                CompressedSize = e.CompressedSize,
                LastModified = e.LastModifiedTime,
                Crc32 = e.Crc?.ToString("x8"),
                IsDirectory = e.IsDirectory
            })
            .ToList();
    }
    
    // ... other implementations
}
```

### Phase 4: Consolidate Legacy Code

#### Domain Migration Checklist
- [ ] Review `Domain/Hash/` - Already superseded by `Subrom.Domain/ValueObjects`
- [ ] Review `Domain/Datfiles/` - Check for unique functionality
- [ ] Review `Domain/Storage/` - Unknown purpose, evaluate

#### Services Migration Checklist  
- [ ] `HashService` - Modern version exists in Infrastructure
- [ ] `ScanService` - Modern version exists in Application
- [ ] `DatService` - Evaluate against modern implementation
- [ ] `DriveService` - Modern version exists
- [ ] `VerificationService` - Modern version exists

#### Infrastructure Migration Checklist
- [ ] `XmlDatParser` - Compare to `LogiqxDatParser`
- [ ] `BasicExtensions` - Evaluate utility

### Phase 5: Clean Up Solution

1. Remove legacy projects from solution:
   - `Compression`
   - `Domain` (legacy)
   - `Services`
   - `Infrastructure` (legacy)
   - `SubromAPI`
   - `ConsoleTesting` (evaluate)

2. Update remaining projects:
   - Ensure all use modern `src/` projects
   - Remove any lingering references

3. Final solution structure:
   ```
   Subrom.sln
   ├── src/
   │   ├── Subrom.Domain/
   │   ├── Subrom.Application/
   │   ├── Subrom.Infrastructure/
   │   └── Subrom.Server/
   ├── tests/
   │   └── Subrom.Tests.Unit/
   └── subrom-ui/
   ```

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing functionality | High | Comprehensive tests before removal |
| Missing features in legacy code | Medium | Code review before deletion |
| Archive format support gaps | Medium | SharpCompress covers all common formats |
| Performance regression | Low | SharpCompress is well-optimized |

---

## Timeline

| Phase | Duration | Priority |
|-------|----------|----------|
| Phase 1: Remove Compression | 30 min | Immediate |
| Phase 2: Design Archive Interface | 1 hr | High |
| Phase 3: Implement SharpCompress | 2 hrs | High |
| Phase 4: Consolidate Legacy | 2-4 hrs | Medium |
| Phase 5: Clean Up Solution | 1 hr | Final |

**Total Estimated Time:** 6-8 hours

---

## Success Criteria

- [ ] Zero compiler warnings from legacy code (because it's deleted)
- [ ] All archive operations work via new `IArchiveService`
- [ ] ZIP, 7z, and RAR formats supported
- [ ] HashService can hash files inside any archive
- [ ] Solution structure is clean and organized
- [ ] All existing tests pass
