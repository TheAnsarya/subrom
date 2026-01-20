# GitHub Epics and Issue Tracking

This document tracks all GitHub epics and their associated issues for the Subrom project.

## Epic Overview

| Epic # | Title | Status | Issues | Progress |
|--------|-------|--------|--------|----------|
| #1 | Foundation Infrastructure | 🟡 In Progress | 15 | 60% |
| #2 | DAT Provider Integration | 🟡 In Progress | 12 | 10% |
| #3 | ROM Scanning Engine | 🟡 In Progress | 14 | 40% |
| #4 | File Organization | ⚪ Not Started | 10 | 0% |
| #5 | Storage Management | ⚪ Not Started | 8 | 0% |
| #6 | Web UI Rebuild | 🟢 Near Complete | 25 | 92% |
| #7 | Advanced Features | ⚪ Not Started | 15 | 0% |
| #8 | Large Dataset Handling | 🟡 In Progress | 20 | 30% |
| #9 | Backend Rebuild | 🟡 In Progress | 35 | 75% |
| #10 | System Tray & Service | ⚪ Not Started | 15 | 0% |

---

## Epic #1: Foundation Infrastructure

**Goal:** Establish core infrastructure, domain models, and basic services

**Labels:** `epic`, `foundation`, `priority-high`

### Issues

| # | Title | Status | Assignee |
|---|-------|--------|----------|
| #10 | Define domain models for DAT files | ✅ Done | - |
| #11 | Implement Hash value types (Crc, Md5, Sha1) | ✅ Done | - |
| #12 | Create HashService for parallel hashing | ✅ Done | - |
| #13 | Implement XML DAT file parser | ✅ Done | - |
| #14 | Implement ClrMame Pro DAT parser | ✅ Done | - |
| #15 | Design database schema | ✅ Done | - |
| #16 | Implement EF Core DbContext | ✅ Done | - |
| #17 | Create database migrations | ✅ Done | - |
| #18 | Implement basic file scanner | ✅ Done | - |
| #19 | Create CLI project structure | ⬜ Todo | - |
| #20 | Add logging infrastructure | ✅ Done | - |
| #21 | Implement configuration system | ⬜ Todo | - |
| #22 | Add unit test project | ⬜ Todo | - |
| #23 | Set up CI/CD pipeline | ⬜ Todo | - |
| #24 | Create README and documentation | ⬜ Todo | - |

---

## Epic #2: DAT Provider Integration

**Goal:** Integrate with all major DAT file providers

**Labels:** `epic`, `dat-providers`, `priority-high`

### Issues

| # | Title | Status | Assignee |
|---|-------|--------|----------|
| #30 | Research No-Intro DAT distribution | ⬜ Todo | - |
| #31 | Implement No-Intro DAT downloader | ⬜ Todo | - |
| #32 | Research TOSEC DAT distribution | ⬜ Todo | - |
| #33 | Implement TOSEC DAT downloader | ⬜ Todo | - |
| #34 | Research Redump DAT distribution | ⬜ Todo | - |
| #35 | Implement Redump DAT downloader | ⬜ Todo | - |
| #36 | Add GoodSets DAT support (legacy) | ⬜ Todo | - |
| #37 | Add MAME DAT support | ⬜ Todo | - |
| #38 | Implement DAT update scheduler | ⬜ Todo | - |
| #39 | Add DAT version tracking | ⬜ Todo | - |
| #40 | Implement DAT diff detection | ⬜ Todo | - |
| #41 | Create DAT merge/conflict resolution | ⬜ Todo | - |

---

## Epic #3: ROM Scanning Engine

**Goal:** Build comprehensive ROM scanning and verification engine

**Labels:** `epic`, `scanning`, `priority-high`

### Issues

| # | Title | Status | Assignee |
|---|-------|--------|----------|
| #50 | Implement recursive folder scanner | ✅ Done | - |
| #51 | Add ZIP archive support | ⬜ Todo | - |
| #52 | Add 7z archive support | ⬜ Todo | - |
| #53 | Add RAR archive support | ⬜ Todo | - |
| #54 | Implement ROM header detection | ⬜ Todo | - |
| #55 | Create header removal service | ⬜ Todo | - |
| #56 | Build hash database with indexing | ✅ Done | - |
| #57 | Implement ROM verification against DATs | ✅ Done | - |
| #58 | Create missing ROM detection | ✅ Done | - |
| #59 | Implement duplicate detection | ⬜ Todo | - |
| #60 | Add bad dump identification | ⬜ Todo | - |
| #61 | Implement scan progress tracking | ✅ Done | - |
| #62 | Add scan resumability | ⬜ Todo | - |
| #63 | Create scan result reporting | ✅ Done | - |

