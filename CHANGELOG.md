# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Global error handling with ProblemDetails response format
- 1.0.0 release planning documentation

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
| 1.0.0-alpha | 2026-01-22 | Initial alpha release |

## Links

- [GitHub Repository](https://github.com/TheAnsarya/subrom)
- [Documentation](~docs/README.md)
- [API Reference](~docs/api-reference.md)
