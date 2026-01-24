# DAT File Format Documentation

## Overview

DAT files are XML or plain-text files that describe ROM sets. They contain metadata about games and their associated ROM files, including hash values for verification.

## Supported Formats

### 1. XML DAT (LogiqX/No-Intro Format)

The most common modern format, used by No-Intro, Redump, and others.

**Structure:**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE datafile PUBLIC "-//Logiqx//DTD ROM Management Datafile//EN" 
	      "http://www.logiqx.com/Dats/datafile.dtd">
<datafile>
	<header>
	    <name>Nintendo - Nintendo Entertainment System</name>
	    <description>Nintendo - Nintendo Entertainment System</description>
	    <version>20260115-123456</version>
	    <author>No-Intro</author>
	    <homepage>https://www.no-intro.org</homepage>
	    <url>https://datomatic.no-intro.org</url>
	</header>
	<game name="Super Mario Bros. (USA)">
	    <description>Super Mario Bros.</description>
	    <rom name="Super Mario Bros. (USA).nes" 
	         size="40976" 
	         crc="d445f698" 
	         md5="811b027eaf99c2def7b933c5208636de" 
	         sha1="facee9c577a5262dbee256de7740d2d87e85f3e0"/>
	</game>
</datafile>
```

**Key Elements:**
- `<header>` - Metadata about the DAT file
- `<game>` or `<machine>` - Individual game entries
- `<rom>` - ROM file with hash values
- `<disk>` - CHD/ISO disc images
- `<sample>` - Audio samples (MAME)
- `<biosset>` - BIOS set definitions

### 2. ClrMame Pro Format

Plain-text format, popular with older tools and some providers.

**Structure:**
```
clrmamepro (
	name "Nintendo - Nintendo Entertainment System"
	description "Nintendo - Nintendo Entertainment System"
	version "20260115"
	author "No-Intro"
)

game (
	name "Super Mario Bros. (USA)"
	description "Super Mario Bros."
	rom ( name "Super Mario Bros. (USA).nes" size 40976 crc d445f698 md5 811b027eaf99c2def7b933c5208636de sha1 facee9c577a5262dbee256de7740d2d87e85f3e0 )
)
```

### 3. MAME DAT Format

Extended format for arcade games with additional metadata.

**Additional Elements:**
- `<machine>` instead of `<game>`
- `cloneof` attribute for clone relationships
- `romof` attribute for BIOS/parent ROMs
- `<device_ref>` for device dependencies
- `<softwarelist>` for software lists

### 4. TOSEC Format

Uses XML with TOSEC-specific naming conventions.

**Naming Convention:**
```
Game Name (Year)(Publisher)(System)(Country)(Language)(More Info)[Flags]
```

**Example:**
```
Super Mario Bros. (1985)(Nintendo)(NES)(USA)[!]
```

**Flags:**
- `[!]` - Verified good dump
- `[a]` - Alternate version
- `[b]` - Bad dump
- `[f]` - Fixed
- `[h]` - Hack
- `[o]` - Overdump
- `[p]` - Pirate
- `[t]` - Trained
- `[cr]` - Cracked

## Hash Algorithms

### CRC32
- 32-bit checksum
- Fast to compute
- Used by most DAT files
- Format: 8 hex characters (e.g., `d445f698`)

### MD5
- 128-bit hash
- Slower than CRC32
- More collision-resistant
- Format: 32 hex characters

### SHA1
- 160-bit hash
- Most secure of the three
- Used by Redump, Trusted dumps
- Format: 40 hex characters

### SHA256
- 256-bit hash
- Used by some modern DATs
- Format: 64 hex characters

## ROM Status Values

| Status | Description |
|--------|-------------|
| `good` | Verified good dump |
| `baddump` | Known bad dump |
| `nodump` | No dump available |
| `verified` | Hash verified against trusted source |

## Parsing Considerations

### Streaming vs DOM Parsing
- Large DAT files (MAME: 200MB+) require streaming
- Small DATs can use DOM parsing for simplicity
- Consider memory constraints

### Encoding
- UTF-8 is standard
- Some older DATs use ISO-8859-1
- Handle BOM (Byte Order Mark) properly

### Entity References
- XML entities must be decoded
- Common: `&amp;`, `&lt;`, `&gt;`, `&quot;`
- Some DATs use numeric entities

### Case Sensitivity
- Hash values should be stored lowercase
- Game names preserve original case
- Comparisons should be case-insensitive for hashes

## Implementation Notes

### C# Parsing Strategy

```csharp
public interface IDatParser {
	Task<Datafile> ParseAsync(Stream stream, CancellationToken ct = default);
	bool CanParse(Stream stream);
}

public class XmlDatParser : IDatParser {
	public async Task<Datafile> ParseAsync(Stream stream, CancellationToken ct) {
	    using var reader = XmlReader.Create(stream, new XmlReaderSettings {
	        Async = true,
	        IgnoreWhitespace = true
	    });
	    
	    // Stream parse for large files
	    while (await reader.ReadAsync()) {
	        if (reader.NodeType == XmlNodeType.Element) {
	            switch (reader.LocalName) {
	                case "header":
	                    ParseHeader(reader);
	                    break;
	                case "game":
	                case "machine":
	                    yield return ParseGame(reader);
	                    break;
	            }
	        }
	    }
	}
}
```

### Database Schema for DAT Storage

```sql
CREATE TABLE DatFiles (
	Id TEXT PRIMARY KEY,
	Name TEXT NOT NULL,
	Description TEXT,
	Version TEXT,
	Author TEXT,
	Provider TEXT,
	FilePath TEXT,
	ImportedAt TEXT,
	GameCount INTEGER,
	RomCount INTEGER
);

CREATE TABLE DatGames (
	Id TEXT PRIMARY KEY,
	DatFileId TEXT NOT NULL,
	Name TEXT NOT NULL,
	Description TEXT,
	Year TEXT,
	Manufacturer TEXT,
	CloneOf TEXT,
	RomOf TEXT,
	FOREIGN KEY (DatFileId) REFERENCES DatFiles(Id)
);

CREATE TABLE DatRoms (
	Id TEXT PRIMARY KEY,
	DatGameId TEXT NOT NULL,
	Name TEXT NOT NULL,
	Size INTEGER,
	Crc32 TEXT,
	Md5 TEXT,
	Sha1 TEXT,
	Status TEXT DEFAULT 'good',
	FOREIGN KEY (DatGameId) REFERENCES DatGames(Id)
);

CREATE INDEX idx_datroms_crc32 ON DatRoms(Crc32);
CREATE INDEX idx_datroms_sha1 ON DatRoms(Sha1);
```

## References

- [LogiqX DTD](http://www.logiqx.com/Dats/datafile.dtd)
- [No-Intro DAT-o-MATIC](https://datomatic.no-intro.org)
- [TOSEC Naming Convention](https://www.tosecdev.org/tosec-naming-convention)
- [Redump.org](http://redump.org)
- [MAME](https://www.mamedev.org)
