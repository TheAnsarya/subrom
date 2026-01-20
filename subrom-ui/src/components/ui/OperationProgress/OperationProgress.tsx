import { useMemo } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faSpinner,
	faCheck,
	faTimes,
	faPause,
	faClock,
	faFileImport,
	faHdd,
	faFingerprint,
} from '@fortawesome/free-solid-svg-icons';
import { ProgressBar } from '../ProgressBar';
import {
	calculateProgressPercent,
	formatTimeRemaining,
	formatThroughput,
	formatFileSize,
} from '@/hooks/useSignalR';
import type {
	BatchProgress,
	DatImportProgress,
	RomScanProgress,
	FileHashProgress,
	OperationStatus,
} from '@/types/signalr';
import styles from './OperationProgress.module.css';

// ============================================================================
// Types
// ============================================================================

export type OperationType = 'dat-import' | 'rom-scan' | 'file-hash' | 'generic';

export interface OperationProgressProps {
	/** Type of operation for icon/styling */
	type: OperationType;
	/** Operation title */
	title: string;
	/** Operation status */
	status: OperationStatus['status'];
	/** Progress data (generic batch progress) */
	progress?: BatchProgress;
	/** DAT import specific progress */
	datImportProgress?: DatImportProgress;
	/** ROM scan specific progress */
	romScanProgress?: RomScanProgress;
	/** File hash specific progress */
	fileHashProgress?: FileHashProgress;
	/** Error message if failed */
	errorMessage?: string;
	/** Cancel handler */
	onCancel?: () => void;
	/** Retry handler */
	onRetry?: () => void;
	/** Dismiss handler */
	onDismiss?: () => void;
	/** Show detailed stats */
	showDetails?: boolean;
	/** Compact mode */
	compact?: boolean;
}

// ============================================================================
// Helper Functions
// ============================================================================

function getStatusIcon(status: OperationStatus['status']) {
	switch (status) {
		case 'pending':
			return faClock;
		case 'running':
			return faSpinner;
		case 'paused':
			return faPause;
		case 'completed':
			return faCheck;
		case 'failed':
		case 'cancelled':
			return faTimes;
		default:
			return faSpinner;
	}
}

function getTypeIcon(type: OperationType) {
	switch (type) {
		case 'dat-import':
			return faFileImport;
		case 'rom-scan':
			return faHdd;
		case 'file-hash':
			return faFingerprint;
		default:
			return faSpinner;
	}
}

function getStatusClass(status: OperationStatus['status']): string {
	switch (status) {
		case 'completed':
			return styles.statusCompleted;
		case 'failed':
			return styles.statusFailed;
		case 'cancelled':
			return styles.statusCancelled;
		case 'paused':
			return styles.statusPaused;
		default:
			return '';
	}
}

function getProgressVariant(status: OperationStatus['status']): 'primary' | 'success' | 'danger' | 'warning' {
	switch (status) {
		case 'completed':
			return 'success';
		case 'failed':
		case 'cancelled':
			return 'danger';
		case 'paused':
			return 'warning';
		default:
			return 'primary';
	}
}

// ============================================================================
// Main Component
// ============================================================================

