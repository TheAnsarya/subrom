# Competitor Analysis: ROM Management Tools

## Overview

This document analyzes existing ROM management tools to inform Subrom's design.

---

## RomVault

**Website:** https://www.romvault.com
**Platform:** Windows (.NET)
**License:** Proprietary (free)
**Status:** Actively maintained

### Strengths
- Fast scanning with multi-threaded hashing
- Excellent archive support (ZIP, 7z)
- Tree-based DAT organization
- ToSort functionality
- Multiple fix options
- RVDB format for DAT caching

### Weaknesses
- Windows-only
- Complex interface for beginners
- Limited customization
- No built-in DAT downloading

### Features to Emulate
- ✅ Multi-threaded hash calculation
- ✅ Archive-aware scanning
- ✅ DAT tree organization
- ✅ ToSort for unknown files
- ✅ Fix scripting

### Features to Improve Upon
- 🔄 Cross-platform support
- 🔄 Modern UI
- 🔄 Built-in DAT provider integration
- 🔄 Better documentation

---

## ClrMame Pro

**Website:** https://mamedev.emulab.it/clrmamepro/
**Platform:** Windows
**License:** Freeware
**Status:** Actively maintained

### Strengths
- Industry standard for MAME
- Powerful batch operations
- Flexible renaming options
- Profile system for different sets
- Advanced filtering

### Weaknesses
- Steep learning curve
- Dated interface
- Windows-only
- Slow initial scan

### Features to Emulate
- ✅ Profile/workspace system
- ✅ Comprehensive rebuild options
- ✅ Scanner cache database
- ✅ Parent/clone awareness

### Features to Improve Upon
- 🔄 User-friendly interface
- 🔄 Modern tech stack
- 🔄 Cross-platform

---

## Romulus

**Website:** https://romulus.dats.site
**Platform:** Windows (.NET)
**License:** Free
**Status:** Actively maintained

### Strengths
- Modern .NET codebase
- Clean interface
- Good performance
- Active development

### Weaknesses
- Fewer features than RomVault
- Limited documentation
- Windows-only

### Features to Emulate
- ✅ Clean UI design
- ✅ Modern .NET practices
- ✅ Fast development cycle

---

## JRomManager

**Website:** https://github.com/optyfr/JRomManager
**Platform:** Cross-platform (Java)
**License:** GPL-2.0
**Status:** Actively maintained

### Strengths
- Cross-platform
- Open source
- Active community
- Good feature set

### Weaknesses
- Java dependency
- Memory hungry
- Slower than native solutions

### Features to Emulate
- ✅ Cross-platform architecture
- ✅ Open source community
- ✅ Plugin system

---

## ROMVault Feature Comparison

| Feature | RomVault | ClrMame | Romulus | JRomMgr | **Subrom** |
|---------|----------|---------|---------|---------|------------|
| Cross-platform | ❌ | ❌ | ❌ | ✅ | ✅ |
| Modern UI | ⚠️ | ❌ | ✅ | ✅ | ✅ |
| Web interface | ❌ | ❌ | ❌ | ❌ | ✅ |
| Built-in DAT download | ❌ | ❌ | ❌ | ⚠️ | ✅ |
| Multi-drive support | ✅ | ⚠️ | ⚠️ | ⚠️ | ✅ |
| Offline drive handling | ❌ | ❌ | ❌ | ❌ | ✅ |
| Archive support | ✅ | ✅ | ✅ | ✅ | ✅ |
| 7z support | ✅ | ✅ | ⚠️ | ✅ | ✅ |
| Header skip | ✅ | ✅ | ✅ | ✅ | ✅ |
| 1G1R | ✅ | ⚠️ | ⚠️ | ✅ | ✅ |
| Open source | ❌ | ❌ | ❌ | ✅ | ✅ |

Legend: ✅ Full support | ⚠️ Partial | ❌ No support

---

## Key Differentiators for Subrom

### 1. **Cross-Platform First**
- .NET 8+ for native cross-platform
- Web UI accessible from any device
- Same features on Windows, macOS, Linux

### 2. **Offline Drive Resilience**
- **Critical differentiator:** Never lose track of ROMs when drives go offline
- Database preserves all ROM information
- Automatic reconnection when drives return
- Clear status indicators for offline content

### 3. **Modern Web UI**
- React-based responsive interface
- Real-time updates via WebSocket
- Dashboard with statistics
- Mobile-friendly

### 4. **Integrated DAT Management**
- Built-in provider integration
- Automatic update checking
- Version tracking and diff
- No manual DAT hunting

### 5. **Developer Friendly**
- Open source
- Clean API
- Plugin architecture
- Well documented

### 6. **Smart Organization**
- Template-based folder structures
- 1G1R with region priorities
- Parent/clone awareness
- Preview before changes

---

## User Experience Priorities

### Target Users

1. **Casual Collector**
   - Simple setup
   - Automatic organization
   - Clear status display

2. **Power User**
   - Full control over organization
   - Batch operations
   - Advanced filtering

3. **Archivist**
   - Complete preservation
   - Multi-set support
   - Verification reports

### Onboarding Flow

```
1. Welcome → 2. Add Drives → 3. Select DATs → 4. First Scan → 5. Dashboard

Time to first scan: < 5 minutes
```

### Key Metrics
- Time to first scan: < 5 minutes
- Scan speed: > 50 files/second
- UI response time: < 100ms
- Zero data loss guarantee

---

## Technical Lessons

### From RomVault
- Use cached hash database (RVDB equivalent)
- Multi-threaded scanning is essential
- Archive-native operations are faster

### From ClrMame Pro
- Profile system is useful for multiple collections
- Scanner cache dramatically improves repeat scans
- Flexible rebuilding options needed

### From JRomManager
- Cross-platform is achievable
- Plugin system enables community extensions
- Open source builds trust

### Unique to Subrom
- Web-first UI approach
- Offline resilience as core feature
- Integrated DAT ecosystem

---

## Implementation Priorities

### Phase 1: Core Competency
Match basic features of RomVault:
- DAT parsing
- File scanning
- Hash verification
- Basic organization

### Phase 2: Differentiation
Add unique value:
- Multi-drive with offline support
- Web UI
- DAT provider integration

### Phase 3: Power Features
Match advanced features:
- Complex rebuild operations
- Parent/clone handling
- 1G1R implementation

### Phase 4: Ecosystem
Build competitive advantage:
- Plugin system
- Community DAT sharing
- Integration with emulators

---

## References

- [RomVault Documentation](https://www.romvault.com/rvreadme.html)
- [ClrMame Pro Manual](https://mamedev.emulab.it/clrmamepro/docs/)
- [JRomManager GitHub](https://github.com/optyfr/JRomManager)
