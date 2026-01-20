import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faHdd,
	faPlus,
	faSync,
	faCheckCircle,
	faExclamationTriangle,
	faTimesCircle,
	faFolder,
	faFolderOpen,
	faTrash,
	faEllipsisV,
	faSpinner
} from '@fortawesome/free-solid-svg-icons';
import { useDrives, useCreateDrive, useRefreshDrive, useDeleteDrive, useCreateScan, type Drive } from '../../api';
import './DriveManager.css';

function formatBytes(bytes: number): string {
	if (bytes === 0) return '0 B';
	const k = 1024;
	const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

function formatDate(dateString: string | null): string {
	if (!dateString) return 'Never';
	return new Date(dateString).toLocaleString();
}

export default function DriveManager() {
	const { data: drives, isLoading, error, refetch } = useDrives();
	const createDrive = useCreateDrive();
	const refreshDrive = useRefreshDrive();
	const deleteDrive = useDeleteDrive();
	const createScan = useCreateScan();

	const [showAddModal, setShowAddModal] = useState(false);
	const [newDriveLabel, setNewDriveLabel] = useState('');
	const [newDrivePath, setNewDrivePath] = useState('');

	const handleAddDrive = () => {
		if (newDriveLabel.trim() && newDrivePath.trim()) {
			createDrive.mutate(
				{ label: newDriveLabel.trim(), path: newDrivePath.trim() },
				{
					onSuccess: () => {
						setShowAddModal(false);
						setNewDriveLabel('');
						setNewDrivePath('');
					},
				}
			);
		}
	};

	const handleScan = (drive: Drive) => {
		createScan.mutate({ rootPath: drive.path, driveId: drive.id, recursive: true, verifyHashes: true });
	};

	const handleDelete = (drive: Drive) => {
		if (window.confirm(`Are you sure you want to remove "${drive.label}"? ROM records will be preserved.`)) {
			deleteDrive.mutate(drive.id);
		}
	};

	const getStatusIcon = (drive: Drive) => {
		if (drive.isOnline) {
			return <FontAwesomeIcon icon={faCheckCircle} className="drive-status-icon online" />;
		}
		return <FontAwesomeIcon icon={faTimesCircle} className="drive-status-icon offline" />;
	};

	const getUsagePercent = (drive: Drive): number => {
		if (drive.totalCapacity === 0) return 0;
		const used = drive.totalCapacity - drive.freeSpace;
		return (used / drive.totalCapacity) * 100;
	};

	const offlineDrives = drives?.filter(d => !d.isOnline) ?? [];
	const offlineRomCount = offlineDrives.reduce((sum, d) => sum + d.romCount, 0);

	if (error) {
		return (
			<div className="drive-manager">
				<div className="error-message">
					<FontAwesomeIcon icon={faExclamationTriangle} />
					Failed to load drives: {error.message}
				</div>
			</div>
		);
	}

	return (
		<div className="drive-manager">
			<div className="page-header">
				<div className="header-left">
					<h1>Drive Manager</h1>
					<p className="page-subtitle">Manage your ROM storage locations</p>
				</div>
				<div className="header-actions">
					<button className="btn btn-secondary" onClick={() => refetch()} disabled={isLoading}>
						<FontAwesomeIcon icon={isLoading ? faSpinner : faSync} spin={isLoading} />
						Refresh All
					</button>
					<button className="btn btn-primary" onClick={() => setShowAddModal(true)}>
						<FontAwesomeIcon icon={faPlus} />
						Add Drive
					</button>
				</div>
			</div>

			<div className="important-notice">
				<FontAwesomeIcon icon={faExclamationTriangle} />
				<div className="notice-content">
					<strong>Offline Drive Protection</strong>
					<p>
						ROM records are never deleted when drives go offline. Your collection database 
						remains intact even if storage is temporarily unavailable. Reconnect drives 
						anytime to resume access.
					</p>
				</div>
			</div>

			{isLoading ? (
				<div className="loading-container">
					<FontAwesomeIcon icon={faSpinner} spin size="2x" />
					<p>Loading drives...</p>
				</div>
			) : drives && drives.length > 0 ? (
				<div className="drives-grid">
					{drives.map(drive => (
						<div key={drive.id} className={`drive-card ${drive.isOnline ? 'online' : 'offline'}`}>
							<div className="drive-header">
								<div className="drive-icon">
									<FontAwesomeIcon icon={faHdd} />
								</div>
								<div className="drive-title">
									<h3>{drive.label}</h3>
									<span className="drive-path">{drive.path}</span>
								</div>
								<button className="drive-menu">
									<FontAwesomeIcon icon={faEllipsisV} />
								</button>
							</div>

							<div className="drive-status">
								{getStatusIcon(drive)}
								<span className="status-label">
									{drive.isOnline ? 'Online' : 'Offline'}
								</span>
							</div>

							<div className="drive-storage">
								<div className="storage-info">
									<span>{formatBytes(drive.totalCapacity - drive.freeSpace)} / {formatBytes(drive.totalCapacity)}</span>
									<span>{Math.round(getUsagePercent(drive))}%</span>
								</div>
								<div className="storage-bar">
									<div 
										className="storage-fill" 
										style={{ width: `${getUsagePercent(drive)}%` }}
									/>
								</div>
							</div>

							<div className="drive-stats">
								<div className="stat">
									<FontAwesomeIcon icon={faFolder} />
									<span>{drive.romCount.toLocaleString()} ROMs</span>
								</div>
								<div className="stat">
									<FontAwesomeIcon icon={faSync} />
									<span>{formatDate(drive.lastScanned)}</span>
								</div>
							</div>

							<div className="drive-actions">
								<button 
									className="btn btn-small btn-secondary" 
									disabled={!drive.isOnline}
									onClick={() => refreshDrive.mutate(drive.id)}
								>
									<FontAwesomeIcon icon={faFolderOpen} />
									Refresh
								</button>
								<button 
									className="btn btn-small btn-secondary" 
									disabled={!drive.isOnline}
									onClick={() => handleScan(drive)}
								>
									<FontAwesomeIcon icon={faSync} />
									Scan
								</button>
								<button 
									className="btn btn-small btn-danger"
									onClick={() => handleDelete(drive)}
								>
									<FontAwesomeIcon icon={faTrash} />
								</button>
							</div>
						</div>
					))}
				</div>
			) : (
				<div className="empty-state">
					<FontAwesomeIcon icon={faHdd} size="3x" />
					<h3>No Drives Configured</h3>
					<p>Add a drive to start scanning your ROM collection.</p>
					<button className="btn btn-primary" onClick={() => setShowAddModal(true)}>
						<FontAwesomeIcon icon={faPlus} />
						Add Your First Drive
					</button>
				</div>
			)}

			{offlineDrives.length > 0 && (
				<div className="offline-summary">
					<h3>Offline Storage Summary</h3>
					<p>
						<strong>{offlineDrives.length} drive{offlineDrives.length > 1 ? 's' : ''}</strong> currently offline, 
						containing <strong>{offlineRomCount.toLocaleString()} ROMs</strong>.
						These ROMs are tracked in your database and will be accessible when the drive reconnects.
					</p>
				</div>
			)}

			{showAddModal && (
				<div className="modal-overlay" onClick={() => setShowAddModal(false)}>
					<div className="modal" onClick={(e) => e.stopPropagation()}>
						<h2>Add New Drive</h2>
						<div className="form-group">
							<label>Label</label>
							<input
								type="text"
								value={newDriveLabel}
								onChange={(e) => setNewDriveLabel(e.target.value)}
								placeholder="e.g., Main ROM Storage"
							/>
						</div>
						<div className="form-group">
							<label>Path</label>
							<input
								type="text"
								value={newDrivePath}
								onChange={(e) => setNewDrivePath(e.target.value)}
								placeholder="e.g., D:\ROMs"
							/>
						</div>
						<div className="modal-actions">
							<button className="btn btn-secondary" onClick={() => setShowAddModal(false)}>
								Cancel
							</button>
							<button 
								className="btn btn-primary" 
								onClick={handleAddDrive}
								disabled={createDrive.isPending || !newDriveLabel.trim() || !newDrivePath.trim()}
							>
								{createDrive.isPending ? (
									<><FontAwesomeIcon icon={faSpinner} spin /> Adding...</>
								) : (
									<><FontAwesomeIcon icon={faPlus} /> Add Drive</>
								)}
							</button>
						</div>
					</div>
				</div>
			)}
		</div>
	);
}
