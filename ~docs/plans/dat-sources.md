# ROM DAT Sources and Collection Strategy

## Overview

This document catalogs all known ROM/ISO DAT file sources for the Subrom project, their formats, access methods, and collection strategies.

## Major DAT Providers

### 1. No-Intro

**Website:** https://no-intro.org/ | https://datomatic.no-intro.org/

**Coverage:** Cartridge-based systems (Nintendo, Sega, Atari, etc.)

**Format:** XML (Logiqx format)

**Quality:** ⭐⭐⭐⭐⭐ Gold standard for cartridge ROMs

> ⛔ **CRITICAL WARNING: DO NOT SCRAPE**
> 
> We were **INSTANTLY IP BANNED** from datomatic.no-intro.org when we attempted
> automated scraping/downloading. The NoIntroProvider is DISABLED for downloads.
> 
> **Alternatives needed:**
> - Manual download by users (requires free registration)
> - Authenticated API access (investigate DAT-o-Matic API)
> - Community DAT pack distributions
> - Contact shippa6@hotmail.com to discuss lifting ban

**Access Method:**
- ~~Automated download~~ **BANNED**
- Manual download via Datomatic (requires registration)
- Daily updated packs
- XML API: `https://datomatic.no-intro.org/stuff/datinfo_xml.php` (requires auth)

**Systems Covered (60+):**
- Nintendo: NES, SNES, N64, GC, Wii, GB, GBC, GBA, DS, 3DS
- Sega: SMS, MD/Genesis, Saturn, Dreamcast, GG
- Sony: PS1, PS2, PSP
- Atari: 2600, 5200, 7800, Lynx, Jaguar
- NEC: PC Engine/TurboGrafx-16
- SNK: Neo Geo Pocket/Color
- Bandai: WonderSwan/Color
- And many more...

### 2. Redump

**Website:** http://redump.org/

**Coverage:** Optical disc systems (CD, DVD, Blu-ray, GD-ROM)

**Format:** XML (Logiqx format) + CUE sheets

**Quality:** ⭐⭐⭐⭐⭐ Gold standard for disc images

**Access Method:**
- Direct download from Redump servers
- Daily updated
- API: `http://redump.org/datfile/`

**Systems Covered (40+):**
- PlayStation 1, 2, 3, 4
- Xbox, Xbox 360, Xbox One
- Sega Saturn, Dreamcast, CD, Mega-CD
- Nintendo GameCube, Wii, Wii U
- PC (IBM PC compatible)
- Audio CDs, Video DVDs

### 3. TOSEC (The Old School Emulation Center)

**Website:** https://www.tosecdev.org/

**Coverage:** Comprehensive - computers, consoles, calculators, everything

**Format:** XML (Logiqx format) + TXT

**Quality:** ⭐⭐⭐⭐ Very comprehensive, includes non-game software

**Access Method:**
- GitHub releases: https://github.com/tosec-dev/tosec
- Manual download from website

**Systems Covered (200+):**
- All major consoles
- Home computers (Amiga, Atari ST, C64, ZX Spectrum, etc.)
- Calculators (TI, Casio)
- Arcade machines
- V.Smile, LeapPad, and educational systems

### 4. MAME (Multiple Arcade Machine Emulator)

**Website:** https://www.mamedev.org/

**Coverage:** Arcade machines

**Format:** XML (custom MAME format, convertible to Logiqx)

**Quality:** ⭐⭐⭐⭐⭐ Definitive arcade ROM catalog

**Access Method:**
- Generated from MAME executable: `mame -listxml > mame.xml`
- Also available via third-party: https://www.progettosnaps.net/dats/

**Systems Covered:**
- 40,000+ arcade games and machines
- Pinball machines
- Slot machines
- Electromechanical games

### 5. MESS (Multi Emulator Super System)

**Website:** Merged into MAME (2015)

**Coverage:** Home computers and consoles (now part of MAME)

**Format:** XML (MAME format)

**Quality:** ⭐⭐⭐⭐

**Access Method:**
- Same as MAME: `mame -listxml > mess.xml`
- Filter for non-arcade systems

### 6. GoodTools

**Website:** https://github.com/frederic-marti/Good-ROM-Sets (archived)

**Coverage:** Legacy tool, various consoles

**Format:** Custom text format

**Quality:** ⭐⭐⭐ Outdated but historically significant

**Status:** ⚠️ No longer maintained (last update ~2016)

**Access Method:**
- Manual download of legacy GoodSets
- Convert to XML for Subrom compatibility

**Systems:**
- GoodNES, GoodSNES, GoodGen, GoodGBA, etc.

### 7. Other Notable Sources

#### libretro-database
**URL:** https://github.com/libretro/libretro-database
- RetroArch DAT files
- RDB format (custom binary)
- Covers 50+ systems

