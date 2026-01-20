import React, { useState } from 'react';
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
	faEllipsisV
} from '@fortawesome/free-solid-svg-icons';
import './DriveManager.css';

interface Drive {
	id: string;
	name: string;
	path: string;
	status: 'online' | 'offline' | 'scanning';
	totalSize: string;
	usedSize: string;
	romCount: number;
	lastScan?: string;
}

const DriveManager: React.FC = () => {
	const [drives] = useState<Drive[]>([
		{
			id: '1',
			name: 'Main ROM Storage',
			path: 'D:\\ROMs',
			status: 'online',
			totalSize: '2 TB',
			usedSize: '1.2 TB',
			romCount: 15234,
			lastScan: '2024-01-19 10:30'
		},
		{
			id: '2',
			name: 'Backup Drive',
			path: 'E:\\ROM-Backup',
			status: 'online',
			totalSize: '4 TB',
			usedSize: '2.8 TB',
			romCount: 23456,
			lastScan: '2024-01-18 22:15'
		},
		{
			id: '3',
			name: 'Portable Storage',
			path: 'F:\\Games',
			status: 'offline',
			totalSize: '500 GB',
			usedSize: '450 GB',
			romCount: 8721,
			lastScan: '2024-01-15 14:45'
		},
		{
			id: '4',
			name: 'NAS Archive',
			path: '\\\\NAS\\ROMs',
			status: 'scanning',
			totalSize: '8 TB',
			usedSize: '5.2 TB',
			romCount: 45678,
			lastScan: 'In progress...'
		},
	]);

	const getStatusIcon = (status: string) => {
		switch (status) {
			case 'online':
				return <FontAwesomeIcon icon={faCheckCircle} className="drive-status-icon online" />;
			case 'offline':
				return <FontAwesomeIcon icon={faTimesCircle} className="drive-status-icon offline" />;
			case 'scanning':
				return <FontAwesomeIcon icon={faSync} className="drive-status-icon scanning fa-spin" />;
			default:
				return null;
		}
	};

	const getUsagePercent = (drive: Drive): number => {
		const used = parseFloat(drive.usedSize);
		const total = parseFloat(drive.totalSize);
		return (used / total) * 100;
	};

	return (
		<div className="drive-manager">
			<div className="page-header">
				<div className="header-left">
					<h1>Drive Manager</h1>
					<p className="page-subtitle">Manage your ROM storage locations</p>
				</div>
				<div className="header-actions">
					<button className="btn btn-secondary">
						<FontAwesomeIcon icon={faSync} />
						Refresh All
					</button>
					<button className="btn btn-primary">
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

			<div className="drives-grid">
				{drives.map(drive => (
					<div key={drive.id} className={`drive-card ${drive.status}`}>
						<div className="drive-header">
							<div className="drive-icon">
								<FontAwesomeIcon icon={faHdd} />
							</div>
							<div className="drive-title">
								<h3>{drive.name}</h3>
								<span className="drive-path">{drive.path}</span>
							</div>
							<button className="drive-menu">
								<FontAwesomeIcon icon={faEllipsisV} />
							</button>
						</div>

						<div className="drive-status">
							{getStatusIcon(drive.status)}
							<span className="status-label">
								{drive.status.charAt(0).toUpperCase() + drive.status.slice(1)}
							</span>
						</div>

						<div className="drive-storage">
							<div className="storage-info">
								<span>{drive.usedSize} / {drive.totalSize}</span>
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
								<span>{drive.lastScan}</span>
							</div>
						</div>

						<div className="drive-actions">
							<button className="btn btn-small btn-secondary" disabled={drive.status === 'offline'}>
								<FontAwesomeIcon icon={faFolderOpen} />
								Browse
							</button>
							<button className="btn btn-small btn-secondary" disabled={drive.status !== 'online'}>
								<FontAwesomeIcon icon={faSync} />
								Scan
							</button>
							<button className="btn btn-small btn-danger">
								<FontAwesomeIcon icon={faTrash} />
							</button>
						</div>
					</div>
				))}
			</div>

			<div className="offline-summary">
				<h3>Offline Storage Summary</h3>
				<p>
					<strong>1 drive</strong> is currently offline, containing <strong>8,721 ROMs</strong>.
					These ROMs are tracked in your database and will be accessible when the drive reconnects.
				</p>
			</div>
		</div>
	);
};

export default DriveManager;
