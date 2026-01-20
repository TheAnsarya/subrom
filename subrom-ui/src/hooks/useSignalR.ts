import { useEffect, useRef, useState, useCallback } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState, LogLevel } from '@microsoft/signalr';
import type { ScanProgress } from '@/types/api';
import type {
	SignalREventMap,
	SignalREventName,
	DatImportStarted,
	DatImportProgress,
	DatImportCompleted,
	DatImportError,
	RomScanStarted,
	RomScanProgress,
	RomScanFileCompleted,
	RomScanCompleted,
	RomScanError,
	FileHashStarted,
	FileHashProgress,
	FileHashCompleted,
	CacheInvalidation,
	DataChunkResponse,
	ConnectionInfo,
} from '@/types/signalr';

const SIGNALR_URL = '/hubs/scan';

// ============================================================================
// Legacy Options Interface (backward compatible)
// ============================================================================

export interface UseSignalROptions {
	// Legacy scan events
	onScanProgress?: (progress: ScanProgress) => void;
	onScanStarted?: () => void;
	onScanCompleted?: () => void;
	onScanError?: (error: string) => void;

	// DAT Import events
	onDatImportStarted?: (event: DatImportStarted) => void;
	onDatImportProgress?: (event: DatImportProgress) => void;
	onDatImportCompleted?: (event: DatImportCompleted) => void;
	onDatImportError?: (event: DatImportError) => void;

	// ROM Scan events
	onRomScanStarted?: (event: RomScanStarted) => void;
	onRomScanProgress?: (event: RomScanProgress) => void;
	onRomScanFileCompleted?: (event: RomScanFileCompleted) => void;
	onRomScanCompleted?: (event: RomScanCompleted) => void;
	onRomScanError?: (event: RomScanError) => void;

	// File Hashing events (for large files)
	onFileHashStarted?: (event: FileHashStarted) => void;
	onFileHashProgress?: (event: FileHashProgress) => void;
	onFileHashCompleted?: (event: FileHashCompleted) => void;

	// Cache events
	onCacheInvalidation?: (event: CacheInvalidation) => void;

	// Data streaming events
	onDataChunk?: (event: DataChunkResponse) => void;

	// Connection events
	onConnected?: (info: ConnectionInfo) => void;
	onDisconnected?: () => void;
	onReconnecting?: () => void;
	onReconnected?: () => void;

	autoConnect?: boolean;
}

// ============================================================================
// Result Interface
// ============================================================================

export interface UseSignalRResult {
	connection: HubConnection | null;
	connectionState: HubConnectionState;
	connect: () => Promise<void>;
	disconnect: () => Promise<void>;
	isConnected: boolean;

	// Methods for requesting data
	requestDataChunk: (dataType: 'games' | 'roms' | 'dats', cursor?: string, limit?: number) => Promise<void>;

	// Subscribe to specific operation updates
	subscribeToOperation: (operationId: string) => Promise<void>;
	unsubscribeFromOperation: (operationId: string) => Promise<void>;

	// Cancel operations
	cancelOperation: (operationId: string) => Promise<void>;
}

// ============================================================================
// Hook Implementation
// ============================================================================

