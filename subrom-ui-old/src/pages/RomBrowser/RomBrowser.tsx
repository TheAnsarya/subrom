import { useState, useMemo } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faGamepad,
	faSearch,
	faCheckCircle,
	faTimesCircle,
	faExclamationTriangle,
	faChevronRight,
	faChevronDown,
	faFolder,
	faFolderOpen,
	faSpinner,
	faHdd
} from '@fortawesome/free-solid-svg-icons';
import { useRoms, useDrives, useRomStats, type RomFile } from '../../api';
import { useDisplaySettings } from '../../store';
import './RomBrowser.css';

function formatBytes(bytes: number): string {
	if (bytes === 0) return '0 B';
	const k = 1024;
	const sizes = ['B', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

export default function RomBrowser() {
	const displaySettings = useDisplaySettings();
	const [selectedDriveId, setSelectedDriveId] = useState<string | null>(null);
	const [searchTerm, setSearchTerm] = useState('');
	const [statusFilter, setStatusFilter] = useState<'all' | 'online' | 'offline' | 'verified'>('all');
	const [page, setPage] = useState(1);

	const { data: drives, isLoading: drivesLoading } = useDrives();
	const { data: romStats } = useRomStats();
	const { data: romsData, isLoading: romsLoading, error } = useRoms({
		driveId: selectedDriveId ?? undefined,
		search: searchTerm || undefined,
		online: statusFilter === 'online' ? true : statusFilter === 'offline' ? false : undefined,
		verified: statusFilter === 'verified' ? true : undefined,
		page,
		pageSize: displaySettings.pageSize,
	});

	const roms = romsData?.items ?? [];
	const totalPages = romsData?.totalPages ?? 1;

	const getStatusIcon = (rom: RomFile) => {
		if (rom.verifiedAt) {
			return <FontAwesomeIcon icon={faCheckCircle} className="rom-status verified" title="Verified" />;
		}
		if (!rom.isOnline) {
			return <FontAwesomeIcon icon={faTimesCircle} className="rom-status offline" title="Offline" />;
		}
		return <FontAwesomeIcon icon={faExclamationTriangle} className="rom-status unverified" title="Unverified" />;
	};

	const driveMap = useMemo(() => {
		const map = new Map<string, string>();
		drives?.forEach(d => map.set(d.id, d.label));
		return map;
	}, [drives]);

	if (error) {
		return (
			<div className="rom-browser">
				<div className="error-message">
					<FontAwesomeIcon icon={faExclamationTriangle} />
					Failed to load ROMs: {error.message}
				</div>
			</div>
		);
	}

	return (
		<div className="rom-browser">
			<div className="page-header">
				<h1>ROM Collection</h1>
				<p className="page-subtitle">
					{romStats ? `${romStats.totalRoms.toLocaleString()} ROMs (${formatBytes(romStats.totalSize)})` : 'Browse and verify your ROM files'}
				</p>
			</div>

			<div className="browser-content">
				<aside className="system-sidebar">
					<div className="sidebar-header">
						<h3>Drives</h3>
					</div>
					{drivesLoading ? (
						<div className="loading-spinner">
							<FontAwesomeIcon icon={faSpinner} spin />
						</div>
					) : (
						<div className="system-tree">
							<div
								className={`tree-item ${!selectedDriveId ? 'selected' : ''}`}
								onClick={() => setSelectedDriveId(null)}
							>
								<FontAwesomeIcon icon={faGamepad} className="tree-icon" />
								<span className="tree-label">All ROMs</span>
								<span className="tree-count">{romStats?.totalRoms ?? 0}</span>
							</div>
							{drives?.map(drive => (
								<div
									key={drive.id}
									className={`tree-item ${selectedDriveId === drive.id ? 'selected' : ''} ${!drive.isOnline ? 'offline' : ''}`}
									onClick={() => setSelectedDriveId(drive.id)}
								>
									<FontAwesomeIcon icon={faHdd} className="tree-icon" />
									<span className="tree-label">{drive.label}</span>
									<span className="tree-count">{drive.romCount}</span>
									{!drive.isOnline && (
										<FontAwesomeIcon icon={faTimesCircle} className="offline-indicator" title="Offline" />
									)}
								</div>
							))}
						</div>
					)}
				</aside>

				<main className="rom-list-container">
					<div className="rom-filters">
						<div className="search-box">
							<FontAwesomeIcon icon={faSearch} className="search-icon" />
							<input
								type="text"
								placeholder="Search ROMs..."
								value={searchTerm}
								onChange={(e) => { setSearchTerm(e.target.value); setPage(1); }}
							/>
						</div>
						<select
							value={statusFilter}
							onChange={(e) => { setStatusFilter(e.target.value as typeof statusFilter); setPage(1); }}
							className="status-filter"
						>
							<option value="all">All Status</option>
							<option value="verified">Verified</option>
							<option value="online">Online</option>
							<option value="offline">Offline</option>
						</select>
					</div>

					{romsLoading ? (
						<div className="loading-container">
							<FontAwesomeIcon icon={faSpinner} spin size="2x" />
							<p>Loading ROMs...</p>
						</div>
					) : roms.length > 0 ? (
						<>
							<div className="rom-list">
								{roms.map(rom => (
									<div key={rom.id} className={`rom-item ${rom.isOnline ? 'online' : 'offline'}`}>
										<div className="rom-icon">
											{getStatusIcon(rom)}
										</div>
										<div className="rom-info">
											<div className="rom-name">{rom.fileName}</div>
											<div className="rom-meta">
												<span className="rom-size">{formatBytes(rom.size)}</span>
												{rom.crc32 && <span className="rom-crc">CRC: {rom.crc32}</span>}
												<span className="rom-drive">{driveMap.get(rom.driveId) ?? 'Unknown Drive'}</span>
											</div>
										</div>
										<div className="rom-status-badge">
											{rom.isOnline ? 'Online' : 'Offline'}
										</div>
									</div>
								))}
							</div>
							{totalPages > 1 && (
								<div className="pagination">
									<button
										className="btn btn-secondary"
										disabled={page <= 1}
										onClick={() => setPage(p => p - 1)}
									>
										Previous
									</button>
									<span className="page-info">Page {page} of {totalPages}</span>
									<button
										className="btn btn-secondary"
										disabled={page >= totalPages}
										onClick={() => setPage(p => p + 1)}
									>
										Next
									</button>
								</div>
							)}
						</>
					) : (
						<div className="empty-state">
							<FontAwesomeIcon icon={faGamepad} size="3x" />
							<h3>No ROMs Found</h3>
							<p>{searchTerm || statusFilter !== 'all' ? 'No ROMs match your filters.' : 'Scan a drive to add ROMs to your collection.'}</p>
						</div>
					)}
				</main>
			</div>
		</div>
	);
}
