# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Cross-platform tray app (Avalonia)
- Performance benchmarks
- Windows installer (MSI)
- Docker image

## [1.1.0] - 2026-01-22

### Added
- **Multi-Drive Parallel Scanning** 🚀
  - `ParallelScanService` for scanning multiple drives simultaneously
  - Configurable concurrency limits (drives and hashes per drive)
  - SSD drive prioritization option
  - Multi-drive progress tracking with per-drive status

- **Scan Queue Management** 📋
  - `ScanQueueService` for priority-based scan scheduling
  - Pause/resume queue processing
  - Priority levels (Low, Normal, High)
  - Move jobs to front/back of queue
  - Job cancellation support
  - Queue statistics endpoint

- **Export Functionality** 📤
  - `ExportService` for CSV and JSON export
  - Export all ROMs or filter by drive
  - Export by verification status (Verified, Unknown, BadDump)
  - Collection summary export with statistics
  - Download endpoint with Content-Disposition header

- **New API Endpoints**
  - `/api/export/roms/csv` - Export ROMs as CSV
  - `/api/export/roms/json` - Export ROMs as JSON
  - `/api/export/roms/by-status/{status}` - Export by verification status
  - `/api/export/summary` - Collection statistics summary
  - `/api/export/download/roms` - Download export as file
  - `/api/scan-queue/` - Queue management endpoints
  - `/api/scan-queue/stats` - Queue statistics
  - `/api/scan-queue/pause` - Pause queue
  - `/api/scan-queue/resume` - Resume queue
  - `/api/scan-queue/{id}/priority` - Change job priority
  - `/api/scan-queue/{id}/move-to-front` - Move to front
  - `/api/scan-queue/{id}/move-to-back` - Move to back

### Changed
- API version updated to 1.1.0
- Test count: 375 (maintained from 1.0.0)

## [1.0.0] - 2026-01-22

### Added
- **Cross-Platform Support** 🎉
  - Full Windows, Linux, and macOS support
  - `PlatformHelper` utility class for cross-platform directory resolution
  - Platform-specific data directories (XDG on Linux, Application Support on macOS)
  - Linux systemd service configuration
  - macOS launchd service configuration
  - Installation scripts for Linux and macOS
  - 16 new unit tests for platform utilities

- **Settings Persistence**
  - AppSettings domain entity with comprehensive configuration
  - Settings API endpoints (GET, PUT, PATCH per category, POST reset)
  - Scanning, organization, UI, storage, and verification settings
  - Settings persist across server restarts
  - 27 unit tests for settings

- **Global Error Handling**
  - `/error` endpoint with ProblemDetails response format
  - Exception type mapping (404 for KeyNotFoundException, 400 for InvalidOperation)
  - Development vs production error detail levels

- **Documentation**
  - Comprehensive platform-specific installation guides
  - Manual testing guide (112 test cases)
  - Cross-platform deployment documentation

### Changed
- Test count: 375 (up from 332)
- Server now logs platform name on startup
- Data/log directories use platform-appropriate locations

## [1.0.0-rc1] - 2026-01-22

### Added
- **Settings Persistence**
  - AppSettings domain entity with comprehensive configuration
  - Settings API endpoints (GET, PUT, PATCH per category, POST reset)
  - Scanning, organization, UI, storage, and verification settings
  - Settings persist across server restarts
  - 27 new unit tests for settings

- **Global Error Handling**
  - `/error` endpoint with ProblemDetails response format
  - Exception type mapping (404 for KeyNotFoundException, 400 for InvalidOperation)
  - Development vs production error detail levels

- **Documentation**
  - Comprehensive 1.0.0 release plan
  - Manual testing guide (112 test cases)
  - Updated README with installation guide
  - Feature comparison with RomVault and ClrMame Pro

### Changed
- Test count increased from 332 to 359 (exceeded 350 target)
- Epic #13 progress: 0% → 50%

### Fixed
- All critical blockers for 1.0.0 resolved

## [1.0.0-alpha] - 2026-01-22

### Added

#### Core Features
- **DAT File Support**
  - Logiqx XML DAT parser with streaming support
  - ClrMamePro DAT parser with 21 unit tests
  - DAT import/export via REST API
  - DAT category browser endpoint
  - Support for No-Intro, TOSEC, Redump, MAME formats

- **ROM Scanning**
  - Recursive folder scanning
  - Archive support (ZIP, 7z, RAR, TAR, GZip)
  - Parallel hash computation (CRC32, MD5, SHA1)
  - ROM header detection and removal
  - Scan job queue with background processing
  - Scan resume/checkpoint system
  - Real-time progress via SignalR

- **ROM Verification**
  - Hash-based verification against DAT files
  - Missing ROM detection
  - Duplicate detection
  - Bad dump identification
  - 1G1R (1 Game 1 ROM) filtering
  - Parent/clone relationship detection

- **File Organization**
  - 5 built-in organization templates
  - Custom template support with placeholders
  - Move/copy operations with rollback
  - Dry-run mode for previewing changes
  - Operation logging and undo support

- **Storage Management**
  - Multi-drive ROM storage support
  - Drive registration and tracking
  - Online/offline drive handling
  - Network drive support (UNC paths)
  - Drive space monitoring
  - Relocation suggestions

- **Web UI**
  - Modern React 19 + TypeScript + Vite frontend
  - Dashboard with collection statistics
  - DAT file manager with import
  - ROM collection browser with virtualized tables
  - Verification results viewer
  - Settings configuration page
  - Dark/light theme support
  - Real-time progress updates via SignalR

- **Desktop Integration**
  - Windows system tray application
  - Windows service for background operation
  - Single-instance enforcement
  - Notification support

- **API & Infrastructure**
  - ASP.NET Core Minimal APIs
  - OpenAPI/Scalar documentation
  - SignalR hub for real-time events
  - SQLite database with WAL mode
  - Serilog structured logging
  - Health check endpoint

### Technical Details
- Built on .NET 10 / C# 14
- 332+ unit tests with 100% pass rate
- Zero compiler warnings
- Clean architecture with Domain/Application/Infrastructure layers

### Known Limitations
- DAT auto-sync from providers requires manual download
- No-Intro website blocked automated access (manual DAT download required)
- Windows-only for system tray and service features

---

## Version History

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.0-rc1 | 2026-01-22 | Release candidate with settings, error handling, testing guide |
| 1.0.0-alpha | 2026-01-22 | Initial alpha release |

## Links

- [GitHub Repository](https://github.com/TheAnsarya/subrom)
- [Documentation](~docs/README.md)
- [API Reference](~docs/api-reference.md)
