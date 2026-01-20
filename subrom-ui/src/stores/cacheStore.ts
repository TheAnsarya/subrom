import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import type { CacheInvalidation, DataChunkResponse } from '@/types/signalr';

// ============================================================================
// Types
// ============================================================================

interface CacheEntry<T = unknown> {
	/** Cached data */
	data: T;
	/** Timestamp when entry was created */
	createdAt: number;
	/** Timestamp when entry was last accessed */
	lastAccessedAt: number;
	/** Time-to-live in milliseconds (0 = never expires) */
	ttl: number;
	/** Estimated size in bytes */
	sizeBytes: number;
	/** Access count for LRU tracking */
	accessCount: number;
}

interface CursorState {
	/** Next cursor for pagination */
	nextCursor: string | null;
	/** Whether more data exists */
	hasMore: boolean;
	/** Total count if known */
	totalCount?: number;
}

interface CacheStats {
	/** Total entries in cache */
	entryCount: number;
	/** Total estimated memory usage in bytes */
	totalSizeBytes: number;
	/** Number of cache hits */
	hits: number;
	/** Number of cache misses */
	misses: number;
	/** Number of evictions */
	evictions: number;
	/** Hit rate percentage */
	hitRate: number;
}

interface CacheState {
	// Cache storage
	entries: Map<string, CacheEntry>;
	cursorStates: Map<string, CursorState>;

	// Configuration
	maxSizeBytes: number;
	defaultTtlMs: number;
	maxEntries: number;

	// Statistics
	stats: CacheStats;

	// Actions
	get: <T>(key: string) => T | undefined;
	set: <T>(key: string, data: T, options?: CacheSetOptions) => void;
	has: (key: string) => boolean;
	delete: (key: string) => void;
	clear: () => void;

	// Cursor state management
	getCursorState: (key: string) => CursorState | undefined;
	setCursorState: (key: string, state: CursorState) => void;

	// Bulk operations
	setChunk: <T>(dataType: string, items: T[], cursor: string | null, response: DataChunkResponse) => void;
	getChunk: <T>(dataType: string, cursor?: string) => T[] | undefined;

	// Invalidation
	invalidate: (invalidation: CacheInvalidation) => void;
	invalidateByPrefix: (prefix: string) => void;
	invalidateByPattern: (pattern: RegExp) => void;

	// Memory management
	evictLRU: (targetBytes?: number) => number;
	evictExpired: () => number;
	getStats: () => CacheStats;

	// Configuration
	configure: (config: Partial<CacheConfig>) => void;
}

interface CacheSetOptions {
	/** Time-to-live in milliseconds */
	ttl?: number;
	/** Estimated size in bytes (auto-calculated if not provided) */
	sizeBytes?: number;
}

interface CacheConfig {
	/** Maximum cache size in bytes (default: 100MB) */
	maxSizeBytes: number;
	/** Default TTL in milliseconds (default: 5 minutes) */
	defaultTtlMs: number;
	/** Maximum number of entries (default: 10000) */
	maxEntries: number;
}

// ============================================================================
// Constants
// ============================================================================

const DEFAULT_MAX_SIZE_BYTES = 100 * 1024 * 1024; // 100MB
const DEFAULT_TTL_MS = 5 * 60 * 1000; // 5 minutes
const DEFAULT_MAX_ENTRIES = 10000;

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Estimate the size of a JavaScript value in bytes
 */
function estimateSize(value: unknown): number {
	if (value === null || value === undefined) return 0;

	switch (typeof value) {
		case 'boolean':
			return 4;
		case 'number':
			return 8;
		case 'string':
			return (value as string).length * 2; // UTF-16
		case 'object':
			if (Array.isArray(value)) {
				return value.reduce((sum, item) => sum + estimateSize(item), 0) + 8;
			}
			return Object.entries(value as Record<string, unknown>).reduce(
				(sum, [key, val]) => sum + key.length * 2 + estimateSize(val),
				8
			);
		default:
			return 0;
	}
}