---

## Epic #4: File Organization

**Goal:** Implement intelligent ROM organization system

**Labels:** `epic`, `organization`, `priority-medium`

### Issues

| # | Title | Status | Assignee |
|---|-------|--------|----------|
| #70 | Design folder structure templates | ⬜ Todo | - |
| #71 | Implement template parser | ⬜ Todo | - |
| #72 | Create ROM renaming engine | ⬜ Todo | - |
| #73 | Implement 1G1R support | ⬜ Todo | - |
| #74 | Add region/language prioritization | ⬜ Todo | - |
| #75 | Implement parent/clone organization | ⬜ Todo | - |
| #76 | Create move/copy operations with rollback | ⬜ Todo | - |
| #77 | Add dry-run mode | ⬜ Todo | - |
| #78 | Implement operation logging | ⬜ Todo | - |
| #79 | Create undo functionality | ⬜ Todo | - |

---

## Epic #5: Storage Management

**Goal:** Multi-drive and offline storage support

**Labels:** `epic`, `storage`, `priority-medium`

### Issues

| # | Title | Status | Assignee |
|---|-------|--------|----------|
| #80 | Design drive registration system | ⬜ Todo | - |
| #81 | Implement drive tracking database | ⬜ Todo | - |
| #82 | Create offline drive handling | ⬜ Todo | - |
| #83 | Add drive space monitoring | ⬜ Todo | - |
| #84 | Implement ROM location database | ⬜ Todo | - |
| #85 | Create missing drive notifications | ⬜ Todo | - |
| #86 | Add automatic relocation suggestions | ⬜ Todo | - |
| #87 | Implement network drive support | ⬜ Todo | - |

---

## Epic #6: Web UI Rebuild

**Goal:** Complete rebuild of React frontend with modern Vite tooling and best practices

**Labels:** `epic`, `ui`, `priority-high`

**Status:** 🟢 75% Complete

### Sub-Epic #6.1: Project Setup

**Parent:** #6 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #200 | Create new Vite + React 19 project | ✅ Done | #6.1 |
| #201 | Configure TypeScript 5.8 with strict mode | ✅ Done | #6.1 |
| #202 | Set up .editorconfig (tabs, K&R braces) | ✅ Done | #6.1 |
| #203 | Configure path aliases (@/ imports) | ✅ Done | #6.1 |
| #204 | Create CSS variables and theme system | ✅ Done | #6.1 |
| #205 | Set up Yarn 4 with node-modules linker | ✅ Done | #6.1 |

### Sub-Epic #6.2: Core Components

**Parent:** #6 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #210 | Create Layout component (sidebar, header) | ✅ Done | #6.2 |
| #211 | Create DataTable with sort/filter/pagination | ✅ Done | #6.2 |
| #212 | Create Modal dialog component | ✅ Done | #6.2 |
| #213 | Create FileUpload component | ✅ Done | #6.2 |
| #214 | Create ProgressBar component | ✅ Done | #6.2 |
| #215 | Create Button, Input, Select components | ✅ Done | #6.2 |
| #216 | Create Toast notification system | ✅ Done | #6.2 |

### Sub-Epic #6.3: API Integration

**Parent:** #6 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #220 | Create fetch wrapper with error handling | ✅ Done | #6.3 |
| #221 | Implement DAT files API client | ✅ Done | #6.3 |
| #222 | Implement ROM files API client | ✅ Done | #6.3 |
| #223 | Implement scan API client | ✅ Done | #6.3 |
| #224 | Implement verification API client | ✅ Done | #6.3 |
| #225 | Set up SignalR connection for real-time | ✅ Done | #6.3 |
| #226 | Create useApi and useScanProgress hooks | ✅ Done | #6.3 |

### Sub-Epic #6.4: Pages

**Parent:** #6 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #230 | Create Dashboard page with stats | ✅ Done | #6.4 |
| #231 | Create DAT Manager page with import | ✅ Done | #6.4 |
| #232 | Create ROM Files browser page | ✅ Done | #6.4 |
| #233 | Create Verification results page | ✅ Done | #6.4 |
| #234 | Create Settings page | ✅ Done | #6.4 |
| #235 | Implement React Router navigation | ✅ Done | #6.4 |

