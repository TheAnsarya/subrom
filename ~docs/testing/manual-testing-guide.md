# Subrom 1.0.0 Manual Testing Guide

**Version:** 1.0.0
**Date:** January 22, 2026
**Tester:** _______________

## Overview

This document provides comprehensive manual testing procedures for Subrom 1.0.0. All tests should be completed to verify the release quality.

## Test Environment Setup

### Prerequisites
- Windows 10/11 x64
- .NET 10 SDK installed
- Node.js 20+ with Yarn
- 1GB+ free disk space
- Sample ROM files for testing
- Sample DAT files (No-Intro, TOSEC)

### Build Steps
```powershell
# Clone and build
git clone https://github.com/TheAnsarya/subrom.git
cd subrom
dotnet build Subrom.sln

# Build frontend
cd subrom-ui
yarn install
yarn build
```

### Start Server
```powershell
dotnet run --project src/Subrom.Server
```

Server should start at: `http://localhost:52100`

---

## Test Categories

- ✅ = Pass
- ❌ = Fail
- ⚠️ = Pass with issues
- ⬜ = Not tested

---

## 1. Server & API Tests

### 1.1 Server Startup

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Server starts | Run `dotnet run --project src/Subrom.Server` | Server starts, logs "Subrom server started on port 52100" | ⬜ |
| API responds | GET `http://localhost:52100/api/version` | Returns JSON with version info | ⬜ |
| Health check | GET `http://localhost:52100/health` | Returns 200 OK | ⬜ |
| OpenAPI spec | GET `http://localhost:52100/openapi/v1.json` | Returns OpenAPI JSON | ⬜ |
| Scalar docs | Browse `http://localhost:52100/scalar/v1` | Interactive API docs load | ⬜ |

### 1.2 Error Handling

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| 404 response | GET `/api/nonexistent` | Returns 404 with ProblemDetails | ⬜ |
| Invalid input | POST `/api/drives` with invalid JSON | Returns 400 with error details | ⬜ |
| Error endpoint | Trigger error, check `/error` | ProblemDetails format returned | ⬜ |

---

## 2. DAT File Management Tests

### 2.1 DAT Import

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Import Logiqx XML | POST `/api/datfiles/import` with No-Intro DAT | DAT file created, returns ID | ⬜ |
| Import ClrMamePro | POST `/api/datfiles/import` with TOSEC DAT | DAT file created, returns ID | ⬜ |
| Large DAT (60K+ entries) | Import Nintendo - Game Boy Advance DAT | Completes within 30 seconds | ⬜ |
| Duplicate DAT | Import same DAT twice | Returns error or updates existing | ⬜ |
| Invalid DAT format | Import invalid/corrupted file | Returns 400 with clear error | ⬜ |

### 2.2 DAT Listing

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| List all DATs | GET `/api/datfiles` | Returns array of DAT files | ⬜ |
| Get DAT by ID | GET `/api/datfiles/{id}` | Returns single DAT details | ⬜ |
| DAT categories | GET `/api/datfiles/{id}/categories` | Returns category breakdown | ⬜ |
| DAT games | GET `/api/datfiles/{id}/games` | Returns paginated games | ⬜ |

### 2.3 DAT Delete

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Delete DAT | DELETE `/api/datfiles/{id}` | DAT removed, returns 204 | ⬜ |
| Delete nonexistent | DELETE `/api/datfiles/{bad-id}` | Returns 404 | ⬜ |

---

## 3. Drive Management Tests

### 3.1 Drive Registration

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Register local drive | POST `/api/drives` with valid path | Drive created with ID | ⬜ |
| Register network drive | POST with UNC path `\\server\share` | Drive created, may be offline | ⬜ |
| Invalid path | POST with nonexistent path | Returns 400 error | ⬜ |
| Duplicate path | Register same path twice | Returns error | ⬜ |

### 3.2 Drive Status

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| List drives | GET `/api/drives` | Returns all registered drives | ⬜ |
| Drive online | Check online local drive | `isOnline: true`, space stats | ⬜ |
| Drive offline | Disconnect drive, refresh | `isOnline: false` | ⬜ |
| Refresh status | POST `/api/drives/{id}/refresh` | Stats updated | ⬜ |

### 3.3 Drive Delete

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Delete drive | DELETE `/api/drives/{id}` | Drive removed | ⬜ |
| Delete with ROMs | Delete drive containing scanned ROMs | Cascades correctly | ⬜ |

---

## 4. Scanning Tests

