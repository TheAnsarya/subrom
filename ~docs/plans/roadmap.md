# Subrom Project Roadmap

**Last Updated:** January 22, 2026 (Cross-Platform Planning)

## Vision

Build a modern, efficient ROM management system that rivals RomVault and ClrMame Pro, with a beautiful web-based UI, robust offline support, and intelligent DAT file management from all major providers.

**Run Anywhere:** Windows, Linux, macOS

## Current Status: 1.0.0 🎉

**Released:** January 22, 2026

All core features complete:
- ✅ DAT parsing (Logiqx XML, ClrMamePro)
- ✅ ROM scanning with archive support
- ✅ Hash computation (CRC32, MD5, SHA1)
- ✅ ROM verification
- ✅ File organization with templates
- ✅ Multi-drive support
- ✅ Web UI with real-time updates
- ✅ Cross-platform (Windows, Linux, macOS)
- ✅ System tray app (Windows)
- ✅ 375 unit tests passing

---

## Release Schedule

| Version | Target | Status | Focus |
|---------|--------|--------|-------|
| 1.0.0 | Jan 2026 | ✅ Released | Cross-platform, core features |
| 1.1.0 | Q2 2026 | 🎯 Next | Performance, UX polish |
| 1.2.0 | Q3 2026 | 📋 Planned | Integration features |
| 2.0.0 | Q4 2026 | 📋 Planned | Major enhancements |

---

## 1.0.0 Final Release ✅ Complete

**Goal:** Production-ready stable release with cross-platform support

### Cross-Platform Support ✅ DONE

| Platform | Server | Web UI | Tray App | Service |
|----------|--------|--------|----------|---------|
| **Windows** | ✅ | ✅ | ✅ | ✅ |
| **Linux** | ✅ | ✅ | ⏸️ 1.1.0 | ✅ systemd |
| **macOS** | ✅ | ✅ | ⏸️ 1.1.0 | ✅ launchd |

See [cross-platform-plan.md](cross-platform-plan.md) for full details.

### Completed Features

- ✅ Cross-platform support (Linux, macOS)
- ✅ Platform-aware data directory resolution
- ✅ systemd and launchd service configs
- ✅ Installation scripts
- ✅ Comprehensive documentation
- ✅ 375 unit tests passing

---

## 1.1.0 - Performance & Polish (Q2 2026)

**Goal:** Optimize performance and improve user experience

### Performance Improvements

| Feature | Description | Priority |
|---------|-------------|----------|
| Multi-drive parallel scanning | Spread I/O load across drives during scan | HIGH |
| Memory-mapped file hashing | Use MemoryMappedFiles for large ROMs (>100MB) | HIGH |
| Database query optimization | Add missing indexes, batch operations | MEDIUM |
| Archive caching | Cache extracted files for repeated access | MEDIUM |
| Lazy loading in UI | Load ROM details on demand | MEDIUM |

#### Multi-Drive Parallel Scanning

Current behavior: Sequential scanning across drives
Proposed: Parallel I/O across different physical drives

```
Before: Drive A → Drive B → Drive C (serial)
After:  Drive A ↘
	    Drive B → (parallel, I/O distributed)  
	    Drive C ↗
```

Benefits:
- Better utilization of multiple physical disks
- Reduced total scan time for multi-drive collections
- Configurable parallelism per drive type (SSD vs HDD)

#### Memory-Mapped Hashing

For large files (>100MB), use memory-mapped files instead of FileStream:
- Reduces memory pressure
- Allows OS to manage page caching
- Better performance for very large files (ISO, disc images)

### UX Improvements

| Feature | Description | Priority |
|---------|-------------|----------|
| Scan queue management | Pause, resume, reorder scan queue | HIGH |
| Batch operations UI | Select multiple ROMs for operations | HIGH |
| Keyboard shortcuts | Power user navigation | MEDIUM |
| Export results | CSV/JSON export of verification results | MEDIUM |
| UI loading states | Skeleton loaders, better feedback | LOW |
| Responsive mobile design | Basic mobile-friendly layout | LOW |

### Quality of Life

| Feature | Description | Priority |
|---------|-------------|----------|
| Recent files list | Quick access to recently used DATs | HIGH |
| Favorites/bookmarks | Mark frequently used items | MEDIUM |
| Search history | Remember recent searches | LOW |
| Customizable columns | Show/hide columns in ROM list | MEDIUM |

---

## 1.2.0 - Integrations (Q3 2026)

**Goal:** Connect with external tools and services

### Emulator Integrations

| Feature | Description | Priority |
|---------|-------------|----------|
| RetroArch playlist export | Generate .lpl playlist files | HIGH |
| EmulationStation gamelist | Generate gamelist.xml for ES | HIGH |
| LaunchBox import/export | Interop with LaunchBox databases | MEDIUM |