### Sub-Epic #6.5: Polish & UX

**Parent:** #6 | **Status:** 🟡 In Progress

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #240 | Implement dark/light theme toggle | ✅ Done | #6.5 |
| #241 | Add responsive design breakpoints | ⬜ Todo | #6.5 |
| #242 | Add loading skeletons | ⬜ Todo | #6.5 |
| #243 | Implement error boundaries | ⬜ Todo | #6.5 |
| #244 | Add keyboard shortcuts | ⬜ Todo | #6.5 |

---

## Epic #7: Advanced Features

**Goal:** Power user features and integrations

**Labels:** `epic`, `advanced`, `priority-low`

### Issues

| # | Title | Status | Assignee |
|---|-------|--------|----------|
| #110 | Research ROM download sources | ⬜ Todo | - |
| #111 | Implement RetroArch playlist generation | ⬜ Todo | - |
| #112 | Create EmulationStation gamelist.xml generator | ⬜ Todo | - |
| #113 | Research box art/metadata sources | ⬜ Todo | - |
| #114 | Implement metadata scraping | ⬜ Todo | - |
| #115 | Create collection statistics | ⬜ Todo | - |
| #116 | Implement statistics reports | ⬜ Todo | - |
| #117 | Create backup system | ⬜ Todo | - |
| #118 | Implement restore functionality | ⬜ Todo | - |
| #119 | Design plugin architecture | ⬜ Todo | - |
| #120 | Implement plugin loading | ⬜ Todo | - |
| #121 | Create plugin SDK | ⬜ Todo | - |
| #122 | Design public API for third-party tools | ⬜ Todo | - |
| #123 | Create API documentation | ⬜ Todo | - |
| #124 | Add import from RomVault/ClrMame | ⬜ Todo | - |

---

## Issue Labels

### Priority
- `priority-critical` - Blocking issues
- `priority-high` - Important for current phase
- `priority-medium` - Important but not urgent
- `priority-low` - Nice to have

### Type
- `epic` - Parent epic issue
- `feature` - New feature
- `bug` - Bug fix
- `enhancement` - Improvement to existing feature
- `documentation` - Documentation only
- `refactor` - Code refactoring
- `test` - Test coverage

### Component
- `domain` - Domain models
- `services` - Business logic
- `infrastructure` - External concerns
- `api` - REST API
- `ui` - Frontend
- `cli` - Command line interface
- `database` - Database related
- `performance` - Performance optimization
- `caching` - Cache related

### Status
- `needs-triage` - Needs review
- `blocked` - Waiting on something
- `in-progress` - Being worked on
- `ready-for-review` - PR ready

---

## Epic #8: Large Dataset Handling

**Goal:** Handle DAT files with 60K+ entries, 4K+ DAT file collections, and ROM files ranging from KB to GB

**Labels:** `epic`, `performance`, `priority-high`

**Status:** 🟡 In Progress (30%)

**Reference Data:**
- TOSEC Pack: 4,743 DAT files, ~100MB compressed
- Largest single DAT: 61,454 entries (18MB XML)
- File sizes: KB (NES ROMs) to GB (disc images)

### Sub-Epic #8.1: SignalR Streaming

**Parent:** #8 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #300 | Add SignalR streaming event types | ✅ Done | #8.1 |
| #301 | Enhance useSignalR hook with streaming | ✅ Done | #8.1 |
| #302 | Add large file hashing progress events | ⬜ Todo | #8.1 |
| #303 | Implement cache invalidation events | ⬜ Todo | #8.1 |
| #304 | Add connection state recovery | ⬜ Todo | #8.1 |

### Sub-Epic #8.2: Virtual Data Tables

**Parent:** #8 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #310 | Add react-window for virtualization | ✅ Done | #8.2 |
| #311 | Create VirtualTable component | ✅ Done | #8.2 |
| #312 | Implement infinite scroll loading | ✅ Done | #8.2 |
| #313 | Add cursor-based pagination support | ⬜ Todo | #8.2 |
| #314 | Optimize row rendering performance | ⬜ Todo | #8.2 |

### Sub-Epic #8.3: Client-Side Caching

