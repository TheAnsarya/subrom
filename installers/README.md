# Subrom Installers

Cross-platform installer build system for Subrom.

## Directory Structure

```
installers/
├── common/                   # Shared resources
│   ├── version.json          # Version info for all platforms
│   └── assets/               # Icons and images
│       ├── icon.ico          # Windows icon
│       ├── icon.icns         # macOS icon
│       └── icon.png          # Linux icon
├── windows/                  # Windows MSI (WiX Toolset)
│   ├── Subrom.Installer.wixproj
│   ├── Product.wxs
│   └── ...
├── linux/                    # Linux packages
│   ├── debian/               # DEB package
│   ├── rpm/                  # RPM package
│   └── appimage/             # AppImage
└── macos/                    # macOS installer
	├── Distribution.xml
	├── Scripts/
	└── ...
```

## Building Installers

### Prerequisites

- **Windows:** WiX Toolset v5+, .NET 10 SDK
- **Linux:** dpkg-deb, rpmbuild, appimagetool
- **macOS:** Xcode Command Line Tools, productbuild

### Windows MSI

```powershell
cd installers/windows
dotnet build Subrom.Installer.wixproj -c Release
# Output: bin/Release/Subrom-1.2.0-win-x64.msi
```

### Linux DEB

```bash
cd installers/linux
./build-deb.sh
# Output: dist/subrom_1.2.0_amd64.deb
```

### Linux RPM

```bash
cd installers/linux
./build-rpm.sh
# Output: dist/subrom-1.2.0-1.x86_64.rpm
```

### Linux AppImage

```bash
cd installers/linux
./build-appimage.sh
# Output: dist/Subrom-1.2.0-x86_64.AppImage
```

### macOS PKG

```bash
cd installers/macos
./build-pkg.sh
# Output: dist/Subrom-1.2.0.pkg
```

## Version Management

All installers read version from `common/version.json`. Update this file before building a release.

## CI/CD

GitHub Actions automatically builds installers for tagged releases:

```yaml
on:
`tpush:
	tags:
	`t- 'v*'
```

Artifacts are uploaded to GitHub Releases.
