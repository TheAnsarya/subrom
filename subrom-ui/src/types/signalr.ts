/**
 * SignalR event types for streaming large datasets
 * Supports DAT imports with 60K+ entries and ROM collections with GB of files
 */

// ============================================================================
// Base Progress Types
// ============================================================================

export interface BatchProgress {
	/** Unique identifier for this operation */
	operationId: string;
	/** Current batch number (1-indexed) */
	currentBatch: number;
	/** Total number of batches */
	totalBatches: number;
	/** Items processed in current batch */
	batchItemsProcessed: number;
	/** Total items in current batch */
	batchItemsTotal: number;
	/** Overall items processed */
	totalItemsProcessed: number;
	/** Overall total items */
	totalItems: number;
	/** Bytes processed (for file operations) */
	bytesProcessed?: number;
	/** Total bytes (for file operations) */
	totalBytes?: number;
	/** Processing rate (items per second) */
	itemsPerSecond?: number;
	/** Estimated time remaining in seconds */
	estimatedSecondsRemaining?: number;
}

export interface OperationStatus {
	operationId: string;
	status: 'pending' | 'running' | 'paused' | 'completed' | 'failed' | 'cancelled';
	startedAt?: string;
	completedAt?: string;
	errorMessage?: string;
}

// ============================================================================
// DAT Import Events
// ============================================================================

export interface DatImportStarted {
	operationId: string;
	fileName: string;
	fileSize: number;
	estimatedGameCount?: number;
}

export interface DatImportProgress extends BatchProgress {
	/** Name of the DAT file being imported */
	datFileName: string;
	/** Current phase of import */
	phase: 'parsing' | 'validating' | 'storing' | 'indexing';
	/** Current game being processed */
	currentGameName?: string;
	/** Number of games parsed */
	gamesParsed: number;
	/** Number of ROMs parsed */
	romsParsed: number;
	/** Parse errors encountered */
	parseErrors: number;
}

export interface DatImportCompleted {
	operationId: string;
	datId: number;
	datFileName: string;
	gamesImported: number;
	romsImported: number;
	parseErrors: number;
	durationMs: number;
}

export interface DatImportError {
	operationId: string;
	datFileName: string;
	errorMessage: string;
	errorCode?: string;
	lineNumber?: number;
	recoverable: boolean;
}

// ============================================================================
// ROM Scan Events
// ============================================================================

export interface RomScanStarted {
	operationId: string;
	scanPath: string;
	estimatedFileCount?: number;
	estimatedTotalBytes?: number;
}

export interface RomScanProgress extends BatchProgress {
	/** Current phase of scan */
	phase: 'discovering' | 'hashing' | 'verifying' | 'updating';
	/** Current file being processed */
	currentFile?: string;
	/** Current file size */
	currentFileSize?: number;
	/** Hash algorithm currently running */
	currentHashAlgorithm?: 'crc32' | 'md5' | 'sha1';
	/** Hash progress for current file (0-100) */
	currentFileHashProgress?: number;
}

export interface RomScanFileCompleted {
	operationId: string;
	filePath: string;
	fileName: string;
	fileSize: number;
	crc32?: string;
	md5?: string;
	sha1?: string;
	matchStatus: 'matched' | 'unmatched' | 'error';
	matchedGameName?: string;
}

export interface RomScanCompleted {
	operationId: string;
	filesScanned: number;
	bytesProcessed: number;
	matchedFiles: number;
	unmatchedFiles: number;
	errorFiles: number;
	durationMs: number;
}

export interface RomScanError {
	operationId: string;
	filePath?: string;
	errorMessage: string;
	errorCode?: string;
	recoverable: boolean;
}

// ============================================================================
// Large File Hashing Events (for GB-sized files)
// ============================================================================

export interface FileHashStarted {
	operationId: string;
	filePath: string;
	fileSize: number;
	algorithms: ('crc32' | 'md5' | 'sha1')[];
}

export interface FileHashProgress {
	operationId: string;
	filePath: string;
	bytesProcessed: number;
	totalBytes: number;
	percentComplete: number;
	bytesPerSecond: number;
	estimatedSecondsRemaining: number;
}

export interface FileHashCompleted {
	operationId: string;
	filePath: string;
	fileSize: number;
	crc32?: string;
	md5?: string;
	sha1?: string;
	durationMs: number;
}

// ============================================================================
// Cache Invalidation Events
// ============================================================================

export interface CacheInvalidation {
	/** Type of entity that changed */
	entityType: 'dat' | 'game' | 'rom' | 'scan' | 'all';
	/** Specific entity IDs to invalidate (empty = all of type) */
	entityIds?: number[];
	/** Cache keys to invalidate */
	cacheKeys?: string[];
	/** Reason for invalidation */
	reason: 'created' | 'updated' | 'deleted' | 'bulk-import' | 'scan-complete';
}

// ============================================================================
// Data Streaming Events (for virtual scrolling)
// ============================================================================

export interface DataChunkRequest {
	/** Type of data requested */
	dataType: 'games' | 'roms' | 'dats';
	/** Cursor for pagination (opaque string) */
	cursor?: string;
	/** Number of items to fetch */
	limit: number;
	/** Filter parameters */
	filters?: Record<string, unknown>;
	/** Sort parameters */
	sort?: { field: string; direction: 'asc' | 'desc' };
}

export interface DataChunkResponse<T = unknown> {
	/** Requested data type */
	dataType: 'games' | 'roms' | 'dats';
	/** Data items */
	items: T[];
	/** Cursor for next page (null if no more) */
	nextCursor: string | null;
	/** Total count (if known) */
	totalCount?: number;
	/** Whether more items exist */
	hasMore: boolean;
}

// ============================================================================
// Connection Events
// ============================================================================

export interface ConnectionInfo {
	connectionId: string;
	connectedAt: string;
	serverVersion: string;
}

// ============================================================================
// Event Map for Type Safety
// ============================================================================

export interface SignalREventMap {
	// Connection
	'Connected': ConnectionInfo;

	// DAT Import
	'DatImportStarted': DatImportStarted;
	'DatImportProgress': DatImportProgress;
	'DatImportCompleted': DatImportCompleted;
	'DatImportError': DatImportError;

	// ROM Scan
	'RomScanStarted': RomScanStarted;
	'RomScanProgress': RomScanProgress;
	'RomScanFileCompleted': RomScanFileCompleted;
	'RomScanCompleted': RomScanCompleted;
	'RomScanError': RomScanError;

	// File Hashing
	'FileHashStarted': FileHashStarted;
	'FileHashProgress': FileHashProgress;
	'FileHashCompleted': FileHashCompleted;

	// Cache
	'CacheInvalidation': CacheInvalidation;

	// Data Streaming
	'DataChunk': DataChunkResponse;

	// Legacy (backward compatibility)
	'ScanProgress': import('./api').ScanProgress;
	'ScanStarted': void;
	'ScanCompleted': void;
	'ScanError': string;
}

export type SignalREventName = keyof SignalREventMap;
export type SignalREventPayload<T extends SignalREventName> = SignalREventMap[T];
