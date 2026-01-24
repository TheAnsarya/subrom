# ROM Set Providers

## Overview

This document catalogs the major ROM set providers and their DAT file distribution methods.

## Major Providers

### No-Intro

**Focus:** Cartridge-based consoles, clean dumps
**Website:** https://www.no-intro.org / https://datomatic.no-intro.org
**Philosophy:** One verified dump per game, no overdumps or hacks

**DAT Distribution:**
- DAT-o-MATIC website (registration required)
- Daily packs available
- Individual system DATs
- Parent/Clone DATs available

**Covered Systems:**
- Nintendo: NES, SNES, N64, GB, GBC, GBA, DS, 3DS
- Sega: Master System, Genesis, Game Gear, 32X, Saturn
- Atari: 2600, 5200, 7800, Jaguar, Lynx
- Sony: PSP, PS Vita
- And many more...

**Naming Convention:**
```
Game Name (Region) (Version) (Additional Info)
Super Mario Bros. (USA)
Legend of Zelda, The (USA) (Rev 1)
```

**Integration Notes:**
- Requires registration for DAT downloads
- API not publicly documented
- Consider web scraping with rate limiting
- Update frequency: Daily

---

### TOSEC (The Old School Emulation Center)

**Focus:** All platforms, comprehensive coverage including demos, magazines
**Website:** https://www.tosecdev.org
**Philosophy:** Preserve everything, including variants and hacks

**DAT Distribution:**
- Public FTP/HTTP downloads
- Complete packs available
- Categories: Games, Applications, Demos, Magazines, etc.

**Covered Systems:**
- All retro platforms
- Computer systems (Amiga, C64, DOS, etc.)
- Magazines and coverdiscs
- Demos and intros

**Naming Convention:**
```
Name (Year)(Publisher)(System)(Country)(Language)(Flags)
Super Mario Bros. (1985)(Nintendo)(NES)(US)[!]
```

**Integration Notes:**
- Direct download available
- Well-structured FTP site
- Update frequency: Varies by system

---

### Redump

**Focus:** Optical media (CD/DVD/Blu-ray)
**Website:** http://redump.org
**Philosophy:** Bit-perfect dumps with verification

**DAT Distribution:**
- Website downloads (registration for some)
- CHD (Compressed Hunks of Data) support
- Cuesheets included

**Covered Systems:**
- Sony: PlayStation, PS2, PS3, PSP
- Sega: Saturn, Dreamcast, CD
- Nintendo: GameCube, Wii
- Microsoft: Xbox, Xbox 360
- PC: CD-ROM games

**Naming Convention:**
```
Game Name (Region) (Languages) (Version)
Final Fantasy VII (USA) (Disc 1)
```

**Integration Notes:**
- Registration required for disc images
- DATs freely available
- Large file sizes (ISOs, CHDs)

---

### GoodSets (Legacy)

**Focus:** Historical comprehensive sets
**Status:** No longer maintained
**Philosophy:** Collect everything, mark quality

**Sets:**
- GoodNES, GoodSNES, GoodGEN, GoodGB, etc.
- Last updates circa 2016

**Naming Convention:**
```
Game Name (Region) [Flags]
Super Mario Bros. (U) [!]
```

**Flags:**
- `[!]` - Verified good
- `[a]` - Alternate
- `[b]` - Bad dump
- `[o]` - Overdump
- `[h]` - Hack
- `[p]` - Pirate
- `[t]` - Trained
- `[f]` - Fixed

**Integration Notes:**
- Legacy support only
- DATs still circulating
- No updates expected

---

### MAME

**Focus:** Arcade games
**Website:** https://www.mamedev.org
**Philosophy:** Accurate emulation and preservation

**DAT Distribution:**
- Included with MAME releases
- XML format
- Software lists for home computers

**Naming Convention:**
```
Short name (description)
dkong (Donkey Kong (US set 1))
```

**Features:**
- Parent/clone relationships
- BIOS requirements
- Device dependencies
- CHD support for hard drives/laserdiscs

**Integration Notes:**
- Released with each MAME version
- Large DAT file (~200MB XML)
- Complex parent/clone structure

---

### Other Notable Sources

#### Pleasuredome
- Curated MAME sets
- Strict verification
- Forum-based distribution

#### Archive.org
- Historical preservation
- Various collections
- Public domain focus

#### ROM Depot / RomCenter
- Community databases
- Various quality levels

---

## DAT Collection Strategy

### Priority Order
1. **No-Intro** - Primary for cartridge systems (highest quality)
2. **Redump** - Primary for disc systems
3. **MAME** - Primary for arcade
4. **TOSEC** - Secondary/comprehensive coverage
5. **GoodSets** - Legacy fallback only

### Update Schedule

| Provider | Recommended Interval |
|----------|---------------------|
| No-Intro | Daily/Weekly |
| TOSEC | Monthly |
| Redump | Weekly |
| MAME | Per release (~monthly) |
| GoodSets | One-time import |

### Storage Requirements

| Provider | Estimated Size | Systems |
|----------|---------------|---------|
| No-Intro | ~50MB total | 100+ |
| TOSEC | ~500MB total | 200+ |
| Redump | ~100MB total | 30+ |
| MAME | ~200MB | 1 (many variants) |

---

## Implementation: Provider Service

```csharp
public interface IDatProvider {
	string Name { get; }
	Task<IEnumerable<DatInfo>> GetAvailableDatsAsync();
	Task<Stream> DownloadDatAsync(string datId);
	Task<DateTime?> GetLastUpdateAsync(string datId);
}

public class NoIntroProvider : IDatProvider {
	public string Name => "No-Intro";
	
	// Implementation details...
}

public class TosecProvider : IDatProvider {
	public string Name => "TOSEC";
	
	// Implementation details...
}

public class DatProviderService {
	private readonly Dictionary<string, IDatProvider> _providers;
	
	public async Task UpdateAllDatsAsync() {
	    foreach (var provider in _providers.Values) {
	        var dats = await provider.GetAvailableDatsAsync();
	        foreach (var dat in dats) {
	            if (await NeedsUpdateAsync(dat)) {
	                await DownloadAndImportAsync(provider, dat);
	            }
	        }
	    }
	}
}
```

---

## Legal Considerations

- DAT files themselves are metadata, generally legal to distribute
- Actual ROM files have copyright restrictions
- This tool manages files, does not distribute ROMs
- Users responsible for their own ROM collections
- Respect provider terms of service

---

## References

- [No-Intro Wiki](https://wiki.no-intro.org)
- [TOSEC Naming Convention](https://www.tosecdev.org/tosec-naming-convention)
- [Redump Wiki](http://wiki.redump.org)
- [MAME Documentation](https://docs.mamedev.org)
