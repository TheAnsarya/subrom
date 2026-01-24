# Subrom Current Architecture (January 2026)

## Overview

Subrom uses **Clean Architecture** with a clear separation between layers. The codebase follows Domain-Driven Design (DDD) principles with distinct aggregates for different bounded contexts.

## Project Structure

```
Subrom.sln
├── src/
│   ├── Subrom.Domain/           # Core domain models and value objects
│   ├── Subrom.Application/      # Use cases, interfaces, DTOs
│   ├── Subrom.Infrastructure/   # External concerns (DB, HTTP, files)
│   └── Subrom.Server/           # ASP.NET Core Web API + SignalR
├── tests/
│   └── Subrom.Tests.Unit/       # Unit tests
├── subrom-ui/                   # React + TypeScript frontend
├── scripts/                     # PowerShell automation scripts
└── ~docs/                       # Development documentation
```

## Layer Dependencies

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Subrom.Server                                 │
│              (ASP.NET Core Web API + SignalR)                        │
│   - Endpoints (Minimal API)                                          │
│   - SignalR Hubs for real-time progress                              │
│   - DI composition root                                              │
├─────────────────────────────────────────────────────────────────────┤
│                      Subrom.Application                              │
│                    (Use Cases & Interfaces)                          │
│   - Service interfaces (IHashService, IArchiveService, etc.)        │
│   - Repository interfaces (IDatFileRepository, etc.)                │
│   - DTOs and request/response models                                 │
│   - DependencyInjection.cs (layer registration)                     │
├─────────────────────────────────────────────────────────────────────┤
│                     Subrom.Infrastructure                            │
│                    (External Implementations)                        │
│   ├── Persistence/     - EF Core + SQLite                           │
│   ├── Providers/       - DAT providers (No-Intro, TOSEC, MAME)      │
│   ├── Parsing/         - DAT file parsers (Logiqx XML)              │
│   └── Services/        - HashService, SharpCompressArchiveService   │
├─────────────────────────────────────────────────────────────────────┤
│                        Subrom.Domain                                 │
│                    (Pure Domain Models)                              │
│   ├── Aggregates/                                                    │
│   │   ├── DatFiles/    - DatFile, GameEntry, RomEntry               │
│   │   ├── Scanning/    - ScanJob, RomFile                           │
│   │   └── Storage/     - Drive, FileLocation                        │
│   ├── ValueObjects/    - Crc, Md5, Sha1, RomHashes                  │
│   └── Common/          - Entity base, Result types                  │
└─────────────────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### 1. Clean Architecture Layers

| Layer | Namespace | Purpose | Dependencies |
|-------|-----------|---------|--------------|
| Domain | `Subrom.Domain` | Business entities, value objects | None |
| Application | `Subrom.Application` | Interfaces, DTOs, use cases | Domain |
| Infrastructure | `Subrom.Infrastructure` | DB, HTTP, file system | Application, Domain |
| Server | `Subrom.Server` | Web API, DI composition | All |

### 2. Repository Pattern with Unit of Work

```csharp
// Application layer defines interfaces
public interface IDatFileRepository {
	Task<DatFile?> GetByIdAsync(Guid id, CancellationToken ct);
	Task<IReadOnlyList<DatFile>> GetAllAsync(CancellationToken ct);
	void Add(DatFile datFile);
}

public interface IUnitOfWork {
	Task<int> SaveChangesAsync(CancellationToken ct);
}

// Infrastructure implements them with EF Core
public class DatFileRepository : IDatFileRepository { ... }
public class UnitOfWork : IUnitOfWork { ... }
```

### 3. Service Interfaces in Application Layer

All service interfaces are defined in `Subrom.Application/Interfaces/`:

| Interface | Purpose | Implementation |
|-----------|---------|----------------|
| `IHashService` | Compute CRC32, MD5, SHA-1 | `HashService` |
| `IArchiveService` | Read/extract archives | `SharpCompressArchiveService` |
| `IDatParser` | Parse DAT files | `LogiqxDatParser` |
| `IDatProvider` | Download DATs from sources | `NoIntroProvider`, `TosecProvider`, `MameProvider` |
| `IDatCollectionService` | Manage DAT collections | `DatCollectionService` |

