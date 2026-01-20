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
                                │
                    ┌───────────┴───────────┐
                    │      SQLite DB        │
                    │  (portable, embedded) │
                    └───────────────────────┘
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
    options.CommandTimeout(60);
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
    Task<DatFile?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DatFile>> GetAllAsync(CancellationToken ct = default);
    Task<DatFile> AddAsync(DatFile datFile, CancellationToken ct = default);
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
    private readonly Channel<ScanCommand> _channel;
    // Process scan jobs from queue
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
    // Groups: "dat-import-{id}", "scan-{id}", "all-progress"
    
    // Server → Client
    Task DatImportProgress(DatImportProgressDto progress);
    Task ScanProgress(ScanProgressDto progress);
    Task FileHashProgress(FileHashProgressDto progress);
    Task CacheInvalidation(CacheInvalidationDto invalidation);
    
    // Client → Server
    Task SubscribeToOperation(string operationId);
    Task UnsubscribeFromOperation(string operationId);
    Task CancelOperation(string operationId);
}
```

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- [ ] Create new solution structure
- [ ] Set up Domain project with entities
- [ ] Configure EF Core with SQLite
- [ ] Implement basic repositories
- [ ] Add Serilog logging

### Phase 2: Application Services (Week 2)
- [ ] DatService with import/parse
- [ ] HashService with parallel hashing
- [ ] ScanService with background processing
- [ ] DriveService with online detection

### Phase 3: Web API (Week 3)
- [ ] Controllers for all endpoints
- [ ] SignalR hub implementation
- [ ] Request validation with FluentValidation
- [ ] Global error handling
- [ ] API documentation with OpenAPI

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
  "Subrom": {
    "Database": {
      "Path": "%LOCALAPPDATA%/Subrom/subrom.db"
    },
    "Server": {
      "Port": 52100,
      "EnableSwagger": true
    },
    "Scanning": {
      "MaxParallelFiles": 4,
      "HashAlgorithms": ["CRC32", "MD5", "SHA1"],
      "BufferSizeKB": 1024
    },
    "Drives": {
      "AutoDetect": true,
      "PollIntervalSeconds": 30
    }
  }
}
```

## Security Considerations

- **Local-only by default** - Bind to localhost only
- **Optional network access** - Explicit setting to bind to 0.0.0.0
- **No authentication by default** - Single-user local app
- **Optional API key** - For network access if enabled
