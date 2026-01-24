# Backend Architecture Rebuild Plan

## Overview

This plan describes the complete rebuild of the Subrom backend using modern .NET 10 / C# 14 best practices, designed as a **Plex-like local server application** with web UI access.

## Current State Analysis

### Existing Projects (to be rebuilt)
- `SubromAPI` - ASP.NET Web API (basic structure, incomplete)
- `Domain` - Domain models (DAT parsing models exist, need modernization)
- `Services` - Service layer (ScanService, HashService exist)
- `Infrastructure` - Basic extensions
- `Compression` - 7-Zip support (keep and modernize)

### Issues with Current Backend
1. **No proper architecture** - Missing proper layering, DI patterns
2. **Incomplete EF Core setup** - DbContext exists but limited entities
3. **No background service infrastructure** - Only basic ScanService
4. **Missing system tray/service mode** - No Windows service or tray icon
5. **CORS-only development** - No production deployment strategy
6. **No proper error handling** - Missing global exception handling
7. **Missing logging infrastructure** - No structured logging

## Target Architecture: Plex-Like Local Server

```
┌─────────────────────────────────────────────────────────────────────┐
│                     System Tray Application                          │
│           (Windows Forms / MAUI for tray icon + menu)               │
│        [Open Web UI] [Settings] [View Logs] [Restart] [Exit]        │
├─────────────────────────────────────────────────────────────────────┤
│                      Subrom.Server (Host)                            │
│              Windows Service / IHostedService                        │
│                   http://localhost:52100                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │    Web API       │  │    SignalR       │  │  Static Files    │  │
│  │  Controllers     │  │     Hubs         │  │   (React UI)     │  │
│  └────────┬─────────┘  └────────┬─────────┘  └──────────────────┘  │
│           │                     │                                    │
│  ┌────────┴─────────────────────┴────────────────────────────────┐  │
│  │                     Application Layer                          │  │
│  │   DatService │ ScanService │ VerificationService │ DriveService│  │
│  └────────────────────────────┬──────────────────────────────────┘  │
│                               │                                      │
│  ┌────────────────────────────┴──────────────────────────────────┐  │
│  │                     Domain Layer                               │  │
│  │  Entities │ Value Objects │ Aggregates │ Domain Events         │  │
│  └────────────────────────────┬──────────────────────────────────┘  │
│                               │                                      │
│  ┌────────────────────────────┴──────────────────────────────────┐  │
│  │                  Infrastructure Layer                          │  │
│  │  EF Core │ DAT Parsers │ File System │ Compression │ Logging  │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
└───────────────────────────────┬──────────────────────────────────────┘
`t                            │
`t                ┌───────────┴───────────┐
`t                │      SQLite DB        │
`t                │  (portable, embedded) │
`t                └───────────────────────┘
```

## Database: SQLite Analysis

### Can SQLite Handle the Load?

**YES**, SQLite is ideal for Subrom's use case:

| Factor | Analysis |
|--------|----------|
| **Write Volume** | Low - DAT imports are infrequent, scans are periodic |
| **Read Volume** | High but local-only - Single user, no concurrent connections |
| **Data Size** | Moderate - 60K games × ~10 columns = ~6MB; 1M ROMs = ~100MB |
| **Complexity** | Simple queries - No complex joins, mostly lookups by hash |
| **Deployment** | Zero-config - No server installation needed |
| **Portability** | Single file - Easy backup, move between machines |
| **Performance** | Excellent - Plex uses SQLite for same reasons |

### SQLite Configuration for Performance

```csharp
optionsBuilder.UseSqlite(connectionString, options => {
`toptions.CommandTimeout(60);
})
.EnableSensitiveDataLogging(isDevelopment)
.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

// Connection string optimizations
"Data Source=subrom.db;Cache=Shared;Mode=ReadWriteCreate;Pooling=True"

// Pragmas for performance
PRAGMA journal_mode = WAL;          -- Write-Ahead Logging for concurrent reads
PRAGMA synchronous = NORMAL;        -- Faster writes, still safe
PRAGMA cache_size = -64000;         -- 64MB cache
PRAGMA mmap_size = 268435456;       -- 256MB memory-mapped I/O
PRAGMA temp_store = MEMORY;         -- Temp tables in memory
```

### When to Consider Alternatives

Only if:
- Multi-user server deployment (use PostgreSQL)
- Cloud deployment with multiple instances (use PostgreSQL/SQL Server)
- Data exceeds 10GB+ (unlikely for ROM metadata)

## Project Structure (Rebuild)

> **Implementation Status (Session 07):** The core 4-project structure is complete and building.
> Implemented: Domain (Aggregates, ValueObjects, Enums), Application (Interfaces, DTOs, Progress),
> Infrastructure (DbContext, Repositories, Parsing, Services), Server (Endpoints, Hubs, DI).

```
Subrom/
├── src/
│   ├── Subrom.Domain/                 # Core domain models ✅
│   │   ├── Aggregates/                # DDD aggregates
│   │   │   ├── DatFiles/              # DatFile, GameEntry, RomEntry
│   │   │   ├── Drives/                # Drive aggregate
│   │   │   ├── RomFiles/              # RomFile (scanned files)
│   │   │   └── ScanJobs/              # ScanJob aggregate
│   │   ├── ValueObjects/              # Immutable value types
│   │   │   └── Hashes.cs              # CRC32, MD5, SHA1 records
│   │   └── Enums/                     # DatProvider, ScanStatus, etc.
│   │
│   ├── Subrom.Application/            # Application services ✅
│   │   ├── DTOs/                      # Data transfer objects
│   │   ├── Progress/                  # Progress reporting types
│   │   └── Interfaces/                # Repository + service contracts
│   │
│   ├── Subrom.Infrastructure/         # Infrastructure implementations ✅
│   │   ├── Persistence/
│   │   │   ├── SubromDbContext.cs     # EF Core context
│   │   │   ├── Configurations/        # Entity type configs
│   │   │   ├── Repositories/          # DatFile, Drive, RomFile, ScanJob repos
│   │   │   └── UnitOfWork.cs          # Transaction support
│   │   ├── Parsing/                   # DAT file parsers
│   │   │   ├── LogiqxDatParser.cs     # XML (No-Intro/Redump/TOSEC)
│   │   │   └── DatParserFactory.cs    # Format detection
│   │   ├── Services/
│   │   │   └── HashService.cs         # CRC32/MD5/SHA1 hashing
│   │   └── DependencyInjection.cs     # AddInfrastructure() extension
│   │
│   ├── Subrom.Server/                 # ASP.NET Core host ✅
│   │   ├── Endpoints/                 # Minimal API endpoints
│   │   ├── Hubs/                      # SignalR ProgressHub
│   │   └── Program.cs                 # Host configuration
│   │
│   ├── Subrom.Tray/                   # System tray application (future)
│   │
│   └── Subrom.Service/                # Windows Service wrapper (future)
│
├── tests/                             # (future)
│
```

### Original Planned Structure (Reference)
```
Subrom/
├── src/
│   ├── Subrom.Domain/                 # Core domain models
│   │   ├── Entities/
│   │   │   ├── DatFile.cs
│   │   │   ├── Game.cs
│   │   │   ├── Rom.cs
│   │   │   ├── Drive.cs
│   │   │   ├── ScanJob.cs
│   │   │   └── ScannedFile.cs
│   │   ├── ValueObjects/
│   │   │   ├── Hash.cs
│   │   │   ├── FilePath.cs
│   │   │   └── FileSize.cs
│   │   ├── Enums/
│   │   ├── Events/
│   │   └── Interfaces/
│   │
│   ├── Subrom.Application/            # Application services
│   │   ├── Services/
│   │   │   ├── DatService.cs
│   │   │   ├── ScanService.cs
│   │   │   ├── VerificationService.cs
│   │   │   ├── DriveService.cs
│   │   │   └── HashService.cs
│   │   ├── DTOs/
│   │   ├── Mapping/
│   │   └── Interfaces/
│   │
│   ├── Subrom.Infrastructure/         # Infrastructure implementations
│   │   ├── Persistence/
│   │   │   ├── SubromDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Parsers/
│   │   │   ├── XmlDatParser.cs
│   │   │   ├── ClrMameProParser.cs
│   │   │   └── Interfaces/
│   │   ├── FileSystem/
│   │   ├── Compression/
│   │   └── Hashing/
│   │
│   ├── Subrom.Server/                 # ASP.NET Core host
│   │   ├── Controllers/
│   │   ├── Hubs/
│   │   ├── Middleware/
│   │   ├── BackgroundServices/
│   │   └── Program.cs
│   │
│   ├── Subrom.Tray/                   # System tray application
│   │   ├── TrayIcon.cs
│   │   ├── SettingsForm.cs
│   │   └── Program.cs
│   │
│   └── Subrom.Service/                # Windows Service wrapper
│       └── Program.cs
│
├── tests/
│   ├── Subrom.Domain.Tests/
│   ├── Subrom.Application.Tests/
│   ├── Subrom.Infrastructure.Tests/
│   └── Subrom.Server.Tests/
│
├── subrom-ui/                         # React frontend (existing)
└── Subrom.sln
```

## Technology Stack

### Core
- **.NET 10** - Latest LTS-adjacent (preview)
- **C# 14** - Latest language features
- **ASP.NET Core 10** - Web API + SignalR
- **Entity Framework Core 10** - ORM with SQLite

### Key Libraries
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="FluentValidation" Version="11.11.0" />
<PackageReference Include="Mapperly" Version="4.1.0" />           <!-- Source-gen mapper -->
<PackageReference Include="Polly" Version="8.5.0" />              <!-- Resilience -->
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="10.0.0" />
```

### System Tray (Windows)
- **Windows Forms** - Simple tray icon (NotifyIcon)
- Or **H.NotifyIcon** - Modern WPF/WinUI tray icon library

## Key Design Patterns

### 1. Repository Pattern (Optional)
EF Core is already a repository/UoW, but for testability:
```csharp
public interface IDatRepository {
`tTask<DatFile?> GetByIdAsync(int id, CancellationToken ct = default);
`tTask<IReadOnlyList<DatFile>> GetAllAsync(CancellationToken ct = default);
`tTask<DatFile> AddAsync(DatFile datFile, CancellationToken ct = default);
}
```

### 2. CQRS-lite
Separate read/write operations for complex queries:
```csharp
// Queries
public record GetGamesByHashQuery(string Crc32, string? Md5 = null, string? Sha1 = null);

// Commands
public record ImportDatCommand(Stream DatStream, string FileName);
```

### 3. Background Services
```csharp
public class ScanBackgroundService : BackgroundService {
`tprivate readonly Channel<ScanCommand> _channel;
`t// Process scan jobs from queue
}
```

### 4. Domain Events
```csharp
public record DatImportedEvent(int DatId, string FileName, int GameCount);
public record ScanCompletedEvent(Guid JobId, int TotalFiles, int VerifiedFiles);
```

## API Design

### Endpoints
```
# DAT Management
GET    /api/dats                    # List all DAT files
GET    /api/dats/{id}               # Get DAT details
POST   /api/dats/import             # Import DAT file (multipart)
DELETE /api/dats/{id}               # Remove DAT

# Games
GET    /api/games                   # List games (paged, filtered)
GET    /api/games/{id}              # Get game with ROMs
GET    /api/games/search            # Search by name

# ROMs
GET    /api/roms/lookup             # Lookup by hash
GET    /api/roms/{id}               # Get ROM details

# Drives
GET    /api/drives                  # List configured drives
POST   /api/drives                  # Add drive
PUT    /api/drives/{id}             # Update drive
DELETE /api/drives/{id}             # Remove drive
GET    /api/drives/{id}/status      # Check if online

# Scans
POST   /api/scans                   # Start new scan
GET    /api/scans                   # List scan jobs
GET    /api/scans/{id}              # Get scan status
DELETE /api/scans/{id}              # Cancel scan

# Verification
POST   /api/verification/file       # Verify single file
POST   /api/verification/folder     # Verify folder
GET    /api/verification/report     # Get verification report

# System
GET    /api/system/health           # Health check
GET    /api/system/stats            # System statistics
POST   /api/system/settings         # Update settings
```

### SignalR Hubs
```csharp
public class ProgressHub : Hub {
`t// Groups: "dat-import-{id}", "scan-{id}", "all-progress"
`t
`t// Server → Client
`tTask DatImportProgress(DatImportProgressDto progress);
`tTask ScanProgress(ScanProgressDto progress);
`tTask FileHashProgress(FileHashProgressDto progress);
`tTask CacheInvalidation(CacheInvalidationDto invalidation);
`t
`t// Client → Server
`tTask SubscribeToOperation(string operationId);
`tTask UnsubscribeFromOperation(string operationId);
`tTask CancelOperation(string operationId);
}
```

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1) ✅ COMPLETE
- [x] Create new solution structure
- [x] Set up Domain project with entities
- [x] Configure EF Core with SQLite
- [x] Implement basic repositories
- [ ] Add Serilog logging

### Phase 2: Application Services (Week 2) ✅ COMPLETE
- [x] DatFileService with import/parse
- [x] HashService with parallel hashing
- [x] ScanService with background processing
- [x] DriveService with online detection
- [x] VerificationService for ROM verification

### Phase 3: Web API (Week 3) 🔄 IN PROGRESS
- [ ] Controllers for all endpoints
- [x] SignalR hub implementation (ProgressHub)
- [ ] Request validation with FluentValidation
- [x] Global error handling
- [x] API documentation with OpenAPI (Scalar)

### Phase 4: System Tray & Service (Week 4)
- [ ] Windows service host
- [ ] System tray application
- [ ] Settings management
- [ ] Auto-start configuration
- [ ] Log viewer

### Phase 5: Integration & Polish (Week 5)
- [ ] Static file serving for React UI
- [ ] Production configuration
- [ ] Installer/deployment
- [ ] Documentation
- [ ] Tests

## Performance Targets

| Metric | Target |
|--------|--------|
| DAT Import (60K games) | < 30 seconds |
| ROM Lookup by Hash | < 10ms |
| File Hash (1GB file) | Limited by disk I/O |
| Scan Discovery (10K files) | < 5 seconds |
| Memory Usage (idle) | < 100MB |
| Memory Usage (scanning) | < 500MB |
| Database Size (100K ROMs) | < 50MB |

## Migration Strategy

1. **Keep existing code running** during rebuild
2. **Build new solution alongside** existing structure
3. **Migrate one service at a time** with feature parity
4. **Run integration tests** between old/new
5. **Switch over** once all features work
6. **Remove old code** after verification

## Configuration

```json
{
`t"Subrom": {
`t"Database": {
`t  "Path": "%LOCALAPPDATA%/Subrom/subrom.db"
`t},
`t"Server": {
`t  "Port": 52100,
`t  "EnableSwagger": true
`t},
`t"Scanning": {
`t  "MaxParallelFiles": 4,
`t  "HashAlgorithms": ["CRC32", "MD5", "SHA1"],
`t  "BufferSizeKB": 1024
`t},
`t"Drives": {
`t  "AutoDetect": true,
`t  "PollIntervalSeconds": 30
`t}
`t}
}
```

## Security Considerations

- **Local-only by default** - Bind to localhost only
- **Optional network access** - Explicit setting to bind to 0.0.0.0
- **No authentication by default** - Single-user local app
- **Optional API key** - For network access if enabled
