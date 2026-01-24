# Subrom Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Subrom UI                                  │
│                    (React + TypeScript)                              │
├─────────────────────────────────────────────────────────────────────┤
│                         REST API                                     │
│                    (ASP.NET Core Web API)                           │
├─────────────────────────────────────────────────────────────────────┤
│                      Service Layer                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐   │
│  │ DatService   │  │ ScanService  │  │ OrganizationService      │   │
│  ├──────────────┤  ├──────────────┤  ├──────────────────────────┤   │
│  │ HashService  │  │ FileService  │  │ StorageService           │   │
│  ├──────────────┤  ├──────────────┤  ├──────────────────────────┤   │
│  │ ProviderSvc  │  │ ArchiveSvc   │  │ ConfigurationService     │   │
│  └──────────────┘  └──────────────┘  └──────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────┤
│                      Domain Layer                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐   │
│  │ Datfiles     │  │ Hash         │  │ Storage                  │   │
│  │ - Datafile   │  │ - Crc        │  │ - Drive                  │   │
│  │ - Game       │  │ - Md5        │  │ - FileLocation           │   │
│  │ - Rom        │  │ - Sha1       │  │ - ScanResult             │   │
│  │ - Machine    │  │ - Hashes     │  │ - VerificationResult     │   │
│  └──────────────┘  └──────────────┘  └──────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐   │
│  │ Database     │  │ FileSystem   │  │ HTTP Clients             │   │
│  │ (EF Core +   │  │ Abstraction  │  │ (DAT Providers)          │   │
│  │  SQLite)     │  │              │  │                          │   │
│  └──────────────┘  └──────────────┘  └──────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
Subrom.sln
├── Domain/                      # Core domain models
│   ├── Datfiles/               # DAT file models
│   │   ├── Datafile.cs
│   │   ├── Game.cs
│   │   ├── Rom.cs
│   │   ├── Machine.cs
│   │   └── Kinds/              # Enums and value types
│   ├── Hash/                   # Hash value types
│   │   ├── Crc.cs
│   │   ├── Md5.cs
│   │   ├── Sha1.cs
│   │   └── Hashes.cs
│   ├── Storage/                # Storage domain models
│   │   ├── Drive.cs
│   │   ├── FileLocation.cs
│   │   └── RomFile.cs
│   └── Scanning/               # Scanning domain models
│       ├── ScanJob.cs
│       ├── ScanResult.cs
│       └── VerificationResult.cs
│
├── Services/                    # Business logic services
│   ├── HashService.cs
│   ├── DatService.cs
│   ├── ScanService.cs
│   ├── OrganizationService.cs
│   ├── StorageService.cs
│   └── Interfaces/             # Service interfaces
│
├── Infrastructure/              # External concerns
│   ├── Database/               # EF Core contexts
│   │   ├── SubromDbContext.cs
│   │   └── Migrations/
│   ├── DatProviders/           # DAT file providers
│   │   ├── NoIntroProvider.cs
│   │   ├── TosecProvider.cs
│   │   ├── RedumpProvider.cs
│   │   └── GoodProvider.cs
│   ├── Parsers/                # DAT file parsers
│   │   ├── XmlDatParser.cs
│   │   ├── ClrMameParser.cs
│   │   └── IDatParser.cs
│   └── Extensions/             # Extension methods
│
├── Compression/                 # Archive handling
│   └── SevenZip/               # 7-Zip integration
│
├── Subrom/                      # Web API project
│   ├── Controllers/
│   ├── Program.cs
│   └── appsettings.json
│
├── ConsoleTesting/              # CLI testing project
│
└── subrom-ui/                   # React frontend
	├── src/
	│   ├── components/
	│   ├── pages/
	│   ├── services/
	│   └── hooks/
	└── package.json
```

## Data Flow

### DAT File Processing

```
┌─────────────┐     ┌──────────────┐     ┌───────────────┐
│ DAT Provider│────▶│ DAT Parser   │────▶│ Database      │
│ (Download)  │     │ (XML/ClrMame)│     │ (Store DATs)  │
└─────────────┘     └──────────────┘     └───────────────┘
```

### ROM Scanning

```
┌─────────────┐     ┌──────────────┐     ┌───────────────┐
│ File System │────▶│ Scanner      │────▶│ Hash Service  │
│ (Enumerate) │     │ (Find ROMs)  │     │ (Calculate)   │
└─────────────┘     └──────────────┘     └───────┬───────┘
	                                              │
	                ┌──────────────┐     ┌───────▼───────┐
	                │ Results      │◀────│ Verifier      │
	                │ (Store)      │     │ (Match DATs)  │
	                └──────────────┘     └───────────────┘