### 4.1 Basic Scan

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Start scan | POST `/api/scans` with drive ID | Scan job created | ⬜ |
| Scan progress | Connect to SignalR hub | Receive progress updates | ⬜ |
| Scan completion | Wait for scan to finish | Status changes to Completed | ⬜ |
| Scan results | GET `/api/romfiles` | ROMs discovered and hashed | ⬜ |

### 4.2 Archive Support

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Scan ZIP files | Folder with ZIP archives | ROMs inside ZIPs discovered | ⬜ |
| Scan 7z files | Folder with 7z archives | ROMs inside 7z discovered | ⬜ |
| Scan RAR files | Folder with RAR archives | ROMs inside RAR discovered | ⬜ |
| Nested archives | Archive inside archive | Handles gracefully | ⬜ |

### 4.3 Hash Computation

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| CRC32 hash | Scan ROM file | CRC32 computed correctly | ⬜ |
| MD5 hash | Enable MD5 in settings | MD5 computed | ⬜ |
| SHA1 hash | Enable SHA1 in settings | SHA1 computed | ⬜ |
| Header detection | ROM with header (NES, SNES) | Header size detected | ⬜ |
| Headerless hash | ROM with header | Headerless hash matches DAT | ⬜ |

### 4.4 Scan Resume

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Cancel scan | Cancel running scan | Status: Cancelled | ⬜ |
| Resume scan | Resume cancelled scan | Continues from checkpoint | ⬜ |
| Server restart | Restart during scan | Can resume scan | ⬜ |

---

## 5. Verification Tests

### 5.1 ROM Verification

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Verify by CRC | Verify ROM against DAT | Match found by CRC | ⬜ |
| Verify by MD5 | Verify with MD5 hash | Match found by MD5 | ⬜ |
| Verify by SHA1 | Verify with SHA1 hash | Match found by SHA1 | ⬜ |
| No match | Verify ROM not in any DAT | Status: Unknown | ⬜ |
| Bad dump | Verify known bad dump | Status: BadDump | ⬜ |

### 5.2 Batch Verification

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Verify all ROMs | POST `/api/verify/batch` | All ROMs verified | ⬜ |
| Verification stats | GET verification summary | Counts: verified, unknown, bad | ⬜ |

---

## 6. Duplicate Detection Tests

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Detect duplicates | GET `/api/romfiles/duplicates` | Lists duplicate ROMs | ⬜ |
| Same hash | Two files, same content | Grouped as duplicates | ⬜ |
| Different location | Same ROM on different drives | Both shown as duplicates | ⬜ |

---

## 7. 1G1R Filtering Tests

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Default 1G1R | GET `/api/datfiles/{id}/1g1r` | Returns 1G1R filtered list | ⬜ |
| Region priority | Set USA > Europe > Japan | USA versions preferred | ⬜ |
| Language priority | Set En > Ja > De | English versions preferred | ⬜ |
| Parent preference | Prefer parent over clone | Parent ROMs selected | ⬜ |

---

## 8. Organization Tests

### 8.1 Templates

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| List templates | GET `/api/organization/templates` | Returns 5+ built-in templates | ⬜ |
| Template preview | Preview organization | Shows planned moves | ⬜ |
| Custom template | Create custom template | Template works correctly | ⬜ |

### 8.2 Organization Operations

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Dry run | Organization with dryRun=true | No files moved, shows plan | ⬜ |
| Move files | Organization with move mode | Files moved to new locations | ⬜ |
| Copy files | Organization with copy mode | Files copied, originals kept | ⬜ |
| Rollback | Undo organization operation | Files restored to original | ⬜ |

---

## 9. Settings Tests

### 9.1 Settings API

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Get settings | GET `/api/settings` | Returns all settings | ⬜ |
| Update all | PUT `/api/settings` with full body | All settings updated | ⬜ |
| Update scanning | PATCH `/api/settings/scanning` | Only scanning updated | ⬜ |
| Update UI | PATCH `/api/settings/ui` | Theme, page size updated | ⬜ |
| Reset defaults | POST `/api/settings/reset` | All settings reset | ⬜ |

### 9.2 Settings Persistence

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Persist settings | Change setting, restart server | Setting persists | ⬜ |
| Invalid values | Set parallelThreads=1000 | Clamped to valid range | ⬜ |

---

## 10. Web UI Tests

### 10.1 Dashboard

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Dashboard loads | Navigate to `/` | Dashboard displays | ⬜ |
| Stats display | After scan | Shows ROM counts | ⬜ |
| Drive list | With registered drives | Shows drive cards | ⬜ |
| Quick actions | Click scan button | Scan initiates | ⬜ |

