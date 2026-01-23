# Subrom

**Modern ROM Collection Manager - A feature-rich alternative to RomVault and ClrMame Pro**

[![CI](https://github.com/TheAnsarya/subrom/actions/workflows/ci.yml/badge.svg)](https://github.com/TheAnsarya/subrom/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-1.2.0-blue)](CHANGELOG.md)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB)](https://react.dev/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-375%20passing-brightgreen)](tests/)
[![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)]()

## 📥 Download

| Platform | Download | Notes |
|----------|----------|-------|
| **Windows** | [Subrom-1.2.0-win-x64.zip](https://github.com/TheAnsarya/subrom/releases/latest/download/Subrom-1.2.0-win-x64.zip) | Extract, run `install.bat` as Admin |
| **Linux** | [Subrom-1.2.0-linux-x64.tar.gz](https://github.com/TheAnsarya/subrom/releases/latest/download/Subrom-1.2.0-linux-x64.tar.gz) | Extract, run `sudo ./install.sh` |
| **macOS (Apple Silicon)** | [Subrom-1.2.0-osx-arm64.tar.gz](https://github.com/TheAnsarya/subrom/releases/latest/download/Subrom-1.2.0-osx-arm64.tar.gz) | Extract, run `./install.sh` |
| **macOS (Intel)** | [Subrom-1.2.0-osx-x64.tar.gz](https://github.com/TheAnsarya/subrom/releases/latest/download/Subrom-1.2.0-osx-x64.tar.gz) | Extract, run `./install.sh` |

**[All Releases](https://github.com/TheAnsarya/subrom/releases)** • **[Installation Guide](docs/installation.md)** • **[User Guide](docs/user-guide.md)**

---

## Overview

Subrom is a modern ROM collection management tool with a beautiful web UI, real-time progress updates, and intelligent offline drive support. Built with .NET 10 and React 19, it provides professional-grade ROM verification and organization.

### Key Differentiators

| Feature | RomVault | ClrMame Pro | Subrom |
|---------|----------|-------------|--------|
| Web-based UI | ❌ | ❌ | ✅ |
| Real-time Progress | ❌ | ❌ | ✅ SignalR |
| Offline Drive Support | ❌ | ❌ | ✅ |
| Network Drives | ❌ | ❌ | ✅ UNC |
| Cross-Platform | ❌ | ❌ | ✅ Win/Linux/macOS |
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
- [Node.js 20+](https://nodejs.org/) with Yarn (for building from source)

### Platform Support

| Feature | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Server | ✅ | ✅ | ✅ |
| Web UI | ✅ | ✅ | ✅ |
| System Tray App | ✅ | ❌ | ❌ |
| System Service | ✅ | ✅ systemd | ✅ launchd |

### Installation

<details>
<summary><b>🪟 Windows</b></summary>

#### Option 1: From Source

```powershell
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

#### Option 2: System Tray Application

```powershell
# Build and run tray application
dotnet run --project src/Subrom.Tray
```

Right-click the tray icon to access:
- Open in Browser
- Start/Stop Server  
- Settings
- Exit

#### Option 3: Windows Service

```powershell
# Build the service
dotnet publish src/Subrom.Service -c Release -o C:\Subrom

# Install as Windows Service (Admin PowerShell)
sc.exe create SubromService binPath="C:\Subrom\Subrom.Service.exe" start=auto
sc.exe start SubromService
```

</details>

<details>
<summary><b>🐧 Linux</b></summary>

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

#### Option 2: Publish & Install as systemd Service

```bash
# Build self-contained release
dotnet publish src/Subrom.Server -c Release -r linux-x64 --self-contained -o ./release

# Copy to installation directory
sudo cp -r ./release/* /opt/subrom/
sudo cp scripts/linux/subrom.service /etc/systemd/system/

# Create service user
sudo useradd --system --no-create-home subrom

# Set permissions
sudo chown -R subrom:subrom /opt/subrom
sudo chmod +x /opt/subrom/Subrom.Server

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable subrom
sudo systemctl start subrom
```

#### Check Status

```bash
sudo systemctl status subrom
sudo journalctl -u subrom -f
```

</details>

<details>
<summary><b>🍎 macOS</b></summary>

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

#### Option 2: Publish & Install as launchd Service

```bash
# Build self-contained release (Apple Silicon)
dotnet publish src/Subrom.Server -c Release -r osx-arm64 --self-contained -o ./release

# Or for Intel Macs
dotnet publish src/Subrom.Server -c Release -r osx-x64 --self-contained -o ./release

# Copy to Applications
sudo mkdir -p /Applications/Subrom
sudo cp -r ./release/* /Applications/Subrom/
sudo cp scripts/macos/com.subrom.server.plist ~/Library/LaunchAgents/

# Start service
launchctl load ~/Library/LaunchAgents/com.subrom.server.plist
```

#### Check Status

```bash
launchctl list | grep subrom
tail -f /tmp/subrom.log
```

</details>

The server will start at `http://localhost:52100`

### Data Locations

| Platform | Data Directory | Logs |
|----------|----------------|------|
| Windows | `%LOCALAPPDATA%\Subrom\` | `%LOCALAPPDATA%\Subrom\logs\` |
| Linux | `~/.config/subrom/` | `~/.config/subrom/logs/` |
| macOS | `~/Library/Application Support/Subrom/` | `~/Library/Logs/Subrom/` |

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

Settings stored in `appsettings.json` in the application directory:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:52100" }
    }
  }
}
```

To change the port or bind address, set the environment variable:
```bash
ASPNETCORE_URLS=http://0.0.0.0:8080  # Bind to all interfaces on port 8080
```

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
- 359+ unit tests

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
