# Subrom Installation Guide

Complete installation instructions for all platforms.

## Table of Contents
- [Windows](#windows)
- [Linux](#linux)
- [macOS](#macos)
- [Building from Source](#building-from-source)

---

## Windows

### Option 1: MSI Installer (Recommended)

1. Download `Subrom-x.x.x-win-x64.msi` from [GitHub Releases](https://github.com/TheAnsarya/subrom/releases)
2. Double-click the MSI file to run the installer
3. Follow the installation wizard
4. Choose installation options:
`t - **Subrom Core** - Required server and web interface
`t - **Background Service** - Windows Service for auto-start (recommended)
`t - **System Tray Application** - Tray icon for easy access (recommended)
`t - **Shortcuts** - Start Menu and Desktop shortcuts

After installation:
- The server starts automatically as a Windows Service
- A system tray icon appears for quick access
- Access the web UI at **http://localhost:5678**

### Option 2: Portable ZIP

1. Download `Subrom-x.x.x-win-x64.zip` from GitHub Releases
2. Extract to any folder
3. Run `Subrom.Server.exe` or `Subrom.Tray.exe`

### Uninstalling

Use Windows Settings > Apps > Subrom, or run the MSI installer and choose "Remove".

---

## Linux

### Option 1: DEB Package (Debian/Ubuntu)

```bash
# Download the package
wget https://github.com/TheAnsarya/subrom/releases/download/vX.X.X/subrom_X.X.X_amd64.deb

# Install
sudo dpkg -i subrom_X.X.X_amd64.deb

# Or with apt (handles dependencies)
sudo apt install ./subrom_X.X.X_amd64.deb
```

The service starts automatically. Access at **http://localhost:5678**

#### Managing the Service

```bash
# Check status
sudo systemctl status subrom

# Stop/Start/Restart
sudo systemctl stop subrom
sudo systemctl start subrom
sudo systemctl restart subrom

# View logs
journalctl -u subrom -f
```

### Option 2: RPM Package (Fedora/RHEL/CentOS)

```bash
# Download the package
wget https://github.com/TheAnsarya/subrom/releases/download/vX.X.X/subrom-X.X.X-1.x86_64.rpm

# Install
sudo dnf install ./subrom-X.X.X-1.x86_64.rpm
# Or on older systems:
sudo yum install ./subrom-X.X.X-1.x86_64.rpm
```

### Option 3: AppImage (Universal)

```bash
# Download
wget https://github.com/TheAnsarya/subrom/releases/download/vX.X.X/Subrom-X.X.X-x86_64.AppImage

# Make executable
chmod +x Subrom-X.X.X-x86_64.AppImage

# Run
./Subrom-X.X.X-x86_64.AppImage
```

No installation required! Data is stored in `~/.local/share/subrom/`

### Uninstalling

```bash
# DEB
sudo apt remove subrom
sudo apt purge subrom  # Also removes config files

# RPM
sudo dnf remove subrom

# AppImage - just delete the file
rm Subrom-X.X.X-x86_64.AppImage
```

---

## macOS

### Option 1: PKG Installer (Recommended)

1. Download `Subrom-X.X.X.pkg` from [GitHub Releases](https://github.com/TheAnsarya/subrom/releases)
2. Double-click to open the installer
3. Follow the installation wizard
4. If prompted about unidentified developer:
`t - Go to System Settings > Privacy & Security
`t - Click "Open Anyway"

After installation:
- Subrom is installed to `/Applications/Subrom.app`
- A LaunchAgent starts the server automatically on login
- Access the web UI at **http://localhost:5678**

### Option 2: DMG (Drag and Drop)

1. Download `Subrom-X.X.X.dmg` from GitHub Releases
2. Open the DMG file
3. Drag Subrom to Applications folder
4. Run Subrom from Applications

### Managing the Service

```bash
# Stop the service
launchctl unload ~/Library/LaunchAgents/com.subrom.server.plist

# Start the service
launchctl load ~/Library/LaunchAgents/com.subrom.server.plist

# View logs
tail -f ~/Library/Logs/Subrom/stdout.log
```

### Uninstalling

1. Stop the service (see above)
2. Delete `/Applications/Subrom.app`
3. Delete `~/Library/LaunchAgents/com.subrom.server.plist`
4. Delete `~/Library/Application Support/Subrom/` (optional - removes data)
5. Delete `~/Library/Logs/Subrom/` (optional - removes logs)

---

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) (for UI development)
- [Yarn](https://yarnpkg.com/) (package manager for UI)

### Build Steps

```bash
# Clone the repository
git clone https://github.com/TheAnsarya/subrom.git
cd subrom

# Build the backend
dotnet build Subrom.sln

# Build the UI
cd subrom-ui
yarn install
yarn build
cd ..

# Run the server
dotnet run --project src/Subrom.Server
```

### Building Installers

See [Installer Build Guide](../installers/README.md) for creating platform-specific installers.

---

## Configuration

### Data Directories

| Platform | Data Directory |
|----------|---------------|
| Windows | `%ProgramData%\Subrom` or `%APPDATA%\Subrom` |
| Linux | `~/.local/share/subrom` or `/var/lib/subrom` |
| macOS | `~/Library/Application Support/Subrom` |

### Log Directories

| Platform | Log Directory |
|----------|--------------|
| Windows | `%ProgramData%\Subrom\Logs` |
| Linux | `~/.local/share/subrom/logs` or `/var/log/subrom` |
| macOS | `~/Library/Logs/Subrom` |

### Server Port

Default: **5678**

To change, set the environment variable:
```bash
export ASPNETCORE_URLS=http://localhost:8080
```

Or edit `appsettings.json`:
```json
{
`t"Urls": "http://localhost:8080"
}
```

---

## Troubleshooting

### Server won't start

1. Check if port 5678 is in use: 
`t - Windows: `netstat -ano | findstr 5678`
`t - Linux/macOS: `lsof -i :5678`
2. Check logs for errors
3. Ensure .NET runtime is installed

### Can't access web interface

1. Ensure server is running
2. Try `http://127.0.0.1:5678` instead of `localhost`
3. Check firewall settings

### Service won't auto-start

- **Windows**: Check Services (services.msc) for "SubromService"
- **Linux**: Check `systemctl status subrom`
- **macOS**: Check `launchctl list | grep subrom`

---

## Next Steps

- [User Guide](user-guide.md) - Learn how to use Subrom
- [API Reference](api-reference.md) - For developers
