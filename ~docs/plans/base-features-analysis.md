# Base Features Analysis - ROM Scanner Core Functionality

**Date:** January 2026
**Purpose:** Identify gaps in core ROM scanner functionality to ensure base implementation is complete before advanced features

## ROM Scanner Core Workflow

A complete ROM scanner needs these fundamental capabilities:

```
1. DAT Import ─► 2. Drive Registration ─► 3. File Scanning ─► 4. Hashing ─► 5. Verification ─► 6. Organization ─► 7. Reporting
```

## Feature Analysis Matrix

| Feature | Interface | Implementation | Tests | Endpoints | Status |
|---------|-----------|----------------|-------|-----------|--------|
| **DAT Parsing - Logiqx XML** | ✅ IDatParser | ✅ LogiqxDatParser | ⚠️ Partial | ✅ /datfiles | ✅ Complete |
| **DAT Parsing - ClrMamePro** | ✅ IDatParser | ❌ Missing | ❌ None | - | ❌ **MISSING** |
| **DAT Parsing - Streaming** | ✅ IDatParser | ✅ StreamingLogiqxParser | ❌ None | - | ⚠️ Needs tests |
| **DAT Import** | ✅ IDatFileRepository | ✅ DatFileService | ✅ Yes | ✅ POST /datfiles | ✅ Complete |
| **DAT Category Tree** | - | ✅ DatFileService | ❌ None | ✅ GET /datfiles/categories | ⚠️ Needs tests |
| **Drive Registration** | ✅ IDriveRepository | ✅ DriveService | ✅ DriveTests | ✅ POST /drives | ✅ Complete |
| **Drive Online/Offline** | ✅ Drive entity | ✅ DriveService | ✅ Yes | ✅ /drives/{id}/status | ✅ Complete |
| **File Scanning** | ✅ IScanJobRepository | ✅ ScanService | ✅ Yes | ✅ /scans | ✅ Complete |
| **Scan Resume** | ✅ IScanResumeService | ✅ ScanResumeService | ✅ Yes | ⚠️ Implicit | ✅ Complete |
| **Archive Support** | ✅ IArchiveService | ✅ SharpCompressArchiveService | ⚠️ Partial | - | ✅ Complete |
| **Hash Computation** | ✅ IHashService | ✅ HashService | ✅ Yes | - | ✅ Complete |
| **Header Detection** | ✅ IRomHeaderService | ✅ RomHeaderService | ✅ Yes | - | ✅ Complete |
| **Hash Caching** | ✅ RomFile entity | ✅ HashService | ✅ Yes | - | ✅ Complete |
| **Verification** | ✅ (inline) | ✅ VerificationService | ❌ None | ⚠️ Partial | ⚠️ Needs endpoints |
| **Duplicate Detection** | ✅ IDuplicateDetectionService | ✅ DuplicateDetectionService | ✅ Yes | ❌ None | ⚠️ Needs endpoints |
| **Bad Dump Detection** | ✅ IBadDumpService | ✅ BadDumpService | ✅ Yes | ❌ None | ⚠️ Needs endpoints |
| **1G1R Filtering** | ✅ IOneGameOneRomService | ✅ OneGameOneRomService | ✅ Yes | ❌ None | ⚠️ Needs endpoints |
| **Parent/Clone** | ✅ IParentCloneService | ✅ ParentCloneService | ✅ Yes | ❌ None | ⚠️ Needs endpoints |
| **Organization** | ✅ IOrganizationService | ✅ OrganizationService | ⚠️ Partial | ✅ /organization | ✅ Complete |
| **Organization Logging** | ✅ IOrganizationLogRepository | ✅ Repository | ❌ None | ❌ None | ⚠️ Needs endpoints |
| **Storage Monitor** | ✅ IStorageMonitorService | ✅ StorageMonitorService | ✅ Yes | ✅ /storage | ✅ Complete |

## Critical Gaps Identified