export function useSignalR(options: UseSignalROptions = {}): UseSignalRResult {
	const { autoConnect = true } = options;

	const [connectionState, setConnectionState] = useState<HubConnectionState>(
		HubConnectionState.Disconnected
	);
	const connectionRef = useRef<HubConnection | null>(null);
	const optionsRef = useRef(options);
	optionsRef.current = options;

	// Build connection once
	useEffect(() => {
		const connection = new HubConnectionBuilder()
			.withUrl(SIGNALR_URL)
			.withAutomaticReconnect({
				nextRetryDelayInMilliseconds: (retryContext) => {
					// Exponential backoff: 0, 2s, 4s, 8s, max 30s
					if (retryContext.previousRetryCount >= 10) {
						return null; // Stop retrying after 10 attempts
					}
					return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
				},
			})
			.configureLogging(LogLevel.Warning)
			.build();

		connectionRef.current = connection;

		// Connection state handlers
		connection.onreconnecting(() => {
			setConnectionState(HubConnectionState.Reconnecting);
			optionsRef.current.onReconnecting?.();
		});

		connection.onreconnected(() => {
			setConnectionState(HubConnectionState.Connected);
			optionsRef.current.onReconnected?.();
		});

		connection.onclose(() => {
			setConnectionState(HubConnectionState.Disconnected);
			optionsRef.current.onDisconnected?.();
		});

		// ============================================================
		// Register Legacy Event Handlers
		// ============================================================

		connection.on('ScanProgress', (progress: ScanProgress) => {
			optionsRef.current.onScanProgress?.(progress);
		});

		connection.on('ScanStarted', () => {
			optionsRef.current.onScanStarted?.();
		});

		connection.on('ScanCompleted', () => {
			optionsRef.current.onScanCompleted?.();
		});

		connection.on('ScanError', (error: string) => {
			optionsRef.current.onScanError?.(error);
		});

		// ============================================================
		// DAT Import Event Handlers
		// ============================================================

		connection.on('DatImportStarted', (event: DatImportStarted) => {
			optionsRef.current.onDatImportStarted?.(event);
		});

		connection.on('DatImportProgress', (event: DatImportProgress) => {
			optionsRef.current.onDatImportProgress?.(event);
		});

		connection.on('DatImportCompleted', (event: DatImportCompleted) => {
			optionsRef.current.onDatImportCompleted?.(event);
		});

		connection.on('DatImportError', (event: DatImportError) => {
			optionsRef.current.onDatImportError?.(event);
		});

		// ============================================================
		// ROM Scan Event Handlers
		// ============================================================

		connection.on('RomScanStarted', (event: RomScanStarted) => {
			optionsRef.current.onRomScanStarted?.(event);
		});

		connection.on('RomScanProgress', (event: RomScanProgress) => {
			optionsRef.current.onRomScanProgress?.(event);
		});

		connection.on('RomScanFileCompleted', (event: RomScanFileCompleted) => {
			optionsRef.current.onRomScanFileCompleted?.(event);
		});

		connection.on('RomScanCompleted', (event: RomScanCompleted) => {
			optionsRef.current.onRomScanCompleted?.(event);
		});

		connection.on('RomScanError', (event: RomScanError) => {
			optionsRef.current.onRomScanError?.(event);
		});

		// ============================================================
		// File Hashing Event Handlers (for large files)
		// ============================================================

		connection.on('FileHashStarted', (event: FileHashStarted) => {
			optionsRef.current.onFileHashStarted?.(event);
		});

		connection.on('FileHashProgress', (event: FileHashProgress) => {
			optionsRef.current.onFileHashProgress?.(event);
		});

		connection.on('FileHashCompleted', (event: FileHashCompleted) => {
			optionsRef.current.onFileHashCompleted?.(event);
		});

		// ============================================================
		// Cache Event Handlers
		// ============================================================

		connection.on('CacheInvalidation', (event: CacheInvalidation) => {
			optionsRef.current.onCacheInvalidation?.(event);
		});

		// ============================================================
		// Data Streaming Event Handlers
		// ============================================================

		connection.on('DataChunk', (event: DataChunkResponse) => {
			optionsRef.current.onDataChunk?.(event);
		});

		// ============================================================
		// Connection Info Handler
		// ============================================================

		connection.on('Connected', (info: ConnectionInfo) => {
			optionsRef.current.onConnected?.(info);
		});

		return () => {
			connection.stop();
		};
	}, []);

	// ============================================================
	// Connection Methods
	// ============================================================

	const connect = useCallback(async () => {
		const connection = connectionRef.current;
		if (!connection || connection.state === HubConnectionState.Connected) {
			return;
		}

		try {
			setConnectionState(HubConnectionState.Connecting);
			await connection.start();
			setConnectionState(HubConnectionState.Connected);
		} catch (error) {
			console.error('SignalR connection failed:', error);
			setConnectionState(HubConnectionState.Disconnected);
		}
	}, []);

	const disconnect = useCallback(async () => {
		const connection = connectionRef.current;
		if (!connection || connection.state !== HubConnectionState.Connected) {
			return;
		}

		try {
			await connection.stop();
			setConnectionState(HubConnectionState.Disconnected);
		} catch (error) {
			console.error('SignalR disconnect failed:', error);
		}
	}, []);

	// ============================================================
	// Data Streaming Methods
	// ============================================================

	const requestDataChunk = useCallback(async (
		dataType: 'games' | 'roms' | 'dats',
		cursor?: string,
		limit: number = 100
	) => {
		const connection = connectionRef.current;
		if (!connection || connection.state !== HubConnectionState.Connected) {
			console.warn('Cannot request data chunk: not connected');
			return;
		}

		try {
			await connection.invoke('RequestDataChunk', { dataType, cursor, limit });
		} catch (error) {
			console.error('Failed to request data chunk:', error);
		}
	}, []);

	// ============================================================
	// Operation Subscription Methods
	// ============================================================

	const subscribeToOperation = useCallback(async (operationId: string) => {
		const connection = connectionRef.current;
		if (!connection || connection.state !== HubConnectionState.Connected) {
			console.warn('Cannot subscribe to operation: not connected');
			return;
		}

		try {
			await connection.invoke('SubscribeToOperation', operationId);
		} catch (error) {
			console.error('Failed to subscribe to operation:', error);
		}
	}, []);

	const unsubscribeFromOperation = useCallback(async (operationId: string) => {
		const connection = connectionRef.current;
		if (!connection || connection.state !== HubConnectionState.Connected) {
			console.warn('Cannot unsubscribe from operation: not connected');
			return;
		}

		try {
			await connection.invoke('UnsubscribeFromOperation', operationId);
		} catch (error) {
			console.error('Failed to unsubscribe from operation:', error);
		}
	}, []);

	// ============================================================
	// Operation Control Methods
	// ============================================================

	const cancelOperation = useCallback(async (operationId: string) => {
		const connection = connectionRef.current;
		if (!connection || connection.state !== HubConnectionState.Connected) {
			console.warn('Cannot cancel operation: not connected');
			return;
		}

		try {
			await connection.invoke('CancelOperation', operationId);
		} catch (error) {
			console.error('Failed to cancel operation:', error);
		}
	}, []);

	// Auto-connect on mount if enabled
	useEffect(() => {
		if (autoConnect) {
			connect();
		}
	}, [autoConnect, connect]);

	return {
		connection: connectionRef.current,
		connectionState,
		connect,
		disconnect,
		isConnected: connectionState === HubConnectionState.Connected,
		requestDataChunk,
		subscribeToOperation,
		unsubscribeFromOperation,
		cancelOperation,
	};
}

