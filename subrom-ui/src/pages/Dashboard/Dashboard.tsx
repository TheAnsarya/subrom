import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import {
	faGamepad,
	faCheckCircle,
	faExclamationTriangle,
	faHdd,
	faPlay,
	faSync,
	faFolderOpen,
	faCog,
	faSpinner
} from '@fortawesome/free-solid-svg-icons';
import { useNavigate } from 'react-router-dom';
import { useDrives, useRomStats, useScans, useDatProviders } from '../../api';
import './Dashboard.css';

interface StatCardProps {
	title: string;
	value: string | number;
	icon: IconDefinition;
	color: string;
	subtitle?: string;
	loading?: boolean;
}

function StatCard({ title, value, icon, color, subtitle, loading }: StatCardProps) {
	return (
		<div className="stat-card">
			<div className={`stat-icon ${color}`}>
				<FontAwesomeIcon icon={loading ? faSpinner : icon} spin={loading} />
			</div>
			<div className="stat-content">
				<div className="stat-value">{loading ? '...' : value}</div>
				<div className="stat-title">{title}</div>
				{subtitle && <div className="stat-subtitle">{subtitle}</div>}
			</div>
		</div>
	);
}

interface QuickActionProps {
	label: string;
	icon: IconDefinition;
	onClick: () => void;
}

function QuickAction({ label, icon, onClick }: QuickActionProps) {
	return (
		<button className="quick-action" onClick={onClick}>
			<FontAwesomeIcon icon={icon} className="quick-action-icon" />
			<span>{label}</span>
		</button>
	);
}

function formatBytes(bytes: number): string {
	if (bytes === 0) return '0 B';
	const k = 1024;
	const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

function formatTimeAgo(dateString: string | null): string {
	if (!dateString) return 'Never';
	const date = new Date(dateString);
	const now = new Date();
	const diffMs = now.getTime() - date.getTime();
	const diffMins = Math.floor(diffMs / 60000);
	if (diffMins < 1) return 'Just now';
	if (diffMins < 60) return `${diffMins} min ago`;
	const diffHours = Math.floor(diffMins / 60);
	if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
	const diffDays = Math.floor(diffHours / 24);
	return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
}

export default function Dashboard() {
	const navigate = useNavigate();
	const { data: drives, isLoading: drivesLoading } = useDrives();
	const { data: romStats, isLoading: statsLoading } = useRomStats();
	const { data: scans, isLoading: scansLoading } = useScans({ limit: 5 });
	const { data: providers, isLoading: providersLoading } = useDatProviders();

	const onlineDrives = drives?.filter(d => d.isOnline).length ?? 0;
	const totalDrives = drives?.length ?? 0;
	const totalRoms = romStats?.totalRoms ?? 0;
	const verifiedRoms = romStats?.verifiedRoms ?? 0;
	const verifiedPercent = totalRoms > 0 ? ((verifiedRoms / totalRoms) * 100).toFixed(1) : '0';
	const totalSize = romStats?.totalSize ?? 0;

	const recentScans = scans?.slice(0, 4).map(scan => ({
		id: scan.id,
		action: scan.status === 'Running'
			? `Scanning: ${scan.currentFile ?? scan.rootPath}`
			: `Scanned ${scan.rootPath} (${scan.processedFiles} files)`,
		time: formatTimeAgo(scan.completedAt ?? scan.startedAt),
		status: scan.status,
	})) ?? [];

	return (
		<div className="dashboard">
			<div className="page-header">
				<h1>Dashboard</h1>
				<p className="page-subtitle">Overview of your ROM collection</p>
			</div>

			<div className="stats-grid">
				<StatCard
					title="Total ROMs"
					value={totalRoms.toLocaleString()}
					icon={faGamepad}
					color="blue"
					loading={statsLoading}
					subtitle={formatBytes(totalSize)}
				/>
				<StatCard
					title="Verified"
					value={`${verifiedPercent}%`}
					icon={faCheckCircle}
					color="green"
					loading={statsLoading}
					subtitle={`${verifiedRoms.toLocaleString()} ROMs`}
				/>
				<StatCard
					title="Online"
					value={romStats?.onlineRoms?.toLocaleString() ?? 0}
					icon={faExclamationTriangle}
					color="yellow"
					loading={statsLoading}
					subtitle="accessible now"
				/>
				<StatCard
					title="Drives"
					value={`${onlineDrives}/${totalDrives}`}
					icon={faHdd}
					color={onlineDrives === totalDrives ? 'green' : 'yellow'}
					loading={drivesLoading}
					subtitle={onlineDrives === totalDrives ? 'All online' : 'Some offline'}
				/>
			</div>

			<div className="dashboard-grid">
				<div className="dashboard-card">
					<h2 className="card-title">Recent Scans</h2>
					{scansLoading ? (
						<div className="loading-spinner">
							<FontAwesomeIcon icon={faSpinner} spin />
						</div>
					) : recentScans.length > 0 ? (
						<ul className="activity-list">
							{recentScans.map((item) => (
								<li key={item.id} className={`activity-item ${item.status === 'Running' ? 'running' : ''}`}>
									<span className="activity-action">{item.action}</span>
									<span className="activity-time">{item.time}</span>
								</li>
							))}
						</ul>
					) : (
						<p className="empty-state">No recent scans</p>
					)}
				</div>

				<div className="dashboard-card">
					<h2 className="card-title">DAT Providers</h2>
					{providersLoading ? (
						<div className="loading-spinner">
							<FontAwesomeIcon icon={faSpinner} spin />
						</div>
					) : providers && providers.length > 0 ? (
						<div className="chart-placeholder">
							{providers.map(provider => {
								const percent = Math.min(100, Math.round((provider.romCount / Math.max(totalRoms, 1)) * 100));
								return (
									<div key={provider.provider} className="system-bar">
										<span className="system-name">{provider.provider || 'Unknown'}</span>
										<div className="progress-bar">
											<div className="progress-fill" style={{ width: `${percent}%` }}></div>
										</div>
										<span className="system-percent">{provider.romCount.toLocaleString()}</span>
									</div>
								);
							})}
						</div>
					) : (
						<p className="empty-state">No DAT files imported</p>
					)}
				</div>
			</div>

			<div className="dashboard-card">
				<h2 className="card-title">Quick Actions</h2>
				<div className="quick-actions">
					<QuickAction
						label="Start Scan"
						icon={faPlay}
						onClick={() => navigate('/drives')}
					/>
					<QuickAction
						label="Manage DATs"
						icon={faSync}
						onClick={() => navigate('/dats')}
					/>
					<QuickAction
						label="Browse ROMs"
						icon={faFolderOpen}
						onClick={() => navigate('/roms')}
					/>
					<QuickAction
						label="Settings"
						icon={faCog}
						onClick={() => navigate('/settings')}
					/>
				</div>
			</div>
		</div>
	);
}