```

### ROM Organization

```
┌─────────────┐     ┌──────────────┐     ┌───────────────┐
│ Scan Results│────▶│ Organization │────▶│ File Mover    │
│ (Input)     │     │ Rules        │     │ (Execute)     │
└─────────────┘     └──────────────┘     └───────┬───────┘
	                                              │
	                ┌──────────────┐     ┌───────▼───────┐
	                │ Database     │◀────│ Logger        │
	                │ (Update)     │     │ (Record)      │
	                └──────────────┘     └───────────────┘
```

## Key Design Decisions

### 1. Offline Drive Resilience

**Problem:** When a drive goes offline, ROMs shouldn't be "lost" from the database.

**Solution:**
- Store drive registration with unique ID
- Track ROM locations with drive reference
- Mark ROMs as "offline" when drive unavailable
- Reconnect automatically when drive returns
- Never delete ROM records just because drive is offline

### 2. Streaming DAT Parsing

**Problem:** Large DAT files (MAME) can be hundreds of MB.

**Solution:**
- Use streaming XML parsing
- Process games one at a time
- Store directly to database
- Support incremental updates

### 3. Hash Caching

**Problem:** Re-hashing unchanged files is slow.

**Solution:**
- Store file size + modification time with hash
- Skip rehashing if metadata unchanged
- Invalidate cache on file change
- Support forced re-verification

### 4. Multi-Provider DAT Handling

**Problem:** Same ROM may exist in multiple DAT sets with different names.

**Solution:**
- Primary hash-based identification
- Cross-reference table for DAT entries
- User preference for naming source
- Merge duplicate entries intelligently

## Database Schema (Conceptual)

```sql
-- DAT Files
DatFiles (Id, Name, Version, Date, Provider, Category, FilePath)
DatGames (Id, DatFileId, Name, Description, Year, Manufacturer)
DatRoms (Id, DatGameId, Name, Size, Crc32, Md5, Sha1, Status)

-- Storage
Drives (Id, VolumeId, Label, Path, IsOnline, LastSeen)
RomFiles (Id, DriveId, Path, Size, ModifiedAt)
RomHashes (RomFileId, Crc32, Md5, Sha1, VerifiedAt)

-- Cross Reference
RomDatMatches (RomFileId, DatRomId, MatchType)

-- Scan Jobs
ScanJobs (Id, StartedAt, CompletedAt, Status, TotalFiles, ProcessedFiles)
ScanResults (ScanJobId, RomFileId, ResultType, Details)
```

## API Endpoints (Planned)

```
GET    /api/dats                    # List all DAT files
POST   /api/dats/import             # Import a DAT file
POST   /api/dats/update             # Update DATs from providers
GET    /api/dats/{id}/games         # List games in a DAT

GET    /api/drives                  # List registered drives
POST   /api/drives                  # Register a drive
DELETE /api/drives/{id}             # Unregister a drive

POST   /api/scan                    # Start a scan job
GET    /api/scan/{id}               # Get scan job status
GET    /api/scan/{id}/results       # Get scan results

GET    /api/roms                    # List known ROMs
GET    /api/roms/missing            # List missing ROMs
GET    /api/roms/duplicates         # List duplicate ROMs

POST   /api/organize/preview        # Preview organization changes
POST   /api/organize/execute        # Execute organization
POST   /api/organize/undo           # Undo last organization
```

## Configuration

```json
{
	"Subrom": {
		"Database": {
			"ConnectionString": "Data Source=subrom.db"
		},
		"Scanning": {
			"ParallelThreads": 4,
			"SkipHiddenFiles": true,
			"SupportedExtensions": [".zip", ".7z", ".rar", ".nes", ".sfc", ".gba"]
		},
		"Organization": {
			"DefaultTemplate": "{System}/{Game}.{Extension}",
			"PreferRegion": ["USA", "Europe", "Japan"],
			"Use1G1R": true
		},
		"Providers": {
			"NoIntro": {
				"Enabled": true,
				"UpdateInterval": "7d"
			},
			"TOSEC": {
				"Enabled": true,
				"UpdateInterval": "30d"
			}
		}
	}
}
```

## Related Documents

- [Project Roadmap](roadmap.md)
- [UI Design Plans](ui-plans.md)
- [API Design](api-design.md)