/**
 * Generate cache key for data chunks
 */
function chunkKey(dataType: string, cursor?: string): string {
	return cursor ? `chunk:${dataType}:${cursor}` : `chunk:${dataType}:initial`;
}

/**
 * Check if cache key matches entity type for invalidation
 */
function keyMatchesEntityType(key: string, entityType: string): boolean {
	const prefixMap: Record<string, string[]> = {
		dat: ['dat:', 'chunk:dats:'],
		game: ['game:', 'chunk:games:'],
		rom: ['rom:', 'chunk:roms:'],
		scan: ['scan:'],
		all: [''], // matches everything
	};

	const prefixes = prefixMap[entityType] || [];
	return prefixes.some(prefix => prefix === '' || key.startsWith(prefix));
}

// ============================================================================
// Store Implementation
// ============================================================================

export const useCacheStore = create<CacheState>()(
	devtools(
		(set, get) => ({
			// Initial state
			entries: new Map(),
			cursorStates: new Map(),
			maxSizeBytes: DEFAULT_MAX_SIZE_BYTES,
			defaultTtlMs: DEFAULT_TTL_MS,
			maxEntries: DEFAULT_MAX_ENTRIES,
			stats: {
				entryCount: 0,
				totalSizeBytes: 0,
				hits: 0,
				misses: 0,
				evictions: 0,
				hitRate: 0,
			},

			// ============================================================
			// Basic Cache Operations
			// ============================================================

			get: <T>(key: string): T | undefined => {
				const state = get();
				const entry = state.entries.get(key);

				if (!entry) {
					set(s => ({
						stats: {
							...s.stats,
							misses: s.stats.misses + 1,
							hitRate: s.stats.hits / (s.stats.hits + s.stats.misses + 1),
						},
					}));
					return undefined;
				}

				// Check expiration
				const now = Date.now();
				if (entry.ttl > 0 && now - entry.createdAt > entry.ttl) {
					state.delete(key);
					set(s => ({
						stats: {
							...s.stats,
							misses: s.stats.misses + 1,
							hitRate: s.stats.hits / (s.stats.hits + s.stats.misses + 1),
						},
					}));
					return undefined;
				}

				// Update access time and count
				entry.lastAccessedAt = now;
				entry.accessCount++;

				set(s => ({
					stats: {
						...s.stats,
						hits: s.stats.hits + 1,
						hitRate: (s.stats.hits + 1) / (s.stats.hits + s.stats.misses + 1),
					},
				}));

				return entry.data as T;
			},

			set: <T>(key: string, data: T, options?: CacheSetOptions) => {
				const state = get();
				const now = Date.now();
				const sizeBytes = options?.sizeBytes ?? estimateSize(data);
				const ttl = options?.ttl ?? state.defaultTtlMs;

				// Evict if necessary to make room
				const currentSize = state.stats.totalSizeBytes;
				const existingEntry = state.entries.get(key);
				const existingSize = existingEntry?.sizeBytes ?? 0;
				const newTotalSize = currentSize - existingSize + sizeBytes;

				if (newTotalSize > state.maxSizeBytes) {
					state.evictLRU(newTotalSize - state.maxSizeBytes);
				}

				// Evict if too many entries
				if (!existingEntry && state.entries.size >= state.maxEntries) {
					state.evictLRU();
				}

				const entry: CacheEntry<T> = {
					data,
					createdAt: now,
					lastAccessedAt: now,
					ttl,
					sizeBytes,
					accessCount: 0,
				};

				set(s => {
					const newEntries = new Map(s.entries);
					newEntries.set(key, entry as CacheEntry);
					return {
						entries: newEntries,
						stats: {
							...s.stats,
							entryCount: newEntries.size,
							totalSizeBytes: s.stats.totalSizeBytes - existingSize + sizeBytes,
						},
					};
				});
			},

			has: (key: string): boolean => {
				const entry = get().entries.get(key);
				if (!entry) return false;

				// Check expiration
				if (entry.ttl > 0 && Date.now() - entry.createdAt > entry.ttl) {
					get().delete(key);
					return false;
				}

				return true;
			},

			delete: (key: string) => {
				set(s => {
					const entry = s.entries.get(key);
					if (!entry) return s;

					const newEntries = new Map(s.entries);
					newEntries.delete(key);

					return {
						entries: newEntries,
						stats: {
							...s.stats,
							entryCount: newEntries.size,
							totalSizeBytes: s.stats.totalSizeBytes - entry.sizeBytes,
						},
					};
				});
			},

			clear: () => {
				set({
					entries: new Map(),
					cursorStates: new Map(),
					stats: {
						entryCount: 0,
						totalSizeBytes: 0,
						hits: 0,
						misses: 0,
						evictions: 0,
						hitRate: 0,
					},
				});
			},

			// ============================================================
			// Cursor State Management
			// ============================================================

			getCursorState: (key: string): CursorState | undefined => {
				return get().cursorStates.get(key);
			},

			setCursorState: (key: string, state: CursorState) => {
				set(s => {
					const newCursorStates = new Map(s.cursorStates);
					newCursorStates.set(key, state);
					return { cursorStates: newCursorStates };
				});
			},

			// ============================================================
			// Chunk Operations (for virtual scrolling)
			// ============================================================

			setChunk: <T>(dataType: string, items: T[], cursor: string | null, response: DataChunkResponse) => {
				const state = get();
				const key = chunkKey(dataType, cursor ?? undefined);

				// Store the chunk data
				state.set(key, items);

				// Update cursor state
				state.setCursorState(dataType, {
					nextCursor: response.nextCursor,
					hasMore: response.hasMore,
					totalCount: response.totalCount,
				});
			},

			getChunk: <T>(dataType: string, cursor?: string): T[] | undefined => {
				const key = chunkKey(dataType, cursor);
				return get().get<T[]>(key);
			},

			// ============================================================
			// Invalidation
			// ============================================================

			invalidate: (invalidation: CacheInvalidation) => {
				const state = get();

				// Invalidate by specific cache keys
				if (invalidation.cacheKeys?.length) {
					invalidation.cacheKeys.forEach(key => state.delete(key));
				}

				// Invalidate by entity type
				if (invalidation.entityType) {
					const keysToDelete: string[] = [];

					state.entries.forEach((_, key) => {
						if (keyMatchesEntityType(key, invalidation.entityType)) {
							// If specific IDs provided, only delete matching
							if (invalidation.entityIds?.length) {
								const keyHasId = invalidation.entityIds.some(id =>
									key.includes(`:${id}`) || key.endsWith(`:${id}`)
								);
								if (keyHasId) keysToDelete.push(key);
							} else {
								keysToDelete.push(key);
							}
						}
					});

					keysToDelete.forEach(key => state.delete(key));

					// Clear cursor states for affected data types
					if (invalidation.entityType === 'all') {
						set({ cursorStates: new Map() });
					} else {
						const cursorKey = invalidation.entityType === 'dat' ? 'dats'
							: invalidation.entityType === 'game' ? 'games'
							: invalidation.entityType === 'rom' ? 'roms'
							: invalidation.entityType;

						set(s => {
							const newCursorStates = new Map(s.cursorStates);
							newCursorStates.delete(cursorKey);
							return { cursorStates: newCursorStates };
						});
					}
				}
			},

			invalidateByPrefix: (prefix: string) => {
				const state = get();
				const keysToDelete: string[] = [];

				state.entries.forEach((_, key) => {
					if (key.startsWith(prefix)) {
						keysToDelete.push(key);
					}
				});

				keysToDelete.forEach(key => state.delete(key));
			},

			invalidateByPattern: (pattern: RegExp) => {
				const state = get();
				const keysToDelete: string[] = [];

				state.entries.forEach((_, key) => {
					if (pattern.test(key)) {
						keysToDelete.push(key);
					}
				});

				keysToDelete.forEach(key => state.delete(key));
			},

			// ============================================================
			// Memory Management
			// ============================================================

			evictLRU: (targetBytes?: number): number => {
				const state = get();
				const entries = Array.from(state.entries.entries());

				// Sort by last accessed time (oldest first), then by access count
				entries.sort((a, b) => {
					const timeA = a[1].lastAccessedAt;
					const timeB = b[1].lastAccessedAt;
					if (timeA !== timeB) return timeA - timeB;
					return a[1].accessCount - b[1].accessCount;
				});

				let bytesEvicted = 0;
				let evictionCount = 0;
				const targetEviction = targetBytes ?? entries[0]?.[1].sizeBytes ?? 0;

				for (const [key, entry] of entries) {
					if (bytesEvicted >= targetEviction) break;

					state.delete(key);
					bytesEvicted += entry.sizeBytes;
					evictionCount++;
				}

				set(s => ({
					stats: {
						...s.stats,
						evictions: s.stats.evictions + evictionCount,
					},
				}));

				return evictionCount;
			},

			evictExpired: (): number => {
				const state = get();
				const now = Date.now();
				const keysToDelete: string[] = [];

				state.entries.forEach((entry, key) => {
					if (entry.ttl > 0 && now - entry.createdAt > entry.ttl) {
						keysToDelete.push(key);
					}
				});

				keysToDelete.forEach(key => state.delete(key));

				if (keysToDelete.length > 0) {
					set(s => ({
						stats: {
							...s.stats,
							evictions: s.stats.evictions + keysToDelete.length,
						},
					}));
				}

				return keysToDelete.length;
			},

			getStats: (): CacheStats => {
				return get().stats;
			},

			// ============================================================
			// Configuration
			// ============================================================

			configure: (config: Partial<CacheConfig>) => {
				set(s => ({
					maxSizeBytes: config.maxSizeBytes ?? s.maxSizeBytes,
					defaultTtlMs: config.defaultTtlMs ?? s.defaultTtlMs,
					maxEntries: config.maxEntries ?? s.maxEntries,
				}));

				// Evict if new limits are exceeded
				const state = get();
				if (state.stats.totalSizeBytes > state.maxSizeBytes) {
					state.evictLRU(state.stats.totalSizeBytes - state.maxSizeBytes);
				}
				if (state.entries.size > state.maxEntries) {
					state.evictLRU();
				}
			},
		}),
		{ name: 'cache-store' }
	)
);