### 1. ❌ ClrMamePro DAT Parser (MISSING)
- **Impact:** Cannot import ~20% of DAT files (TOSEC, older collections)
- **Epic Reference:** #425 marked as ✅ Done but not implemented
- **Priority:** HIGH

### 2. ⚠️ Missing API Endpoints for Core Features
These services exist but have no REST API endpoints:

| Service | Missing Endpoint | Priority |
|---------|------------------|----------|
| VerificationService | POST /verify, GET /verification/status | HIGH |
| DuplicateDetectionService | GET /romfiles/duplicates | MEDIUM |
| BadDumpService | GET /romfiles/baddumps | MEDIUM |
| OneGameOneRomService | POST /datfiles/{id}/1g1r | MEDIUM |
| ParentCloneService | GET /datfiles/{id}/parent-clone | LOW |

### 3. ⚠️ Missing Unit Tests
Critical services without test coverage:

| Service | Test File | Needed Tests |
|---------|-----------|--------------|
| VerificationService | ❌ None | Hash matching, batch verification |
| ScanService | ❌ None | Scan lifecycle, cancellation |
| DatFileService | ✅ Basic | Import with archives, categories |
| StreamingLogiqxParser | ❌ None | Large file parsing |
| OrganizationService | ❌ None | Template parsing, file operations |

### 4. ⚠️ Scan Execution Not Wired
- ScanEndpoints creates scan jobs but doesn't execute them
- `TODO: Queue the job for background execution via ExecuteScanAsync` comment in code
- Need background job processor integration

## Database Schema Analysis

Current entities:
- ✅ DatFile, GameEntry, RomEntry (DAT data)
- ✅ Drive, RomFile (Storage tracking)
- ✅ ScanJob (Scanning)
- ✅ OrganizationOperationLog, OrganizationOperationEntry (Logging)

Missing:
- ⚠️ Settings/Configuration entity (mentioned in Epic #9.1 #406)
- ⚠️ No indexes on GameEntry/RomEntry for hash lookups (performance concern)

## SignalR Integration Status

Current hubs and events:
- ✅ ScanProgress events
- ✅ HashProgress events
- ✅ DatImportProgress events
- ⚠️ Verification progress events (partial)
- ❌ Organization progress events

## Recommended Priority Order

### Phase 1: Critical Missing Features (This Session)
1. **ClrMamePro DAT Parser** - Essential for many DAT sources
2. **Verification Endpoints** - Core scanner functionality
3. **Scan Execution Wiring** - Jobs created but not executed

### Phase 2: API Completeness (Next Session)
1. Duplicate detection endpoint
2. Bad dump detection endpoint
3. 1G1R filtering endpoint
4. Organization log viewing endpoints

### Phase 3: Testing & Polish
1. Add missing unit tests
2. Integration tests for full workflow
3. Performance testing with large datasets

## Files to Create/Modify

### New Files Needed:
```
src/Subrom.Infrastructure/Parsing/ClrMameProDatParser.cs
src/Subrom.Server/Endpoints/VerificationEndpoints.cs
src/Subrom.Server/BackgroundJobs/ScanJobProcessor.cs (or use IHostedService)
tests/Subrom.Tests.Unit/Application/Services/VerificationServiceTests.cs
tests/Subrom.Tests.Unit/Infrastructure/Parsing/ClrMameProParserTests.cs
```

### Files to Modify:
```
src/Subrom.Infrastructure/DependencyInjection.cs - Register ClrMamePro parser
src/Subrom.Server/Endpoints/EndpointExtensions.cs - Add verification routes
src/Subrom.Server/Endpoints/RomFileEndpoints.cs - Add duplicate/baddump filters
```

## Summary

**Core Implementation Status: ~75% Complete**

The ROM scanner has solid foundations but needs:
1. ClrMamePro parser for format support
2. Verification API endpoints to expose existing functionality
3. Background scan execution to actually run scans
4. Missing tests for critical paths

Focus should be completing these base features before:
- Advanced Epic #8 features (large dataset handling)
- Epic #7 features (RetroArch integration, metadata scraping)
- UI polish (Epic #6.5)
