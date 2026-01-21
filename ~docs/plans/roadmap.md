# Subrom Project Roadmap

## Vision

Build a modern, efficient ROM management system that rivals RomVault and ClrMame Pro, with a beautiful web-based UI, robust offline support, and intelligent DAT file management from all major providers.

## Project Phases

### Phase 1: Foundation (Current) 🚧

**Goal:** Establish core infrastructure and basic DAT file support

**Timeline:** Q1 2026

**Deliverables:**
- [x] Domain models for DAT files (Datafile, Game, Rom, etc.)
- [x] Hash computation service (CRC32, MD5, SHA1)
- [x] Basic project structure with layered architecture
- [ ] Complete DAT file parsing (XML format)
- [ ] DAT file parsing (ClrMame Pro format)
- [ ] Database schema for ROM catalog
- [ ] File scanner service
- [ ] Basic CLI interface

**GitHub Epic:** #1 - Foundation Infrastructure

---

### Phase 2: DAT Providers (Q2 2026) 📋

**Goal:** Integrate with major DAT file providers

**Timeline:** Q2 2026

**Deliverables:**
- [ ] No-Intro DAT downloader and parser
- [ ] TOSEC DAT downloader and parser
- [ ] Redump DAT support
- [ ] GoodSets DAT support (legacy)
- [ ] MAME DAT support
- [ ] DAT update service with scheduling
- [ ] DAT version tracking and diff detection
- [ ] DAT merge/conflict resolution

**GitHub Epic:** #2 - DAT Provider Integration

---

### Phase 3: ROM Scanning & Verification (Q2-Q3 2026) 🔍

**Goal:** Build comprehensive ROM scanning and verification engine

**Timeline:** Q2-Q3 2026

**Deliverables:**
- [ ] Recursive folder scanner
- [ ] Archive support (ZIP, 7z, RAR)
- [ ] ROM header detection and removal
- [ ] Hash database with indexing
- [ ] ROM verification against DAT files
- [ ] Missing ROM detection
- [ ] Duplicate detection
- [ ] Bad dump identification
- [ ] Scan progress and resumability

**GitHub Epic:** #3 - ROM Scanning Engine

---

### Phase 4: File Organization (Q3 2026) ✅ COMPLETE

**Goal:** Implement intelligent ROM organization system

**Timeline:** Q3 2026

**Status:** Completed (10/10 issues)

**Deliverables:**
- [x] Configurable folder structures (5 built-in templates)
- [x] ROM renaming engine (TemplateParser with placeholders)
- [x] 1G1R (1 Game 1 ROM) support (OneGameOneRomService)
- [x] Region/language prioritization (configurable priority lists)
- [x] Parent/clone organization (DAT-based and inference)
- [x] Move/copy operations with rollback (OrganizationService)
- [x] Dry-run mode (PlanAsync)
- [x] Operation logging and undo (OrganizationOperationLog)

**Key Components:**
- `OrganizationTemplate` - Entity with built-in templates
- `TemplateParser` - Parse and validate template strings  
- `IOrganizationService` - Plan, execute, rollback operations
- `IOneGameOneRomService` - 1G1R filtering with scoring
- `IParentCloneService` - Parent/clone relationship management
- `OrganizationOperationLog` - Persistent operation history

**GitHub Epic:** #35 (Closed)

---

### Phase 5: Storage Management (Q3-Q4 2026) 💾

**Goal:** Multi-drive and offline storage support

**Timeline:** Q3-Q4 2026

**Deliverables:**
- [ ] Multi-drive ROM storage
- [ ] Drive registration and tracking
- [ ] Offline drive handling (don't drop files!)
- [ ] Drive space monitoring
- [ ] ROM location database
- [ ] Missing drive notifications
- [ ] Automatic relocation suggestions
- [ ] Network drive support

**GitHub Epic:** #5 - Storage Management

---

### Phase 6: Web UI (Q4 2026) 🎨

**Goal:** Modern React-based web interface

**Timeline:** Q4 2026

**Deliverables:**
- [ ] Dashboard with collection overview
- [ ] DAT file browser and manager
- [ ] ROM collection navigator
- [ ] Search and filter functionality
- [ ] Scan progress visualization
- [ ] Settings and configuration UI
- [ ] Dark/light theme support
- [ ] Responsive design

**GitHub Epic:** #6 - Web UI

---

### Phase 7: Advanced Features (2027) 🚀

**Goal:** Power user features and integrations

**Timeline:** 2027

**Deliverables:**
- [ ] ROM download integration
- [ ] RetroArch playlist generation
- [ ] EmulationStation gamelist.xml generation
- [ ] Box art and metadata scraping
- [ ] Collection statistics and reports
- [ ] Backup and restore
- [ ] Plugin system
- [ ] API for external tools

**GitHub Epic:** #7 - Advanced Features

---

## Technology Stack

### Backend
- **.NET 8+** - Core runtime
- **ASP.NET Core** - Web API
- **Entity Framework Core** - Database ORM
- **SQLite** - Local database
- **LiteDB** - Document storage option

### Frontend
- **React 18+** - UI framework
- **TypeScript** - Type safety
- **TailwindCSS** - Styling
- **React Query** - Data fetching
- **React Router** - Navigation

### Infrastructure
- **GitHub Actions** - CI/CD
- **Docker** - Containerization (optional)

---

## Success Metrics

1. **Performance:** Scan 10,000 files in under 60 seconds
2. **Accuracy:** 100% hash verification accuracy
3. **Reliability:** Zero data loss during organization
4. **Usability:** < 5 minute setup to first scan
5. **Coverage:** Support all major DAT providers

---

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
