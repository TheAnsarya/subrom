# Cross-Platform Installer Plan

## Overview

Subrom needs professional installers for Windows, Linux, and macOS that:
- Install the server application
- Set up background services
- Install the system tray application
- Create shortcuts and menu entries
- Handle updates and uninstallation

## Platform-Specific Approaches

### Windows - MSI + WiX Toolset

**Technology:** WiX Toolset v5 (modern .NET-based)

**Features:**
- MSI installer for enterprise deployment
- Windows Service installation
- System tray application
- Start Menu shortcuts
- Desktop shortcut (optional)
- Uninstaller
- Upgrade support

**Components:**
1. `Subrom.Server.exe` - Web API server
2. `Subrom.Service.exe` - Windows Service wrapper
3. `Subrom.Tray.exe` - System tray application
4. SQLite database (user data directory)

**Registry Entries:**
- `HKLM\SOFTWARE\Subrom` - Installation path
- `HKCU\SOFTWARE\Subrom` - User preferences
- Service registration

### Linux - DEB/RPM + systemd

**Technology:** 
- `.deb` package for Debian/Ubuntu
- `.rpm` package for Fedora/RHEL/CentOS
- AppImage for universal distribution

**Features:**
- systemd service unit
- Desktop entry file
- App indicator/tray icon (via libappindicator)
- Package manager integration
- Automatic service start

**Files:**
- `/opt/subrom/` - Application files
- `/etc/subrom/` - Configuration
- `~/.local/share/subrom/` - User data (XDG compliant)
- `/lib/systemd/system/subrom.service` - Service unit

### macOS - PKG + launchd

**Technology:**
- `.pkg` installer (productbuild)
- Optional: `.dmg` disk image
- Notarized and signed for Gatekeeper

**Features:**
- LaunchAgent for background service
- Menu bar application
- Applications folder installation
- Uninstaller script

**Files:**
- `/Applications/Subrom.app` - Application bundle
- `~/Library/Application Support/Subrom/` - User data
- `~/Library/LaunchAgents/com.subrom.server.plist` - Launch agent

## Build System

### Directory Structure

```
installers/
├── build.ps1                 # Master build script (cross-platform)
├── build.sh                  # Linux/macOS build script
├── common/
│   ├── version.json          # Version info for all platforms
│   └── assets/
│       ├── icon.ico          # Windows icon
│       ├── icon.icns         # macOS icon
│       └── icon.png          # Linux icon (multiple sizes)
├── windows/
│   ├── Subrom.Installer.wixproj
│   ├── Product.wxs           # Main WiX configuration
│   ├── Service.wxs           # Service component
│   └── UI.wxs                # Custom UI
├── linux/
│   ├── debian/
│   │   ├── control
│   │   ├── postinst
│   │   ├── prerm
│   │   └── postrm
│   ├── rpm/
│   │   └── subrom.spec
│   └── appimage/
│       └── AppDir/
└── macos/
	├── Distribution.xml
	├── Scripts/
	│   ├── preinstall
	│   └── postinstall
	└── Subrom.app/
		└── Contents/
			├── Info.plist
			└── MacOS/
```

## Build Commands

### Windows
```powershell
# Build MSI
dotnet build installers/windows/Subrom.Installer.wixproj -c Release
# Output: installers/windows/bin/Release/Subrom-1.2.0-win-x64.msi
```

### Linux
```bash
# Build DEB
./installers/linux/build-deb.sh
# Output: installers/linux/dist/subrom_1.2.0_amd64.deb

# Build RPM
./installers/linux/build-rpm.sh
# Output: installers/linux/dist/subrom-1.2.0-1.x86_64.rpm

# Build AppImage
./installers/linux/build-appimage.sh
# Output: installers/linux/dist/Subrom-1.2.0-x86_64.AppImage
```

### macOS
```bash
# Build PKG
./installers/macos/build-pkg.sh
# Output: installers/macos/dist/Subrom-1.2.0.pkg

# Build DMG (optional)
./installers/macos/build-dmg.sh
# Output: installers/macos/dist/Subrom-1.2.0.dmg
```

## Implementation Phases

### Phase 1: Windows MSI (Priority)
1. Create WiX project structure
2. Define Product.wxs with components
3. Add service installation
4. Create custom installer UI
5. Test installation/uninstallation
6. Add upgrade support

### Phase 2: Linux Packages
1. Create DEB package structure
2. Write postinst/prerm scripts
3. Create RPM spec file
4. Build AppImage for universal support
5. Test on Ubuntu, Fedora, Arch

### Phase 3: macOS PKG
1. Create app bundle structure
2. Write Distribution.xml
3. Create install/uninstall scripts
4. Sign and notarize
5. Test on macOS 12+

## CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Build Installers

on:
`tpush:
	tags:
	`t- 'v*'

jobs:
`tbuild-windows:
	runs-on: windows-latest
	steps:
	`t- uses: actions/checkout@v4
	`t- name: Build MSI
		run: dotnet build installers/windows/Subrom.Installer.wixproj -c Release

`tbuild-linux:
	runs-on: ubuntu-latest
	steps:
	`t- uses: actions/checkout@v4
	`t- name: Build DEB
		run: ./installers/linux/build-deb.sh

`tbuild-macos:
	runs-on: macos-latest
	steps:
	`t- uses: actions/checkout@v4
	`t- name: Build PKG
		run: ./installers/macos/build-pkg.sh
```

## Version Management

All installers pull version from:
1. `installers/common/version.json`
2. Git tag (for CI builds)
3. Assembly version in .csproj files

## Testing Requirements

### Windows
- [ ] Clean install on Windows 10
- [ ] Clean install on Windows 11
- [ ] Upgrade from previous version
- [ ] Uninstall completely
- [ ] Service starts automatically
- [ ] Tray app starts on login
- [ ] Shortcuts work correctly

### Linux
- [ ] Install on Ubuntu 22.04
- [ ] Install on Ubuntu 24.04
- [ ] Install on Fedora 39
- [ ] Install on Debian 12
- [ ] AppImage runs on various distros
- [ ] systemd service works
- [ ] Desktop entry appears

### macOS
- [ ] Install on macOS 12 Monterey
- [ ] Install on macOS 13 Ventura
- [ ] Install on macOS 14 Sonoma
- [ ] LaunchAgent works
- [ ] Menu bar app works
- [ ] Uninstall removes all files

## Security Considerations

### Windows
- Code signing with EV certificate (optional for now)
- SmartScreen reputation building

### Linux
- GPG signed packages (optional)
- AppImage signature verification

### macOS
- Apple Developer ID signing (required)
- Notarization (required for Gatekeeper)
- Hardened runtime

## Related Documents

- [~docs/plans/cross-platform-plan.md](cross-platform-plan.md) - Overall cross-platform strategy
- [~docs/issues/epic-15-installers.md](../issues/epic-15-installers.md) - Epic tracking
- [scripts/](../../scripts/) - Platform-specific scripts
