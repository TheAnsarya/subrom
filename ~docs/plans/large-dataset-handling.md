# Large Dataset Handling Strategy

## Problem Statement

Subrom must handle DAT files and ROM collections of vastly different sizes:

### DAT File Scale
- **TOSEC Pack**: 4,743 DAT files totaling ~100MB compressed
- **Single Large DAT**: Up to 61,454 game entries (18MB XML)
- **Small DAT**: Can have just a handful of entries
- **Hierarchical Structure**: DATs organized by platform → category → format

### ROM File Scale
- **File Sizes**: Range from a few KB (NES ROMs) to several GB (disc images)
- **Collection Sizes**: Can range from hundreds to hundreds of thousands of files
- **Hash Calculations**: Must hash files up to several GB in size

### Memory Constraints
- Loading 61K entries × multiple DATs = potential OOM
- Caching entire collections = RAM exhaustion
- UI rendering 10K+ rows = browser freeze

## Architecture Strategy

### 1. Server-Side: Streaming & Pagination

#### DAT Import Pipeline
```
DAT File → XML Stream Reader → Batch Processor → Database
                ↓
         SignalR Progress Updates
```

- Stream-parse XML (don't load entire file into memory)
- Process in batches of 500-1000 entries
- Report progress via SignalR every batch
- Database bulk inserts with transactions

#### API Design
```
GET /api/dats                      → Paginated list of DAT files
GET /api/dats/{id}                 → Single DAT metadata
GET /api/dats/{id}/games           → Paginated games (cursor-based)
GET /api/dats/{id}/games/search    → Server-side search with pagination
GET /api/roms                      → Paginated ROM files
GET /api/roms/search               → Full-text search with pagination
```

#### Cursor-Based Pagination
- Better for large datasets than offset pagination
- Use `lastId` + `limit` pattern
- Stable pagination even with concurrent writes

### 2. Client-Side: Virtualization & Caching

#### Data Virtualization (UI)
- Only render visible rows (~20-50 at a time)
- Virtual scrolling for large lists
- Progressive loading on scroll
- React-window or similar library

#### Client-Side Cache Strategy
```typescript
interface CacheEntry<T> {
	data: T;
	timestamp: number;
	accessCount: number;
	sizeEstimate: number;
}

interface CacheConfig {
	maxEntries: number;      // e.g., 1000
	maxMemoryMB: number;     // e.g., 50MB
	ttlMs: number;           // e.g., 5 minutes
	evictionPolicy: 'lru' | 'lfu' | 'ttl';
}
```

#### Cache Eviction
- **LRU** (Least Recently Used) for general data
- **TTL** (Time To Live) for volatile data
- **Size-based** eviction when approaching memory limit
- Automatic cleanup on visibility change (tab hidden)

### 3. SignalR Real-Time Updates

#### Event Types
```typescript
// DAT Import Progress
interface DatImportProgress {
	datId: number;
	datName: string;
	phase: 'parsing' | 'processing' | 'indexing';
	current: number;
	total: number;
	percentage: number;
	currentItem?: string;
}

// ROM Scan Progress
interface RomScanProgress {
	scanId: string;
	status: 'scanning' | 'hashing' | 'verifying' | 'completed';
	currentPath: string;
	filesScanned: number;
	filesTotal: number;
	bytesProcessed: number;
	bytesTotal: number;
	currentFileProgress?: number; // For large files
}

// Cache Invalidation
interface CacheInvalidation {
	type: 'dat' | 'game' | 'rom' | 'verification';
	ids: number[];
	action: 'update' | 'delete' | 'insert';
}
```

### 4. DAT Hierarchy Model

```typescript
interface DatHierarchy {
	id: number;
	name: string;
	parentId?: number;
	children: DatHierarchy[];
	datFiles: DatFile[];
	
	// Aggregated stats
	totalDats: number;
	totalGames: number;
	totalRoms: number;
}

// Example structure:
// TOSEC/
//   Commodore/
//     C64/
//       Games - [D64].dat
//       Demos - [D64].dat
//     Amiga/
//       Games - [ADF].dat
//   Sinclair/
//     ZX Spectrum/
//       Games - [TAP].dat
```

### 5. Implementation Phases

#### Phase 1: SignalR Streaming Foundation
- Enhance SignalR hub with progress channels
- Add batch progress reporting
- Implement cache invalidation events

#### Phase 2: Virtual DataTable
- Add react-window for virtualization
- Implement infinite scroll
- Add cursor-based pagination support

#### Phase 3: Client Cache System
- Create cache store with Zustand
- Implement LRU eviction
- Add memory monitoring
- Add cache invalidation handlers

#### Phase 4: Server Streaming
- Add streaming XML parser to backend
- Implement batch processing
- Add cursor-based API endpoints

#### Phase 5: DAT Hierarchy
- Create hierarchy model
- Add tree view component
- Implement lazy loading for branches

## File Structure

```
src/
├── hooks/
│   ├── useVirtualScroll.ts      # Virtual scrolling hook
│   ├── useInfiniteQuery.ts      # Infinite scroll data fetching
│   └── useCacheInvalidation.ts  # SignalR cache sync
├── stores/
│   ├── cacheStore.ts            # LRU cache with memory limits
│   └── datHierarchyStore.ts     # DAT tree structure
├── components/
│   ├── ui/
│   │   ├── VirtualTable/        # Virtualized data table
│   │   └── TreeView/            # Hierarchical tree view
│   └── DatTree/                 # DAT hierarchy browser
└── utils/
    ├── cache.ts                 # Cache utilities
    └── memoryMonitor.ts         # Memory usage tracking
```

## Performance Targets

| Metric | Target |
|--------|--------|
| Initial page load | < 1s |
| DAT list render (1000 items) | < 100ms |
| Game list scroll (60K items) | 60 FPS |
| Memory usage (idle) | < 100MB |
| Memory usage (active) | < 300MB |
| Cache hit rate | > 80% |

## Testing Strategy

### Unit Tests
- Cache eviction logic
- Pagination cursor generation
- Memory estimation functions

### Integration Tests
- SignalR connection lifecycle
- Cache invalidation flow
- Large file hashing progress

### Performance Tests
- Render 60K rows with virtualization
- Import 18MB DAT file
- Scan 10K ROM files

### Load Tests
- Concurrent DAT imports
- Multiple SignalR clients
- Memory pressure scenarios