**Parent:** #8 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #320 | Create LRU cache store | ✅ Done | #8.3 |
| #321 | Implement memory monitoring | ✅ Done | #8.3 |
| #322 | Add TTL-based expiration | ✅ Done | #8.3 |
| #323 | Implement cache invalidation handlers | ⬜ Todo | #8.3 |
| #324 | Add visibility-based cache cleanup | ⬜ Todo | #8.3 |

### Sub-Epic #8.4: Server-Side Streaming

**Parent:** #8 | **Status:** ⬜ Not Started

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #330 | Add streaming XML parser | ⬜ Todo | #8.4 |
| #331 | Implement batch database inserts | ⬜ Todo | #8.4 |
| #332 | Add cursor-based API endpoints | ⬜ Todo | #8.4 |
| #333 | Implement chunked file hashing | ⬜ Todo | #8.4 |

### Sub-Epic #8.5: DAT Hierarchy

**Parent:** #8 | **Status:** ⬜ Not Started

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #340 | Design DAT hierarchy data model | ⬜ Todo | #8.5 |
| #341 | Create TreeView component | ⬜ Todo | #8.5 |
| #342 | Implement lazy branch loading | ⬜ Todo | #8.5 |
| #343 | Add hierarchy stats aggregation | ⬜ Todo | #8.5 |

### Sub-Epic #8.6: Progress Display

**Parent:** #8 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #350 | Create OperationProgress component | ✅ Done | #8.6 |
| #351 | Add multi-stage progress support | ✅ Done | #8.6 |
| #352 | Implement progress streaming | ✅ Done | #8.6 |

---

## Epic #9: Backend Rebuild

**Goal:** Rebuild backend using modern .NET 10/C# 14 with clean architecture, optimized for Plex-like local server operation

**Labels:** `epic`, `backend`, `architecture`, `priority-critical`

**Status:** 🟡 In Progress (75%)

**Reference:** See [backend-rebuild.md](../plans/backend-rebuild.md) for detailed architecture

### Sub-Epic #9.1: Domain Layer

**Parent:** #9 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #400 | Create new Subrom.Domain project | ✅ Done | #9.1 |
| #401 | Design DAT file aggregate root | ✅ Done | #9.1 |
| #402 | Design Game/ROM aggregate with region | ✅ Done | #9.1 |
| #403 | Create Hash value objects (Crc, Md5, Sha1) | ✅ Done | #9.1 |
| #404 | Design Drive entity with offline support | ✅ Done | #9.1 |
| #405 | Create ScanJob entity with progress | ✅ Done | #9.1 |
| #406 | Design Settings configuration entity | ⬜ Todo | #9.1 |
| #407 | Add domain events for SignalR | ✅ Done | #9.1 |
| #408 | Create domain validation rules | ⬜ Todo | #9.1 |

### Sub-Epic #9.2: Application Layer

**Parent:** #9 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #410 | Create new Subrom.Application project | ✅ Done | #9.2 |
| #411 | Implement DatFileService | ✅ Done | #9.2 |
| #412 | Implement ScanService with channels | ✅ Done | #9.2 |
| #413 | Implement HashService with parallel ops | ✅ Done | #9.2 |
| #414 | Implement VerificationService | ✅ Done | #9.2 |
| #415 | Implement DriveService with monitoring | ✅ Done | #9.2 |
| #416 | Create MediatR command/query handlers | ⬜ Todo | #9.2 |
| #417 | Add FluentValidation validators | ⬜ Todo | #9.2 |
| #418 | Implement Mapperly mappers | ⬜ Todo | #9.2 |

### Sub-Epic #9.3: Infrastructure Layer

**Parent:** #9 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #420 | Create new Subrom.Infrastructure project | ✅ Done | #9.3 |
| #421 | Implement EF Core DbContext with SQLite | ✅ Done | #9.3 |
| #422 | Add SQLite optimizations (WAL, mmap) | ✅ Done | #9.3 |
| #423 | Create entity configurations | ✅ Done | #9.3 |
| #424 | Implement repository pattern | ✅ Done | #9.3 |
| #425 | Add XML/ClrMamePro DAT parsers | ✅ Done | #9.3 |
| #426 | Implement streaming XML parser | ⬜ Todo | #9.3 |
| #427 | Create file system abstraction | ⬜ Todo | #9.3 |
| #428 | Add 7-Zip compression support | ✅ Done | #9.3 |

