# Subrom User Guide

A complete guide to using Subrom for ROM management and verification.

## Table of Contents
- [Getting Started](#getting-started)
- [Adding DAT Files](#adding-dat-files)
- [Scanning ROM Files](#scanning-rom-files)
- [Verifying ROMs](#verifying-roms)
- [Organizing ROMs](#organizing-roms)
- [Exporting Results](#exporting-results)

---

## Getting Started

### First Launch

1. **Open the Web Interface**
   - Navigate to **http://localhost:5678** in your browser
   - The dashboard shows your collection statistics

2. **System Tray (Windows)**
   - Look for the Subrom icon in your system tray
   - Right-click for quick actions:
     - Open Web UI
     - Start/Stop Server
     - View Logs
     - Settings

### Dashboard Overview

The dashboard displays:
- Total DAT files loaded
- Total ROMs cataloged
- Verification statistics (Verified, Missing, Unknown)
- Recent scan activity

---

## Adding DAT Files

DAT files define the expected ROMs for a system. Subrom supports:
- **XML DAT** (Logiqx format) - Used by No-Intro, Redump
- **ClrMamePro DAT** - Used by TOSEC, GoodTools

### Importing DAT Files

1. Go to **DAT Manager** page
2. Click **Import DAT**
3. Select one or more `.dat` or `.xml` files
4. Subrom parses and loads the ROMs

### Supported Sources

| Source | Format | Notes |
|--------|--------|-------|
| No-Intro | XML | Must download manually from datomatic.no-intro.org |
| TOSEC | ClrMamePro | Available from tosec.org |
| Redump | XML | Available from redump.org |
| GoodTools | ClrMamePro | Legacy format |

> **Note:** No-Intro requires manual download - do NOT attempt automated scraping.

---

## Scanning ROM Files

### Adding Scan Locations

1. Go to **Settings** > **Scan Locations**
2. Click **Add Folder** or **Add Drive**
3. Select the folder/drive containing ROMs
4. Configure scan options:
   - **Include subfolders** - Scan recursively
   - **Include archives** - Scan inside ZIP, 7z, RAR files

### Starting a Scan

1. Go to **ROM Files** page
2. Click **Scan** to scan all configured locations
3. Or click the scan icon next to a specific drive/folder

### Scan Progress

During scanning, you'll see:
- Current file being processed
- Files scanned / total files
- Hashes computed
- Estimated time remaining

### Scan Queue

Multiple scans are queued automatically:
- View queue on the **Scan Queue** page
- Pause/resume queue processing
- Change priority of pending scans
- Cancel unwanted scans

---

## Verifying ROMs

### Automatic Verification

ROMs are automatically verified against loaded DAT files when:
- A scan completes
- New DAT files are imported

### Verification Status

| Status | Meaning |
|--------|---------|
| ✅ **Verified** | ROM matches a DAT entry exactly |
| ⚠️ **Unknown** | ROM not found in any DAT file |
| 🔴 **Bad Dump** | ROM matches a known bad dump |
| 📋 **Missing** | DAT entry with no matching ROM |

### Manual Verification

1. Go to **Verification** page
2. Click **Verify All** or select specific files
3. View results by:
   - System/DAT file
   - Verification status
   - Drive/folder

---

## Organizing ROMs

### 1G1R (One Game, One ROM)

Create a curated set with one ROM per game:

1. Go to **Organization** > **1G1R**
2. Select your DAT file
3. Configure region priority:
   - Drag regions to reorder (e.g., USA > Europe > Japan)
4. Configure language priority
5. Click **Generate 1G1R Set**

### Parent/Clone Organization

Group related ROMs (revisions, regions):

1. Go to **Organization** > **Parent/Clone**
2. Select your DAT file
3. View parent-clone relationships
4. Export or organize by parent

### Duplicate Detection

Find duplicate ROMs across your collection:

1. Go to **Organization** > **Duplicates**
2. Scan for duplicates by:
   - Exact hash match
   - Same game, different format
3. Review and optionally delete duplicates

---

## Exporting Results

### Export Formats

- **CSV** - Spreadsheet-compatible
- **JSON** - Machine-readable

### Export Options

1. Go to **Export** page
2. Choose what to export:
   - All ROMs
   - Verified ROMs only
   - Unknown ROMs only
   - Bad dumps only
   - By specific drive/folder
3. Click **Export**
4. Download the file

### Collection Summary

Export a summary with:
- Total files
- Verification breakdown
- Per-system statistics

---

## Tips & Best Practices

### Organizing Your Collection

1. **One drive per system** - Easier to manage
2. **Use consistent naming** - Helps with matching
3. **Keep originals** - Organize copies, not originals

### Performance Tips

1. **SSD is faster** - Hash computation benefits from fast I/O
2. **Scan overnight** - Large collections take time
3. **Use incremental scans** - Only scan changed files

### Backup

- Export your verification results periodically
- The database is at: `{data-dir}/subrom.db`

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+D` | Go to Dashboard |
| `Ctrl+M` | Go to DAT Manager |
| `Ctrl+R` | Go to ROM Files |
| `Ctrl+S` | Start Scan |
| `Esc` | Close dialog |

---

## Getting Help

- **Issues:** [GitHub Issues](https://github.com/TheAnsarya/subrom/issues)
- **Documentation:** [docs/](index.md)
- **API:** [API Reference](api-reference.md)