// ============================================================================
// Cache Integration Hook
// ============================================================================

/**
 * Hook to integrate cache with SignalR invalidation events
 */
export function useCacheInvalidation(onCacheInvalidation?: (event: CacheInvalidation) => void) {
	const invalidate = useCacheStore(state => state.invalidate);

	return (event: CacheInvalidation) => {
		invalidate(event);
		onCacheInvalidation?.(event);
	};
}

// ============================================================================
// Periodic Cleanup
// ============================================================================

let cleanupInterval: ReturnType<typeof setInterval> | null = null;

/**
 * Start periodic cache cleanup (call once at app startup)
 */
export function startCacheCleanup(intervalMs: number = 60000): void {
	if (cleanupInterval) return;

	cleanupInterval = setInterval(() => {
		useCacheStore.getState().evictExpired();
	}, intervalMs);
}

/**
 * Stop periodic cache cleanup
 */
export function stopCacheCleanup(): void {
	if (cleanupInterval) {
		clearInterval(cleanupInterval);
		cleanupInterval = null;
	}
}

// ============================================================================
// Selectors
// ============================================================================

export const selectCacheStats = (state: CacheState) => state.stats;
export const selectCacheSize = (state: CacheState) => state.stats.totalSizeBytes;
export const selectCacheEntryCount = (state: CacheState) => state.stats.entryCount;
export const selectCacheHitRate = (state: CacheState) => state.stats.hitRate;
