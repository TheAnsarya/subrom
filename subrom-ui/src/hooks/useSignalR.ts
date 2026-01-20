import { useEffect, useRef, useState, useCallback } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState, LogLevel } from '@microsoft/signalr';
import type { ScanProgress } from '@/types/api';

const SIGNALR_URL = '/hubs/scan';

export interface UseSignalROptions {
	onScanProgress?: (progress: ScanProgress) => void;
	onScanStarted?: () => void;
	onScanCompleted?: () => void;
	onScanError?: (error: string) => void;
	autoConnect?: boolean;
}

export interface UseSignalRResult {
	connection: HubConnection | null;
	connectionState: HubConnectionState;
	connect: () => Promise<void>;
	disconnect: () => Promise<void>;
	isConnected: boolean;
}

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
		});

		connection.onreconnected(() => {
			setConnectionState(HubConnectionState.Connected);
		});

		connection.onclose(() => {
			setConnectionState(HubConnectionState.Disconnected);
		});

		// Register event handlers
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

		return () => {
			connection.stop();
		};
	}, []);

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
	};
}

// Global connection singleton for sharing across components
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