// ============================================================================
// Global Connection Singleton
// ============================================================================

let globalConnection: HubConnection | null = null;

export function getGlobalSignalRConnection(): HubConnection {
	if (!globalConnection) {
		globalConnection = new HubConnectionBuilder()
			.withUrl(SIGNALR_URL)
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Warning)
			.build();
	}
	return globalConnection;
}

// ============================================================================
// Type-Safe Event Subscription Hook
// ============================================================================

/**
 * Hook for subscribing to specific SignalR events with type safety
 */
export function useSignalREvent<T extends SignalREventName>(
	connection: HubConnection | null,
	eventName: T,
	handler: (payload: SignalREventMap[T]) => void
): void {
	const handlerRef = useRef(handler);
	handlerRef.current = handler;

	useEffect(() => {
		if (!connection) return;

		const wrappedHandler = (payload: SignalREventMap[T]) => {
			handlerRef.current(payload);
		};

		connection.on(eventName, wrappedHandler);

		return () => {
			connection.off(eventName, wrappedHandler);
		};
	}, [connection, eventName]);
}

// ============================================================================
// Progress Calculation Utilities
// ============================================================================

/**
 * Calculate overall progress percentage from batch progress
 */
export function calculateProgressPercent(progress: {
	totalItemsProcessed: number;
	totalItems: number;
}): number {
	if (progress.totalItems === 0) return 0;
	return Math.round((progress.totalItemsProcessed / progress.totalItems) * 100);
}

/**
 * Format estimated time remaining as human-readable string
 */
export function formatTimeRemaining(seconds?: number): string {
	if (seconds === undefined || seconds < 0) return 'Calculating...';
	if (seconds < 60) return `${Math.round(seconds)}s`;
	if (seconds < 3600) {
		const mins = Math.floor(seconds / 60);
		const secs = Math.round(seconds % 60);
		return `${mins}m ${secs}s`;
	}
	const hours = Math.floor(seconds / 3600);
	const mins = Math.round((seconds % 3600) / 60);
	return `${hours}h ${mins}m`;
}

/**
 * Format bytes per second as human-readable throughput
 */
export function formatThroughput(bytesPerSecond?: number): string {
	if (bytesPerSecond === undefined || bytesPerSecond < 0) return '';
	if (bytesPerSecond < 1024) return `${bytesPerSecond.toFixed(1)} B/s`;
	if (bytesPerSecond < 1024 * 1024) return `${(bytesPerSecond / 1024).toFixed(1)} KB/s`;
	if (bytesPerSecond < 1024 * 1024 * 1024) return `${(bytesPerSecond / 1024 / 1024).toFixed(1)} MB/s`;
	return `${(bytesPerSecond / 1024 / 1024 / 1024).toFixed(2)} GB/s`;
}

/**
 * Format file size as human-readable string
 */
export function formatFileSize(bytes: number): string {
	if (bytes < 1024) return `${bytes} B`;
	if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
	if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
	return `${(bytes / 1024 / 1024 / 1024).toFixed(2)} GB`;
}
