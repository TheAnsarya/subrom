# Subrom 1.0.0 Release Plan

**Date Created:** January 22, 2026
**Target Release:** Q1 2026
**Status:** 📋 Planning

## Executive Summary

Subrom 1.0.0 will be the first stable release of the ROM management toolkit. This document defines the Minimum Viable Product (MVP) feature set, identifies blockers, and provides a roadmap to release.

## 1.0.0 Vision

A functional ROM management tool that can:
1. **Import** DAT files from major providers (No-Intro, TOSEC, Redump)
2. **Scan** ROM collections on local and network drives
3. **Verify** ROMs against DAT files with accurate hash matching
4. **Organize** ROM collections using customizable folder templates
5. **Report** collection status (verified, missing, duplicates, bad dumps)

---

## MVP Feature Set

### ✅ Complete (Ready for 1.0.0)

| Feature | Component | Tests | Status |
|---------|-----------|-------|--------|
| Logiqx XML DAT parsing | LogiqxDatParser | ✅ | Ready |
| ClrMamePro DAT parsing | ClrMameProDatParser | ✅ 21 tests | Ready |
| Streaming XML parsing | StreamingLogiqxParser | ✅ | Ready |
| DAT import/export | DatFileService | ✅ | Ready |
| DAT category browser | DatFileEndpoints | ✅ | Ready |
| Drive registration | DriveService | ✅ 20 tests | Ready |
| Drive online/offline | Drive entity | ✅ | Ready |
| File scanning | ScanService | ✅ 14 tests | Ready |
| Scan job execution | ScanJobProcessor | ✅ | Ready |
| Scan resumability | ScanResumeService | ✅ | Ready |
| Archive support (ZIP, 7z, RAR) | SharpCompressArchiveService | ✅ | Ready |
| Hash computation (CRC, MD5, SHA1) | HashService | ✅ | Ready |
| Header detection | RomHeaderService | ✅ | Ready |
| ROM verification | VerificationService | ✅ 11 tests | Ready |
| Verification endpoints | VerificationEndpoints | ✅ | Ready |
| Duplicate detection | DuplicateDetectionService | ✅ | Ready |
| Duplicate endpoints | RomFileEndpoints | ✅ | Ready |
| Bad dump detection | BadDumpService | ✅ | Ready |
| Bad dump endpoints | RomFileEndpoints | ✅ | Ready |
| 1G1R filtering | OneGameOneRomService | ✅ | Ready |
| 1G1R endpoints | RomFileEndpoints | ✅ | Ready |
| Parent/clone detection | ParentCloneService | ✅ | Ready |
| Parent/clone endpoints | RomFileEndpoints | ✅ | Ready |
| Organization templates | OrganizationTemplate | ✅ | Ready |
| Organization service | OrganizationService | ✅ 17 tests | Ready |
| Organization endpoints | OrganizationEndpoints | ✅ | Ready |
| Organization logging | OrganizationOperationLog | ✅ | Ready |
| Storage monitoring | StorageMonitorService | ✅ | Ready |
| Storage endpoints | StorageEndpoints | ✅ | Ready |
| SignalR real-time updates | SubromHub | ✅ | Ready |
| Web UI Dashboard | React Dashboard | ✅ | Ready |
| Web UI DAT Manager | React DatManager | ✅ | Ready |
| Web UI ROM Browser | React RomBrowser | ✅ | Ready |
| Web UI Verification | React Verification | ✅ | Ready |
| Web UI Settings | React Settings | ✅ | Ready |
| System Tray App | Subrom.Tray | ✅ | Ready |
| Windows Service | Subrom.Service | ✅ | Ready |

### ⚠️ In Progress (Needed for 1.0.0)

| Feature | Component | Blocker | Priority |
|---------|-----------|---------|----------|
| Settings persistence | SettingsService | ✅ RESOLVED | HIGH |
| Global error handling | ExceptionMiddleware | ✅ RESOLVED | MEDIUM |
| API documentation | Scalar/OpenAPI | Needs review | LOW |

### ❌ Deferred (Post 1.0.0)

| Feature | Reason | Target Version |
|---------|--------|----------------|
| DAT auto-sync from providers | Requires auth/scraping work | 1.1.0 |
| Memory-mapped file hashing | Optimization | 1.1.0 |
| Database vacuum scheduling | Optimization | 1.1.0 |
| Integration tests | Non-blocking | 1.1.0 |
| Plugin system | Advanced feature | 2.0.0 |
| RetroArch playlist generation | Advanced feature | 1.2.0 |
| Box art scraping | Advanced feature | 2.0.0 |

