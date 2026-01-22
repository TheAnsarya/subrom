# Subrom

**Modern ROM Collection Manager - A feature-rich alternative to RomVault and ClrMame Pro**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB)](https://react.dev/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-332%20passing-brightgreen)](tests/)

## Overview

Subrom is a modern ROM collection management tool with a beautiful web UI, real-time progress updates, and intelligent offline drive support. Built with .NET 10 and React 19, it provides professional-grade ROM verification and organization.

### Key Differentiators

| Feature | RomVault | ClrMame Pro | Subrom |
|---------|----------|-------------|--------|
| Web-based UI | ❌ | ❌ | ✅ |
| Real-time Progress | ❌ | ❌ | ✅ SignalR |
| Offline Drive Support | ❌ | ❌ | ✅ |
| Network Drives | ❌ | ❌ | ✅ UNC |
| Modern Architecture | ❌ | ❌ | ✅ |

## Features

### ✅ Complete & Working

- **DAT File Support**
  - Logiqx XML parser (No-Intro, Redump, MAME)
  - ClrMamePro DAT parser (TOSEC, legacy)
  - Streaming parser for 60K+ entry files
  - Category browser

- **ROM Scanning**
  - Recursive folder scanning
  - Archive support (ZIP, 7z, RAR, TAR, GZip)
  - Parallel hash computation (CRC32, MD5, SHA1)
  - ROM header detection and removal
  - Scan resume/checkpoint
  - Background job processing

- **Verification**
  - Hash-based verification
  - Duplicate detection
  - Bad dump identification
  - 1G1R filtering
  - Parent/clone detection

- **Organization**
  - 5 built-in templates
  - Custom placeholders
  - Move/copy with rollback
  - Dry-run preview
  - Operation undo

- **Storage Management**
  - Multi-drive support
  - Online/offline tracking
  - Network drives (UNC)
  - Space monitoring

- **Web UI**
  - Dashboard with stats
  - DAT manager
  - ROM browser (virtualized for 60K+ rows)
  - Verification results
  - Settings page
  - Dark/light themes

- **Desktop**
  - System tray application
  - Windows service
  - Auto-start support

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) with Yarn
- Windows 10/11 (for tray app and service)

### Installation

#### Option 1: From Source

```bash
# Clone the repository
git clone https://github.com/TheAnsarya/subrom.git
cd subrom

# Build backend
dotnet build Subrom.sln

# Build frontend
cd subrom-ui
yarn install
yarn build
cd ..

# Run the server
dotnet run --project src/Subrom.Server
```

The server will start at `http://localhost:52100`

#### Option 2: Development Mode

```bash
# Terminal 1 - Backend
dotnet run --project src/Subrom.Server

# Terminal 2 - Frontend (hot reload)
cd subrom-ui
yarn dev
```

Frontend dev server: `http://localhost:5173`

#### Option 3: System Tray (Windows)

```bash
# Build and run tray application
dotnet run --project src/Subrom.Tray
```

Right-click the tray icon to access:
- Open in Browser
- Start/Stop Server
- Settings
- Exit

### First Steps

1. **Import a DAT file**
   - Go to DAT Manager page
   - Click "Import DAT"
   - Upload a No-Intro, TOSEC, or other DAT file

2. **Register a drive**
   - Go to Settings → Drives
   - Add your ROM storage drive(s)

3. **Scan for ROMs**
   - Go to Dashboard
   - Click "Scan" on a registered drive
   - Watch real-time progress

4. **View Results**
   - Browse verified ROMs
   - See duplicates, missing, bad dumps
   - Apply 1G1R filtering

## Project Structure

```
Subrom.sln
├── src/
│   ├── Subrom.Domain/         # Domain models, value objects
│   ├── Subrom.Application/    # Service interfaces, DTOs
│   ├── Subrom.Infrastructure/ # EF Core, parsers, services
│   ├── Subrom.Server/         # ASP.NET Core Web API
│   ├── Subrom.Tray/           # Windows tray application
│   └── Subrom.Service/        # Windows service
├── tests/
│   └── Subrom.Tests.Unit/     # 332+ unit tests
├── subrom-ui/                 # React + TypeScript + Vite
└── ~docs/                     # Documentation
```

## API Reference

API documentation available at `/scalar/v1` when running in development mode.

Key endpoints:
- `GET /api/datfiles` - List DAT files
- `POST /api/datfiles/import` - Import DAT file
- `GET /api/drives` - List drives
- `POST /api/scans` - Start scan
- `GET /api/romfiles` - Browse ROMs
- `POST /api/verify` - Verify ROM

See [API Reference](~docs/api-reference.md) for full documentation.

## Configuration

Settings in `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:52100" }
    }
  }
}
```

Data stored in `%LOCALAPPDATA%/Subrom/`:
- `subrom.db` - SQLite database
- `logs/` - Application logs

## Documentation

- [Release Plan](~docs/plans/release-1.0.0-plan.md)
- [Architecture](~docs/plans/current-architecture.md)
- [API Design](~docs/plans/api-design.md)
- [Roadmap](~docs/plans/roadmap.md)
- [Changelog](CHANGELOG.md)

## Technology Stack

**Backend:**
- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- Entity Framework Core + SQLite
- SignalR for real-time updates
- Serilog structured logging

**Frontend:**
- React 19 + TypeScript 5.8
- Vite build tool
- Zustand state management
- react-window virtualization
- CSS Modules

**Testing:**
- xUnit
- Moq
- 332+ unit tests

## Contributing

1. Check [GitHub Issues](https://github.com/TheAnsarya/subrom/issues)
2. Fork and create a feature branch
3. Follow commit conventions: `feat(#issue): description`
4. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- [No-Intro](https://www.no-intro.org)
- [TOSEC](https://www.tosecdev.org)
- [Redump](http://redump.org)
- [SharpCompress](https://github.com/adamhathcock/sharpcompress)
