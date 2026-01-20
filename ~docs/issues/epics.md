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
| #6 | Web UI Rebuild | � Near Complete | 25 | 92% |
| #7 | Advanced Features | ⚪ Not Started | 15 | 0% |

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

### Status
- `needs-triage` - Needs review
- `blocked` - Waiting on something
- `in-progress` - Being worked on
- `ready-for-review` - PR ready

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
