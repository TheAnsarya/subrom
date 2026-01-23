# Subrom Documentation Index

Welcome to Subrom - a ROM management and verification toolkit.

## Quick Links

### Getting Started
- [README](../README.md) - Project overview, features, and quick start
- [Installation Guide](installation.md) - Detailed installation instructions
- [User Guide](user-guide.md) - How to use Subrom

### For Developers
- [API Reference](api-reference.md) - REST API documentation
- [Architecture Overview](plans/current-architecture.md) - System design
- [CI/CD Guide](ci-cd.md) - GitHub Actions workflows
- [Contributing Guide](../CONTRIBUTING.md) - How to contribute

### Downloads
- [GitHub Releases](https://github.com/TheAnsarya/subrom/releases) - Download installers

---

## Documentation Structure

```
docs/
├── index.md                    # This file - documentation hub
├── installation.md             # Installation guide for all platforms
├── user-guide.md               # End-user documentation
├── api-reference.md            # API documentation
└── plans/                      # Technical planning documents
    ├── current-architecture.md
    ├── cross-platform-plan.md
    ├── cross-platform-installer-plan.md
    └── ...

~docs/                          # Development documentation
├── issues/
│   └── epics.md               # Issue tracking and roadmap
└── plans/                     # Planning documents
```

---

## Installation

### Windows
Download the MSI installer from [GitHub Releases](https://github.com/TheAnsarya/subrom/releases):
- `Subrom-x.x.x-win-x64.msi` - Full installer with service and tray app

### Linux
Download from [GitHub Releases](https://github.com/TheAnsarya/subrom/releases):
- `subrom_x.x.x_amd64.deb` - Debian/Ubuntu package
- `subrom-x.x.x-1.x86_64.rpm` - Fedora/RHEL package  
- `Subrom-x.x.x-x86_64.AppImage` - Universal Linux (no install needed)

### macOS
Download from [GitHub Releases](https://github.com/TheAnsarya/subrom/releases):
- `Subrom-x.x.x.pkg` - macOS installer package

---

## Features Overview

| Feature | Description |
|---------|-------------|
| DAT Parsing | Parse No-Intro, TOSEC, GoodTools catalogs |
| ROM Verification | Verify ROMs against DAT files using CRC32, MD5, SHA1 |
| Archive Support | ZIP, 7z, RAR, TAR, GZip archives |
| Multi-Drive Scanning | Parallel scanning across multiple drives |
| Real-Time Progress | SignalR streaming for live updates |
| Web Interface | Modern React-based UI |
| Background Service | Runs as Windows Service, systemd, or launchd |
| Export | CSV/JSON export of verification results |

---

## Related Documentation

### User Documentation
| Document | Description |
|----------|-------------|
| [Installation](installation.md) | Step-by-step installation |
| [User Guide](user-guide.md) | Using Subrom day-to-day |
| [FAQ](faq.md) | Frequently asked questions |

### Developer Documentation
| Document | Description |
|----------|-------------|
| [API Reference](api-reference.md) | REST API endpoints |
| [Architecture](plans/current-architecture.md) | System design |
| [Backend Rebuild](plans/backend-rebuild.md) | Backend architecture |
| [Installer Plan](~docs/plans/cross-platform-installer-plan.md) | Installer infrastructure |

### Project Management
| Document | Description |
|----------|-------------|
| [Epics & Issues](~docs/issues/epics.md) | GitHub issue tracking |
| [Roadmap](plans/roadmap.md) | Future plans |
| [CHANGELOG](../CHANGELOG.md) | Version history |

---

## Support

- **Issues:** [GitHub Issues](https://github.com/TheAnsarya/subrom/issues)
- **Discussions:** [GitHub Discussions](https://github.com/TheAnsarya/subrom/discussions)

---

## License

MIT License - See [LICENSE](../LICENSE.md)