export function OperationProgress({
	type,
	title,
	status,
	progress,
	datImportProgress,
	romScanProgress,
	fileHashProgress,
	errorMessage,
	onCancel,
	onRetry,
	onDismiss,
	showDetails = true,
	compact = false,
}: OperationProgressProps) {
	// Calculate progress percentage
	const percent = useMemo(() => {
		if (fileHashProgress) {
			return fileHashProgress.percentComplete;
		}
		if (progress) {
			return calculateProgressPercent(progress);
		}
		if (datImportProgress) {
			return calculateProgressPercent(datImportProgress);
		}
		if (romScanProgress) {
			return calculateProgressPercent(romScanProgress);
		}
		return 0;
	}, [progress, datImportProgress, romScanProgress, fileHashProgress]);

	// Get current item being processed
	const currentItem = useMemo(() => {
		if (datImportProgress?.currentGameName) {
			return datImportProgress.currentGameName;
		}
		if (romScanProgress?.currentFile) {
			return romScanProgress.currentFile;
		}
		if (fileHashProgress?.filePath) {
			return fileHashProgress.filePath.split(/[/\\]/).pop();
		}
		return undefined;
	}, [datImportProgress, romScanProgress, fileHashProgress]);

	// Get throughput info
	const throughput = useMemo(() => {
		if (fileHashProgress?.bytesPerSecond) {
			return formatThroughput(fileHashProgress.bytesPerSecond);
		}
		if (progress?.itemsPerSecond) {
			return `${progress.itemsPerSecond.toFixed(1)} items/s`;
		}
		return undefined;
	}, [progress, fileHashProgress]);

	// Get time remaining
	const timeRemaining = useMemo(() => {
		const seconds = fileHashProgress?.estimatedSecondsRemaining
			?? progress?.estimatedSecondsRemaining
			?? datImportProgress?.estimatedSecondsRemaining
			?? romScanProgress?.estimatedSecondsRemaining;
		return formatTimeRemaining(seconds);
	}, [progress, datImportProgress, romScanProgress, fileHashProgress]);

	// Determine if spinning
	const isSpinning = status === 'running';

	return (
		<div className={`${styles.container} ${compact ? styles.compact : ''} ${getStatusClass(status)}`}>
			{/* Header */}
			<div className={styles.header}>
				<div className={styles.iconContainer}>
					<FontAwesomeIcon
						icon={getTypeIcon(type)}
						className={styles.typeIcon}
					/>
					<FontAwesomeIcon
						icon={getStatusIcon(status)}
						spin={isSpinning}
						className={styles.statusIcon}
					/>
				</div>
				<div className={styles.titleContainer}>
					<span className={styles.title}>{title}</span>
					{currentItem && !compact && (
						<span className={styles.currentItem} title={currentItem}>
							{currentItem}
						</span>
					)}
				</div>
				<div className={styles.actions}>
					{status === 'running' && onCancel && (
						<button className={styles.actionButton} onClick={onCancel} title="Cancel">
							<FontAwesomeIcon icon={faTimes} />
						</button>
					)}
					{status === 'failed' && onRetry && (
						<button className={styles.actionButton} onClick={onRetry} title="Retry">
							Retry
						</button>
					)}
					{(status === 'completed' || status === 'failed' || status === 'cancelled') && onDismiss && (
						<button className={styles.actionButton} onClick={onDismiss} title="Dismiss">
							<FontAwesomeIcon icon={faTimes} />
						</button>
					)}
				</div>
			</div>

			{/* Progress Bar */}
			<div className={styles.progressContainer}>
				<ProgressBar
					value={percent}
					variant={getProgressVariant(status)}
					animated={status === 'running'}
					showValue={!compact}
					size={compact ? 'small' : 'medium'}
				/>
			</div>

			{/* Details */}
			{showDetails && !compact && (
				<div className={styles.details}>
					{/* Item counts */}
					{progress && (
						<span className={styles.stat}>
							{progress.totalItemsProcessed.toLocaleString()} / {progress.totalItems.toLocaleString()} items
						</span>
					)}

					{/* Batch info */}
					{progress && progress.totalBatches > 1 && (
						<span className={styles.stat}>
							Batch {progress.currentBatch} / {progress.totalBatches}
						</span>
					)}

					{/* DAT-specific stats */}
					{datImportProgress && (
						<>
							<span className={styles.stat}>
								{datImportProgress.gamesParsed.toLocaleString()} games
							</span>
							<span className={styles.stat}>
								{datImportProgress.romsParsed.toLocaleString()} ROMs
							</span>
							{datImportProgress.parseErrors > 0 && (
								<span className={`${styles.stat} ${styles.error}`}>
									{datImportProgress.parseErrors} errors
								</span>
							)}
							<span className={styles.stat}>
								Phase: {datImportProgress.phase}
							</span>
						</>
					)}

					{/* ROM scan-specific stats */}
					{romScanProgress && (
						<>
							<span className={styles.stat}>
								Phase: {romScanProgress.phase}
							</span>
							{romScanProgress.currentFileSize && (
								<span className={styles.stat}>
									{formatFileSize(romScanProgress.currentFileSize)}
								</span>
							)}
							{romScanProgress.currentHashAlgorithm && (
								<span className={styles.stat}>
									{romScanProgress.currentHashAlgorithm.toUpperCase()}
								</span>
							)}
						</>
					)}

					{/* File hash-specific stats */}
					{fileHashProgress && (
						<span className={styles.stat}>
							{formatFileSize(fileHashProgress.bytesProcessed)} / {formatFileSize(fileHashProgress.totalBytes)}
						</span>
					)}

					{/* Throughput */}
					{throughput && status === 'running' && (
						<span className={styles.stat}>{throughput}</span>
					)}

					{/* Time remaining */}
					{status === 'running' && timeRemaining && (
						<span className={styles.stat}>ETA: {timeRemaining}</span>
					)}

					{/* Error message */}
					{errorMessage && (
						<span className={`${styles.stat} ${styles.error}`}>{errorMessage}</span>
					)}
				</div>
			)}
		</div>
	);
}