#### ScreenScraper
**URL:** https://www.screenscraper.fr/
- French ROM database
- Requires API key
- JSON format

#### OpenVGDB
**URL:** https://github.com/OpenVGDB/OpenVGDB
- Open Video Game Database
- SQLite format
- Good for metadata enrichment

## DAT File Formats

### Logiqx XML (Industry Standard)
```xml
<?xml version="1.0"?>
<!DOCTYPE datafile PUBLIC "-//Logiqx//DTD ROM Management Datafile//EN" "http://www.logiqx.com/Dats/datafile.dtd">
<datafile>
	<header>
		<name>Nintendo - Game Boy</name>
		<description>Nintendo - Game Boy</description>
		<version>20240115-120000</version>
		<author>No-Intro</author>
	</header>
	<game name="Game Name (USA)">
		<description>Game Name (USA)</description>
		<rom name="game.gb" size="262144" crc="12345678" md5="..." sha1="..." />
	</game>
</datafile>
```

### ClrMamePro (Text Format)
```
clrmamepro (
	name "Nintendo - Game Boy"
	description "Nintendo - Game Boy"
	version 20240115-120000
	author "No-Intro"
)

game (
	name "Game Name (USA)"
	description "Game Name (USA)"
	rom ( name "game.gb" size 262144 crc 12345678 md5 ... sha1 ... )
)
```

### MAME XML (Custom Format)
```xml
<mame>
	<machine name="gamename">
		<description>Game Name</description>
		<rom name="game.bin" size="262144" crc="12345678" sha1="..." />
	</machine>
</mame>
```

## Collection Strategy for Subrom

### Phase 1: Core Providers (Implemented)
- ✅ No-Intro (Logiqx XML parser implemented)
- ⬜ Redump (same format, reuse parser)
- ⬜ TOSEC (same format, reuse parser)

### Phase 2: Extended Support
- ⬜ MAME/MESS (custom XML parser needed)
- ⬜ libretro-database (RDB parser)

### Phase 3: Metadata Enrichment
- ⬜ ScreenScraper API integration
- ⬜ OpenVGDB for additional metadata

## Automated DAT Synchronization

### Priority 1: No-Intro
**Frequency:** Daily  
**Method:** Poll Datomatic API for updates  
**Storage:** `C:\~reference-roms\dats\nointro\`

### Priority 2: Redump
**Frequency:** Daily  
**Method:** HTTP download from Redump.org  
**Storage:** `C:\~reference-roms\dats\redump\`

### Priority 3: TOSEC
**Frequency:** Weekly  
**Method:** GitHub releases API  
**Storage:** `C:\~reference-roms\dats\tosec\`

### Priority 4: MAME
**Frequency:** Monthly (with MAME releases)  
**Method:** Generate from MAME executable  
**Storage:** `C:\~reference-roms\dats\mame\`

## Implementation Plan

### Backend Services
1. **DatCollectionService** - Coordinates all DAT providers
2. **NoIntroProvider** - Datomatic API integration
3. **RedumpProvider** - Redump.org download
4. **TosecProvider** - GitHub releases
5. **MameProvider** - MAME XML generation

### Scheduled Jobs
- **DailyDatSync** - No-Intro, Redump (4:00 AM)
- **WeeklyDatSync** - TOSEC (Sunday 2:00 AM)
- **MonthlyDatSync** - MAME (1st of month, 3:00 AM)

### Database Schema
```sql
CREATE TABLE DatSources (
	Id INTEGER PRIMARY KEY,
	Provider TEXT NOT NULL, -- NoIntro, Redump, TOSEC, MAME
	SystemName TEXT NOT NULL,
	LastSyncedAt DATETIME,
	Version TEXT,
	FileCount INTEGER,
	IsEnabled BOOLEAN DEFAULT 1
);
```

## Complete System Coverage Estimate

| Provider | Systems | DAT Files | Total Games |
|----------|---------|-----------|-------------|
| No-Intro | 60+ | ~60 | ~100,000 |
| Redump | 40+ | ~40 | ~50,000 |
| TOSEC | 200+ | ~500 | ~200,000 |
| MAME | 1 | 1 | ~40,000 |
| **TOTAL** | **300+** | **~600** | **~390,000** |

## Storage Requirements

- DAT XML files: ~500MB (compressed)
- Parsed database: ~2GB (SQLite with indexes)
- Full implementation with all DATs: ~3GB total

## Next Steps

1. ✅ Create No-Intro download script
2. ⬜ Implement DatCollectionService
3. ⬜ Add Redump provider
4. ⬜ Add TOSEC provider
5. ⬜ Add MAME provider
6. ⬜ Create scheduled sync jobs
7. ⬜ Build admin UI for DAT management
