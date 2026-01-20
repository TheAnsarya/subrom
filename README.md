# Subrom

**ROM Manager made as an efficient alternative to RomVault**

[![GitHub Issues](https://img.shields.io/github/issues/TheAnsarya/subrom)](https://github.com/TheAnsarya/subrom/issues)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

Subrom is a modern ROM collection management tool designed to be a fast, cross-platform alternative to RomVault and ClrMame Pro. It features a web-based UI, intelligent DAT file management, and most importantly - **never loses track of your ROMs when drives go offline**.

## Key Features

### Current
- ✅ Multi-hash computation (CRC32, MD5, SHA1) in parallel
- ✅ XML DAT file parsing (LogiqX/No-Intro format)
- ✅ ClrMame Pro DAT format support
- ✅ File scanning with progress reporting
- ✅ Hash verification against DAT files
- ✅ 7-Zip compression support

### Planned
- 🔄 Web-based UI with React
- 🔄 Multi-drive storage management
- 🔄 **Offline drive resilience** (ROMs never "lost" when drives disconnect)
- 🔄 DAT providers (No-Intro, TOSEC, Redump, GoodSets)
- 🔄 1G1R (1 Game 1 ROM) filtering
- 🔄 Intelligent ROM organization
- 🔄 RetroArch/EmulationStation playlist generation

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Subrom UI                                │
│                    (React + TypeScript)                          │
├─────────────────────────────────────────────────────────────────┤
│                        REST API                                  │
│                   (ASP.NET Core Web API)                        │
├─────────────────────────────────────────────────────────────────┤
│                     Service Layer                                │
│  DatService │ ScanService │ DriveService │ VerificationService  │
├─────────────────────────────────────────────────────────────────┤
│                     Domain Layer                                 │
│        Datfiles │ Hash │ Storage                                │
├─────────────────────────────────────────────────────────────────┤
│                   Infrastructure                                 │
│      Database │ DAT Parsers │ File System                       │
└─────────────────────────────────────────────────────────────────┘
```

## Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- Node.js 18+ (for UI development)

### Build

```bash
# Build the solution
dotnet build Subrom.sln

# Run the API
dotnet run --project Subrom/SubromAPI.csproj
```

### UI Development

```bash
cd subrom-ui
npm install
npm start
```

## Project Structure

```
Subrom.sln
├── Domain/              # Core domain models
│   ├── Datfiles/       # DAT file models (Datafile, Game, Rom)
│   ├── Hash/           # Hash value types (Crc, Md5, Sha1)
│   └── Storage/        # Storage models (Drive, RomFile, ScanJob)
├── Services/           # Business logic services
│   ├── HashService     # Multi-hash computation
│   ├── DatService      # DAT file management
│   ├── DriveService    # Storage drive management
│   └── Verification    # ROM verification
├── Infrastructure/     # External concerns
│   ├── Parsers/        # DAT file parsers
│   └── Extensions/     # Extension methods
├── Compression/        # Archive handling (7-Zip)
├── Subrom/            # Web API project
└── subrom-ui/         # React frontend
```

## Documentation

Detailed documentation is available in the `~docs/` folder:

- [Project Roadmap](~docs/plans/roadmap.md) - Development phases and timeline
- [Architecture](~docs/plans/architecture.md) - System design and data flow
- [UI Design](~docs/plans/ui-plans.md) - UI mockups and component plans
- [API Design](~docs/plans/api-design.md) - REST API specification
- [GitHub Epics](~docs/issues/epics.md) - Issue tracking and epics

### Research
- [DAT Formats](~docs/research/dat-formats.md) - DAT file format documentation
- [Providers](~docs/research/providers.md) - ROM set provider analysis
- [Competitors](~docs/research/competitors.md) - RomVault/ClrMame analysis

## Offline Drive Resilience

**This is a key differentiator for Subrom.**

Unlike other ROM managers, Subrom **never loses track of your ROMs** when drives go offline:

1. When you register a drive, all ROM locations are tracked in the database
2. If a drive goes offline, ROM records are marked as "offline" - **not deleted**
3. When the drive comes back online, everything is automatically restored
4. You always know what ROMs you have, even if they're on a disconnected drive

## Supported DAT Formats

| Format | Status | Notes |
|--------|--------|-------|
| XML/LogiqX | ✅ Supported | No-Intro, Redump, MAME |
| ClrMame Pro | ✅ Supported | Plain text format |
| TOSEC | 🔄 Planned | Uses XML with TOSEC naming |

## Supported Archive Formats

| Format | Status |
|--------|--------|
| ZIP | ✅ Supported |
| 7z | ✅ Supported |
| RAR | 🔄 Planned |

## Contributing

Contributions are welcome! Please check our [GitHub Issues](https://github.com/TheAnsarya/subrom/issues) for current tasks.

### Commit Convention

All commits should reference an issue:

```
feat(#8): implement XML DAT parser
fix(#20): handle offline drive reconnection
docs(#24): update README
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [No-Intro](https://www.no-intro.org) - ROM verification standards
- [TOSEC](https://www.tosecdev.org) - Comprehensive ROM preservation
- [Redump](http://redump.org) - Disc image verification
- [RomVault](https://www.romvault.com) - Inspiration for features