### 10.2 DAT Manager

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Page loads | Navigate to `/dats` | DAT list displays | ⬜ |
| Import DAT | Click import, select file | DAT imports | ⬜ |
| View DAT | Click DAT name | DAT details show | ⬜ |
| Delete DAT | Click delete, confirm | DAT removed | ⬜ |
| Search games | Search within DAT | Results filter | ⬜ |

### 10.3 ROM Browser

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Page loads | Navigate to `/roms` | ROM list displays | ⬜ |
| Virtual scroll | 60K+ ROMs | Smooth scrolling | ⬜ |
| Filter by status | Filter verified/unknown | List filters | ⬜ |
| Sort columns | Click column headers | Sorting works | ⬜ |
| Search | Enter search term | Results filter | ⬜ |

### 10.4 Verification Page

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Page loads | Navigate to `/verification` | Page displays | ⬜ |
| Stats display | After verification | Shows counts | ⬜ |
| Result details | Click result | Shows match info | ⬜ |

### 10.5 Settings Page

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Page loads | Navigate to `/settings` | Settings display | ⬜ |
| Change theme | Toggle dark/light | Theme changes | ⬜ |
| Save settings | Modify and save | Settings persist | ⬜ |

### 10.6 Real-time Updates

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| SignalR connects | Open UI | Hub connected | ⬜ |
| Scan progress | Start scan | Progress bar updates | ⬜ |
| Auto-refresh | Data changes | UI updates automatically | ⬜ |

---

## 11. System Tray App Tests

### 11.1 Tray Functionality

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| App starts | Run Subrom.Tray.exe | Tray icon appears | ⬜ |
| Single instance | Run twice | Second instance blocked | ⬜ |
| Open browser | Double-click tray icon | Browser opens to UI | ⬜ |
| Context menu | Right-click tray icon | Menu shows options | ⬜ |

### 11.2 Server Control

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Start server | Menu > Start Server | Server starts | ⬜ |
| Stop server | Menu > Stop Server | Server stops | ⬜ |
| Auto-start | With autostart enabled | Server starts with app | ⬜ |

### 11.3 Tray Settings

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Open settings | Menu > Settings | Dialog opens | ⬜ |
| Save settings | Modify and OK | Settings saved to file | ⬜ |
| Start with Windows | Enable/disable | Registry updated | ⬜ |

---

## 12. Performance Tests

### 12.1 Large Dataset

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| 60K+ entries DAT | Import GBA No-Intro | < 30 seconds | ⬜ |
| 10K+ ROM scan | Scan large folder | < 5 minutes | ⬜ |
| UI with 60K rows | Browse full list | Smooth scrolling | ⬜ |
| Memory usage | Monitor during operations | < 500MB | ⬜ |

### 12.2 Concurrent Operations

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Multiple scans | Start 2 scans | Queued correctly | ⬜ |
| API + UI | API calls during UI use | No blocking | ⬜ |

---

## 13. Edge Cases

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Empty folder scan | Scan folder with no ROMs | Completes, 0 results | ⬜ |
| Unicode filenames | ROMs with Japanese/Chinese names | Handles correctly | ⬜ |
| Long file paths | 260+ character paths | Handles or reports error | ⬜ |
| Read-only files | Scan read-only folder | Scans successfully | ⬜ |
| Locked files | File in use | Skips with warning | ⬜ |
| Corrupted archive | Damaged ZIP file | Reports error, continues | ⬜ |

---

## 14. Security Tests

| Test | Steps | Expected Result | Status |
|------|-------|-----------------|--------|
| Path traversal | Import DAT with `../` paths | Rejected or sanitized | ⬜ |
| Large file upload | Upload 1GB+ file | Rejected with size limit | ⬜ |
| CORS | Request from other origin | Blocked in production | ⬜ |

---

## Test Summary

### Category Results

| Category | Total | Pass | Fail | Skip |
|----------|-------|------|------|------|
| Server & API | 8 | | | |
| DAT Management | 10 | | | |
| Drive Management | 9 | | | |
| Scanning | 16 | | | |
| Verification | 7 | | | |
| Duplicates | 3 | | | |
| 1G1R | 4 | | | |
| Organization | 6 | | | |
| Settings | 6 | | | |
| Web UI | 20 | | | |
| Tray App | 9 | | | |
| Performance | 5 | | | |
| Edge Cases | 6 | | | |
| Security | 3 | | | |
| **TOTAL** | **112** | | | |

### Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Developer | | | |
| QA Tester | | | |
| Release Manager | | | |

### Notes

_Record any issues, observations, or blockers found during testing:_

---

## Issue Tracking

| Issue # | Description | Severity | Status |
|---------|-------------|----------|--------|
| | | | |
| | | | |
| | | | |

---

**Document Version:** 1.0
**Last Updated:** January 22, 2026
