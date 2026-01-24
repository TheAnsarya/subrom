# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Cross-platform tray app (Avalonia)
- Performance benchmarks
- Docker image

## [1.2.0] - 2026-01-22

### Added
- **Cross-Platform Installers** 📦
`t- Windows MSI installer with WiX Toolset v5
	- Windows Service installation and auto-start
	- System tray application with auto-start on login
	- Start Menu and Desktop shortcuts
	- Upgrade support for seamless updates
`t- Linux DEB package for Debian/Ubuntu
	- systemd service integration
	- Auto-start on boot
	- Proper file permissions and user creation
`t- Linux RPM package for Fedora/RHEL
`t- Linux AppImage for universal distribution
	- No installation required
	- XDG-compliant data directories
`t- macOS PKG installer
	- App bundle with proper Info.plist
	- LaunchAgent for auto-start on login
	- Optional DMG creation

- **Build Infrastructure**
`t- `installers/` directory with cross-platform build scripts
`t- `version.json` for centralized version management
`t- GitHub Actions workflow for automated installer builds
`t- Artifact upload to GitHub Releases on tag push

- **New Documentation**
`t- Cross-platform installer plan (`~docs/plans/cross-platform-installer-plan.md`)
`t- Epic #15: Cross-Platform Installers tracking
`t- Installer README with build instructions

### Changed
- API version updated to 1.2.0
- Test count: 375 (maintained from 1.1.0)

## [1.1.0] - 2026-01-22

### Added
- **Multi-Drive Parallel Scanning** 🚀
`t- `ParallelScanService` for scanning multiple drives simultaneously
`t- Configurable concurrency limits (drives and hashes per drive)
`t- SSD drive prioritization option
`t- Multi-drive progress tracking with per-drive status

- **Scan Queue Management** 📋
`t- `ScanQueueService` for priority-based scan scheduling
`t- Pause/resume queue processing
`t- Priority levels (Low, Normal, High)
`t- Move jobs to front/back of queue
`t- Job cancellation support
`t- Queue statistics endpoint

- **Export Functionality** 📤
`t- `ExportService` for CSV and JSON export
`t- Export all ROMs or filter by drive
`t- Export by verification status (Verified, Unknown, BadDump)
`t- Collection summary export with statistics
`t- Download endpoint with Content-Disposition header

- **New API Endpoints**
`t- `/api/export/roms/csv` - Export ROMs as CSV
`t- `/api/export/roms/json` - Export ROMs as JSON
`t- `/api/export/roms/by-status/{status}` - Export by verification status
`t- `/api/export/summary` - Collection statistics summary
`t- `/api/export/download/roms` - Download export as file
`t- `/api/scan-queue/` - Queue management endpoints
`t- `/api/scan-queue/stats` - Queue statistics
`t- `/api/scan-queue/pause` - Pause queue
`t- `/api/scan-queue/resume` - Resume queue
`t- `/api/scan-queue/{id}/priority` - Change job priority
`t- `/api/scan-queue/{id}/move-to-front` - Move to front
`t- `/api/scan-queue/{id}/move-to-back` - Move to back

### Changed
- API version updated to 1.1.0
- Test count: 375 (maintained from 1.0.0)

## [1.0.0] - 2026-01-22

### Added
- **Cross-Platform Support** 🎉
`t- Full Windows, Linux, and macOS support
`t- `PlatformHelper` utility class for cross-platform directory resolution
`t- Platform-specific data directories (XDG on Linux, Application Support on macOS)
`t- Linux systemd service configuration
`t- macOS launchd service configuration
`t- Installation scripts for Linux and macOS
`t- 16 new unit tests for platform utilities

- **Settings Persistence**
`t- AppSettings domain entity with comprehensive configuration
`t- Settings API endpoints (GET, PUT, PATCH per category, POST reset)
`t- Scanning, organization, UI, storage, and verification settings
`t- Settings persist across server restarts
`t- 27 unit tests for settings

- **Global Error Handling**
`t- `/error` endpoint with ProblemDetails response format
`t- Exception type mapping (404 for KeyNotFoundException, 400 for InvalidOperation)
`t- Development vs production error detail levels

- **Documentation**
`t- Comprehensive platform-specific installation guides
`t- Manual testing guide (112 test cases)
`t- Cross-platform deployment documentation

### Changed
- Test count: 375 (up from 332)
- Server now logs platform name on startup
- Data/log directories use platform-appropriate locations

## [1.0.0-rc1] - 2026-01-22

### Added
- **Settings Persistence**
`t- AppSettings domain entity with comprehensive configuration
`t- Settings API endpoints (GET, PUT, PATCH per category, POST reset)
`t- Scanning, organization, UI, storage, and verification settings
`t- Settings persist across server restarts
`t- 27 new unit tests for settings

- **Global Error Handling**
`t- `/error` endpoint with ProblemDetails response format
`t- Exception type mapping (404 for KeyNotFoundException, 400 for InvalidOperation)
`t- Development vs production error detail levels

- **Documentation**
`t- Comprehensive 1.0.0 release plan
`t- Manual testing guide (112 test cases)
`t- Updated README with installation guide
`t- Feature comparison with RomVault and ClrMame Pro

### Changed
- Test count increased from 332 to 359 (exceeded 350 target)
- Epic #13 progress: 0% → 50%

### Fixed
- All critical blockers for 1.0.0 resolved

## [1.0.0-alpha] - 2026-01-22

### Added

#### Core Features
- **DAT File Support**
`t- Logiqx XML DAT parser with streaming support
`t- ClrMamePro DAT parser with 21 unit tests
`t- DAT import/export via REST API
`t- DAT category browser endpoint
`t- Support for No-Intro, TOSEC, Redump, MAME formats

- **ROM Scanning**
`t- Recursive folder scanning
`t- Archive support (ZIP, 7z, RAR, TAR, GZip)
`t- Parallel hash computation (CRC32, MD5, SHA1)
`t- ROM header detection and removal
`t- Scan job queue with background processing
`t- Scan resume/checkpoint system
`t- Real-time progress via SignalR

- **ROM Verification**
`t- Hash-based verification against DAT files
`t- Missing ROM detection
`t- Duplicate detection
`t- Bad dump identification
`t- 1G1R (1 Game 1 ROM) filtering
`t- Parent/clone relationship detection

- **File Organization**
`t- 5 built-in organization templates
`t- Custom template support with placeholders
`t- Move/copy operations with rollback
`t- Dry-run mode for previewing changes
`t- Operation logging and undo support

- **Storage Management**
`t- Multi-drive ROM storage support
`t- Drive registration and tracking
`t- Online/offline drive handling
`t- Network drive support (UNC paths)
`t- Drive space monitoring
`t- Relocation suggestions

- **Web UI**
`t- Modern React 19 + TypeScript + Vite frontend
`t- Dashboard with collection statistics
`t- DAT file manager with import
`t- ROM collection browser with virtualized tables
`t- Verification results viewer
`t- Settings configuration page
`t- Dark/light theme support
`t- Real-time progress updates via SignalR

- **Desktop Integration**
`t- Windows system tray application
`t- Windows service for background operation
`t- Single-instance enforcement
`t- Notification support

- **API & Infrastructure**
`t- ASP.NET Core Minimal APIs
`t- OpenAPI/Scalar documentation
`t- SignalR hub for real-time events
`t- SQLite database with WAL mode
`t- Serilog structured logging
`t- Health check endpoint

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
