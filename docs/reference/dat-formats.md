# Supported DAT File Formats

Subrom supports the most common DAT file formats used by ROM cataloging groups.

## Logiqx XML Format

The primary format used by most modern DAT providers. This is an XML-based format with a standardized schema.

**Used by:**
- No-Intro
- TOSEC
- Redump
- MAME Software Lists

**File extensions:** `.dat`, `.xml`

### Structure

```xml
<?xml version="1.0"?>
<!DOCTYPE datafile PUBLIC "-//Logiqx//DTD ROM Management Datafile//EN" 
	      "http://www.logiqx.com/Dats/datafile.dtd">
<datafile>
	<header>
	    <name>Nintendo - Game Boy</name>
	    <description>Nintendo - Game Boy (20260115-123456)</description>
	    <version>20260115-123456</version>
	    <author>No-Intro</author>
	    <homepage>https://no-intro.org</homepage>
	</header>
	<game name="Tetris (World) (Rev 1)">
	    <description>Tetris (World) (Rev 1)</description>
	    <rom name="Tetris (World) (Rev 1).gb" 
	         size="32768" 
	         crc="46df91ad" 
	         md5="982ed5d2b12a0377eb14bcdc4123744e" 
	         sha1="2c1f73e0f5a5fa0d63a3a7e994fc7e4fc5c51c21"/>
	</game>
</datafile>
```

### Key Elements

| Element | Description |
|---------|-------------|
| `<header>` | Metadata about the DAT file (name, version, author) |
| `<game>` | A game entry containing one or more ROMs |
| `<rom>` | ROM file with name, size, and hash values |
| `<machine>` | Alternative to `<game>`, used by MAME |

### Hash Attributes

| Attribute | Format | Description |
|-----------|--------|-------------|
| `crc` | 8 hex chars | CRC32 checksum |
| `md5` | 32 hex chars | MD5 hash |
| `sha1` | 40 hex chars | SHA-1 hash |
| `size` | decimal | File size in bytes |

## ClrMamePro Format (Planned)

A text-based format created by ClrMamePro. Still used by some providers.

**File extensions:** `.dat`

### Structure

```
clrmamepro (
	name "Nintendo - Game Boy"
	description "Nintendo - Game Boy"
	version 20260115
	author "No-Intro"
)

game (
	name "Tetris (World) (Rev 1)"
	description "Tetris (World) (Rev 1)"
	rom ( name "Tetris (World) (Rev 1).gb" size 32768 crc 46df91ad md5 982ed5d2b12a0377eb14bcdc4123744e sha1 2c1f73e0f5a5fa0d63a3a7e994fc7e4fc5c51c21 )
)
```

## RomCenter Format (Planned)

Legacy format used by older ROM managers.

**File extensions:** `.dat`

## Provider-Specific Notes

### No-Intro

- Uses Logiqx XML format
- All three hashes provided (CRC32, MD5, SHA-1)
- Games named with region and revision tags
- Regular updates (daily/weekly)

### TOSEC

- Uses Logiqx XML format
- Comprehensive naming convention including dump info
- Includes more obscure platforms
- May have multiple dumps per ROM

### MAME

- Uses Logiqx XML with `<machine>` instead of `<game>`
- Includes BIOS and device ROMs
- Parent/clone relationships
- CHD (Compressed Hunks of Data) for disk images

### Redump

- Uses Logiqx XML format
- Focus on optical media (CD, DVD, BD)
- SHA-1 as primary hash
- Includes disc track information

## Importing DAT Files

### Automatic Import

Subrom can automatically download and import DATs from supported providers:

1. Go to **DAT Manager**
2. Click **Add Provider**
3. Select provider (No-Intro, TOSEC, etc.)
4. Choose systems to import
5. Click **Import**

### Manual Import

To import a DAT file manually:

1. Go to **DAT Manager**
2. Click **Import DAT**
3. Select your `.dat` or `.xml` file
4. Subrom will detect the format automatically

## Version Tracking

Subrom tracks DAT file versions and can:
- Detect when updates are available
- Show changes between versions
- Update your ROM status when DATs change
- Keep history of previous versions