---

## Blockers & Critical Issues

### 🔴 Critical (Must Fix)

| Issue | Description | Effort | Status |
|-------|-------------|--------|--------|
| Settings entity | Persistent settings across sessions | 4h | ✅ Done |
| Error handling | Global exception handler | 2h | ✅ Done |

### 🟡 High Priority (Should Fix)

| Issue | Description | Effort | Status |
|-------|-------------|--------|--------|
| Domain validation | Validation rules incomplete | 4h | ⬜ Todo |
| Health check | Basic health endpoint exists but needs expansion | 2h | ⬜ Todo |

### 🟢 Low Priority (Nice to Have)

| Issue | Description | Effort | Status |
|-------|-------------|--------|--------|
| Responsive design | UI breakpoints incomplete | 4h | ⬜ Todo |
| Loading skeletons | UI polish | 2h | ⬜ Todo |
| Keyboard shortcuts | Accessibility | 3h | ⬜ Todo |

---

## Test Coverage Requirements

**Current:** 359 unit tests passing ✅

**Target for 1.0.0:** 350+ ✅ ACHIEVED

| Category | Current | Target | Status |
|----------|---------|--------|--------|
| Domain | 57 | 50 | ✅ |
| Application Services | 97 | 90 | ✅ |
| Infrastructure | 100 | 110 | ✅ |
| Parsers | 21 | 25 | ✅ |
| **Total** | **359** | **350** | ✅ |

---

## Release Checklist

### Pre-Release

- [ ] All critical blockers resolved
- [ ] Test coverage meets target (350+ tests)
- [ ] All warnings resolved (currently 0)
- [ ] Build succeeds on clean checkout
- [ ] Documentation reviewed and updated
- [ ] README.md has clear installation instructions
- [ ] CHANGELOG.md created with 1.0.0 entries
- [ ] License file present and correct

### Release Artifacts

- [ ] Windows x64 self-contained executable
- [ ] Windows installer (MSI or NSIS)
- [ ] Docker image (optional for 1.0.0)
- [ ] Source code archive

### Post-Release

- [ ] GitHub release created with notes
- [ ] Tag v1.0.0 created
- [ ] Documentation site updated (if applicable)
- [ ] Community announcement (Reddit, forums)

---

## Timeline

| Week | Tasks | Milestone |
|------|-------|-----------|
| Week 1 | Settings persistence, error handling | Core blockers fixed |
| Week 2 | Additional tests, documentation | Test coverage met |
| Week 3 | Release build testing, installer | Artifacts ready |
| Week 4 | Final QA, bug fixes | 1.0.0 Release |

---

## Success Criteria

1. **Functionality:** All MVP features working end-to-end
2. **Stability:** No crashes during normal operation
3. **Performance:** Scan 10K files in < 60 seconds
4. **Accuracy:** 100% hash verification accuracy
5. **Usability:** New user can import DAT, scan, verify in < 10 minutes

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Settings persistence issues | HIGH | LOW | Use proven config pattern |
| Large collection performance | MEDIUM | MEDIUM | Virtual tables, pagination |
| Archive extraction failures | LOW | LOW | SharpCompress well-tested |
| Database corruption | HIGH | LOW | SQLite WAL mode, backups |

---

## Related Documents

- [Architecture Overview](current-architecture.md)
- [API Reference](../api-reference.md)
- [Epic Tracking](../issues/epics.md)
- [Base Features Analysis](base-features-analysis.md)

---

## Appendix: Feature Parity Comparison

| Feature | RomVault | ClrMame Pro | Subrom 1.0.0 |
|---------|----------|-------------|--------------|
| DAT Import | ✅ | ✅ | ✅ |
| Multi-format DAT | ✅ | ✅ | ✅ |
| ROM Scanning | ✅ | ✅ | ✅ |
| Hash Verification | ✅ | ✅ | ✅ |
| Archive Support | ✅ | ✅ | ✅ |
| 1G1R Filtering | ✅ | ✅ | ✅ |
| Organization | ✅ | ✅ | ✅ |
| Web UI | ❌ | ❌ | ✅ |
| Real-time Progress | ❌ | ❌ | ✅ |
| Multi-drive Support | ✅ | ❌ | ✅ |
| Offline Drives | ❌ | ❌ | ✅ |
| Network Drives | ❌ | ❌ | ✅ |