### 4. Value Objects for Type Safety

The domain uses value objects for type-safe hash values:

```csharp
public readonly record struct Crc {
	public string Value { get; }
	public static Crc Create(string hex) => new(hex.ToLowerInvariant());
}

public readonly record struct RomHashes(Crc Crc, Md5 Md5, Sha1 Sha1);
```

### 5. Dependency Injection

Each layer has a `DependencyInjection.cs` extension method:

```csharp
// Program.cs (composition root)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
```

## Frontend Architecture

```
subrom-ui/
├── src/
│   ├── components/          # Reusable UI components
│   │   └── ui/              # Base UI primitives
│   ├── hooks/               # Custom React hooks
│   ├── stores/              # Zustand state stores
│   ├── types/               # TypeScript type definitions
│   ├── pages/               # Page components
│   └── api/                 # API client functions
├── public/                  # Static assets
└── package.json             # Yarn dependencies
```

### State Management: Zustand

```typescript
// Example store
export const useDatStore = create<DatStore>((set) => ({
	datFiles: [],
	isLoading: false,
	fetchDatFiles: async () => {
	    set({ isLoading: true });
	    const files = await apiClient.getDatFiles();
	    set({ datFiles: files, isLoading: false });
	}
}));
```

### Real-time Updates: SignalR

The frontend connects to SignalR hubs for real-time progress:
- `ScanHub` - File scanning progress
- Progress updates streamed during long operations

## Database Schema (SQLite + EF Core)

### Main Tables

| Table | Purpose |
|-------|---------|
| `DatFiles` | Imported DAT file metadata |
| `GameEntries` | Games within DAT files |
| `RomEntries` | ROMs within games |
| `Drives` | Registered storage drives |
| `RomFiles` | Scanned ROM files |
| `ScanJobs` | File scanning jobs |

### Key Relationships

```
DatFile 1──* GameEntry 1──* RomEntry
Drive 1──* RomFile
ScanJob 1──* RomFile
```

## Archive Support

The `IArchiveService` supports multiple archive formats via SharpCompress:

| Format | Extensions | Support Level |
|--------|------------|---------------|
| ZIP | `.zip` | Full |
| 7-Zip | `.7z` | Full |
| RAR | `.rar` | Read-only |
| TAR | `.tar` | Full |
| GZip | `.gz`, `.tgz` | Full |
| BZip2 | `.bz2` | Full |
| XZ | `.xz` | Full |
| LZip | `.lz` | Full |

## DAT Providers

Currently implemented providers:

| Provider | Status | DAT Format |
|----------|--------|------------|
| No-Intro | ⚠️ Rate-limited | Logiqx XML |
| TOSEC | ✅ Working | Logiqx XML |
| MAME | ✅ Working | Logiqx XML |
| Redump | 🔜 Planned | Logiqx XML |

## API Endpoints

All API endpoints use ASP.NET Core Minimal APIs in `Subrom.Server/Endpoints/`:

| Endpoint Group | Purpose |
|----------------|---------|
| `/api/dat-providers` | List available DAT providers |
| `/api/dat-files` | CRUD for imported DAT files |
| `/api/drives` | Storage drive management |
| `/api/scan` | File scanning operations |

## Build & Run

```bash
# Backend
dotnet build Subrom.sln
dotnet run --project src/Subrom.Server

# Frontend
cd subrom-ui
yarn install
yarn dev
```

## Technology Stack

### Backend
- .NET 10 / C# 14
- ASP.NET Core (Minimal APIs)
- SignalR (real-time)
- EF Core + SQLite
- SharpCompress (archives)

### Frontend
- React 19
- TypeScript 5.7
- Vite 6
- Zustand (state)
- react-window (virtualization)
- FontAwesome icons
- CSS Modules