### Sub-Epic #9.4: Web API Layer

**Parent:** #9 | **Status:** ✅ Complete

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #430 | Create new Subrom.Server project | ✅ Done | #9.4 |
| #431 | Configure minimal API endpoints | ✅ Done | #9.4 |
| #432 | Add Scalar/OpenAPI documentation | ✅ Done | #9.4 |
| #433 | Implement SignalR SubromHub | ✅ Done | #9.4 |
| #434 | Add CORS for localhost dev | ✅ Done | #9.4 |
| #435 | Configure Serilog structured logging | ✅ Done | #9.4 |
| #436 | Add health check endpoint | ✅ Done | #9.4 |
| #437 | Implement static file serving for UI | ✅ Done | #9.4 |
| #438 | Add global exception handling | ⬜ Todo | #9.4 |

### Sub-Epic #9.5: Testing

**Parent:** #9 | **Status:** ⚪ Not Started

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #440 | Create Subrom.Tests.Unit project | ⬜ Todo | #9.5 |
| #441 | Create Subrom.Tests.Integration project | ⬜ Todo | #9.5 |
| #442 | Add domain model unit tests | ⬜ Todo | #9.5 |
| #443 | Add service layer unit tests | ⬜ Todo | #9.5 |
| #444 | Add API integration tests | ⬜ Todo | #9.5 |
| #445 | Add DAT parser tests with sample files | ⬜ Todo | #9.5 |

---

## Epic #10: System Tray & Windows Service

**Goal:** Implement Plex-like system tray application and optional Windows Service for background operation

**Labels:** `epic`, `desktop`, `windows`, `priority-high`

**Status:** ⚪ Not Started

**Reference:** See [plex-like-architecture.md](../plans/plex-like-architecture.md) for detailed design

### Sub-Epic #10.1: System Tray Application

**Parent:** #10 | **Status:** ⚪ Not Started

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #500 | Create Subrom.Tray Windows Forms project | ⬜ Todo | #10.1 |
| #501 | Implement NotifyIcon with context menu | ⬜ Todo | #10.1 |
| #502 | Add server process management | ⬜ Todo | #10.1 |
| #503 | Implement icon state indicators | ⬜ Todo | #10.1 |
| #504 | Add notification support | ⬜ Todo | #10.1 |
| #505 | Create quick actions menu | ⬜ Todo | #10.1 |
| #506 | Implement "Open in Browser" action | ⬜ Todo | #10.1 |
| #507 | Add single-instance enforcement | ⬜ Todo | #10.1 |

### Sub-Epic #10.2: Windows Service

**Parent:** #10 | **Status:** ⚪ Not Started

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #510 | Create Subrom.Service project | ⬜ Todo | #10.2 |
| #511 | Implement WindowsService hosting | ⬜ Todo | #10.2 |
| #512 | Add service installer | ⬜ Todo | #10.2 |
| #513 | Configure service recovery options | ⬜ Todo | #10.2 |
| #514 | Add service control from tray app | ⬜ Todo | #10.2 |

### Sub-Epic #10.3: Settings & Configuration

**Parent:** #10 | **Status:** ⚪ Not Started

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #520 | Design settings.json schema | ⬜ Todo | #10.3 |
| #521 | Implement IOptions configuration | ⬜ Todo | #10.3 |
| #522 | Add settings persistence | ⬜ Todo | #10.3 |
| #523 | Create settings dialog in tray app | ⬜ Todo | #10.3 |
| #524 | Implement startup registration | ⬜ Todo | #10.3 |

### Sub-Epic #10.4: Logging & Diagnostics

**Parent:** #10 | **Status:** ⚪ Not Started

| # | Title | Status | Parent |
|---|-------|--------|--------|
| #530 | Configure Serilog file rolling | ⬜ Todo | #10.4 |
| #531 | Add log viewer dialog | ⬜ Todo | #10.4 |
| #532 | Implement crash reporting | ⬜ Todo | #10.4 |
| #533 | Add diagnostic endpoint | ⬜ Todo | #10.4 |

---

## Commit Message Convention

All commits should reference an issue:

```
feat(#13): implement XML DAT parser

- Add XmlDatParser class
- Support streaming parsing
- Handle nested elements

Closes #13
```

**Format:** `type(#issue): description`

**Types:**
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `style` - Formatting
- `refactor` - Refactoring
- `test` - Tests
- `chore` - Maintenance