### File Management

| Feature | Description | Priority |
|---------|-------------|----------|
| Watch folders | Auto-scan when files added | HIGH |
| Scheduled scans | Run scans on schedule | MEDIUM |
| File integrity checking | Periodic hash verification | MEDIUM |
| Compression optimization | Recompress archives (torrentzip) | LOW |

### Reporting

| Feature | Description | Priority |
|---------|-------------|----------|
| Collection statistics | Charts, graphs, completion % | HIGH |
| Missing ROMs report | Exportable missing list | HIGH |
| Duplicate report | Find and manage duplicates | MEDIUM |
| DAT coverage report | Which DATs have matches | MEDIUM |

---

## 2.0.0 - Major Enhancements (Q4 2026)

**Goal:** Power user features and extensibility

### Advanced Features

| Feature | Description | Priority |
|---------|-------------|----------|
| DAT editor | Create/modify DAT files | MEDIUM |
| Custom hash algorithms | Support additional hashes | LOW |
| Scripting support | PowerShell/Python automation | MEDIUM |
| Backup/restore | Database and settings backup | HIGH |

### Multi-User Support

| Feature | Description | Priority |
|---------|-------------|----------|
| User accounts | Multiple users, separate settings | MEDIUM |
| Shared database | Network database option | LOW |
| Permission levels | Admin/user roles | LOW |

### Platform Support

| Feature | Description | Priority |
|---------|-------------|----------|
| Linux support | Service and CLI for Linux | MEDIUM |
| Docker image | Containerized deployment | MEDIUM |
| macOS support | Basic macOS compatibility | LOW |

---

## Deferred/Reconsidered Features

The following features from the original roadmap have been reconsidered:

| Feature | Original Plan | Decision | Reason |
|---------|--------------|----------|--------|
| ROM download integration | 2027 | ❌ Removed | Legal concerns, out of scope |
| Box art scraping | 2027 | ⏸️ Deferred | Nice-to-have, not core |
| Plugin system | 2027 | ⏸️ Deferred | Complexity, limited benefit |
| LiteDB option | Phase 1 | ❌ Removed | SQLite sufficient |
| DAT auto-sync | Phase 2 | ⏸️ Deferred | Providers block automation |

---

## Technical Stack

### Backend
- **Runtime:** .NET 10 with C# 14
- **Database:** SQLite with EF Core (WAL mode)
- **API:** ASP.NET Core Minimal APIs
- **Real-time:** SignalR for progress streaming
- **Hashing:** Built-in + optimized implementations
- **Compression:** SharpCompress, 7-Zip SDK

### Frontend
- **Framework:** React 19 with TypeScript 5.8
- **State:** Zustand for global state
- **UI:** FontAwesome icons, CSS Modules
- **Build:** Vite 6.0
- **Lists:** react-window for virtualization

### Desktop
- **System Tray:** Windows Service + WebView2
- **Notifications:** Windows toast notifications

---

## Success Metrics

### Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| Scan 10,000 ROMs | < 5 minutes | ✅ Met |
| Hash 1GB file | < 30 seconds | ✅ Met |
| UI response time | < 100ms | ✅ Met |
| Memory usage (scan) | < 500MB | ✅ Met |

### Quality Targets

| Metric | Target | Current |
|--------|--------|---------|
| Unit test coverage | > 80% | ~85% |
| Build warnings | 0 | ✅ 0 |
| Test count | 300+ | ✅ 359 |

---

## Contributing

We welcome contributions! Priority areas:

- 🐛 Bug reports and fixes
- 📖 Documentation improvements
- 🧪 Test coverage expansion
- 🎨 UI/UX improvements
- 🔧 Performance optimizations

### Getting Started

```bash
# Clone repository
git clone https://github.com/TheAnsarya/subrom.git
cd subrom

# Backend
dotnet build
dotnet test

# Frontend
cd subrom-ui
yarn install
yarn dev
```

---

## Links

- **Repository:** [github.com/TheAnsarya/subrom](https://github.com/TheAnsarya/subrom)
- **Releases:** [GitHub Releases](https://github.com/TheAnsarya/subrom/releases)
- **Issues:** [GitHub Issues](https://github.com/TheAnsarya/subrom/issues)

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Data loss during file moves | Implement dry-run mode, undo operations, extensive logging |
| Offline drives losing ROM tracking | Keep ROM records, mark as "offline", reconnect when drive returns |
| Large DAT files causing memory issues | Stream parsing, database indexing |
| Archive extraction performance | Parallel extraction, caching |

---

## Related Documents

- [Architecture Overview](architecture.md)
- [UI Design Plans](ui-plans.md)
- [API Design](api-design.md)
- [GitHub Epics](../issues/epics.md)
