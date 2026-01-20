import React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faGamepad,
	faCheckCircle,
	faExclamationTriangle,
	faHdd,
	faPlay,
	faSync,
	faFolderOpen,
	faCog
} from '@fortawesome/free-solid-svg-icons';
import './Dashboard.css';

interface StatCardProps {
	title: string;
	value: string | number;
	icon: any;
	color: string;
	subtitle?: string;
}

const StatCard: React.FC<StatCardProps> = ({ title, value, icon, color, subtitle }) => (
	<div className="stat-card">
		<div className={`stat-icon ${color}`}>
			<FontAwesomeIcon icon={icon} />
		</div>
		<div className="stat-content">
			<div className="stat-value">{value}</div>
			<div className="stat-title">{title}</div>
			{subtitle && <div className="stat-subtitle">{subtitle}</div>}
		</div>
	</div>
);

interface QuickActionProps {
	label: string;
	icon: any;
	onClick: () => void;
}

const QuickAction: React.FC<QuickActionProps> = ({ label, icon, onClick }) => (
	<button className="quick-action" onClick={onClick}>
		<FontAwesomeIcon icon={icon} className="quick-action-icon" />
		<span>{label}</span>
	</button>
);

const Dashboard: React.FC = () => {
	// Mock data - will be replaced with API calls
	const stats = {
		totalRoms: 45231,
		completePercent: 89.3,
		missingRoms: 4823,
		onlineDrives: 3,
		totalDrives: 4
	};

	const recentActivity = [
		{ id: 1, action: 'Scanned NES folder', time: '2 hours ago' },
		{ id: 2, action: 'Updated No-Intro DATs', time: '5 hours ago' },
		{ id: 3, action: 'Added 234 new ROMs', time: '1 day ago' },
		{ id: 4, action: 'Organized SNES folder', time: '2 days ago' },
	];

	return (
		<div className="dashboard">
			<div className="page-header">
				<h1>Dashboard</h1>
				<p className="page-subtitle">Overview of your ROM collection</p>
			</div>

			<div className="stats-grid">
				<StatCard
					title="Total ROMs"
					value={stats.totalRoms.toLocaleString()}
					icon={faGamepad}
					color="blue"
				/>
				<StatCard
					title="Complete"
					value={`${stats.completePercent}%`}
					icon={faCheckCircle}
					color="green"
					subtitle="of known ROMs"
				/>
				<StatCard
					title="Missing"
					value={stats.missingRoms.toLocaleString()}
					icon={faExclamationTriangle}
					color="yellow"
				/>
				<StatCard
					title="Drives"
					value={`${stats.onlineDrives}/${stats.totalDrives}`}
					icon={faHdd}
					color={stats.onlineDrives === stats.totalDrives ? 'green' : 'yellow'}
					subtitle={stats.onlineDrives === stats.totalDrives ? 'All online' : 'Some offline'}
				/>
			</div>

			<div className="dashboard-grid">
				<div className="dashboard-card">
					<h2 className="card-title">Recent Activity</h2>
					<ul className="activity-list">
						{recentActivity.map((item) => (
							<li key={item.id} className="activity-item">
								<span className="activity-action">{item.action}</span>
								<span className="activity-time">{item.time}</span>
							</li>
						))}
					</ul>
				</div>

				<div className="dashboard-card">
					<h2 className="card-title">Collection by System</h2>
					<div className="chart-placeholder">
						{/* TODO: Add chart component */}
						<div className="system-bar">
							<span className="system-name">NES</span>
							<div className="progress-bar">
								<div className="progress-fill" style={{ width: '92%' }}></div>
							</div>
							<span className="system-percent">92%</span>
						</div>
						<div className="system-bar">
							<span className="system-name">SNES</span>
							<div className="progress-bar">
								<div className="progress-fill" style={{ width: '88%' }}></div>
							</div>
							<span className="system-percent">88%</span>
						</div>
						<div className="system-bar">
							<span className="system-name">Genesis</span>
							<div className="progress-bar">
								<div className="progress-fill" style={{ width: '76%' }}></div>
							</div>
							<span className="system-percent">76%</span>
						</div>
						<div className="system-bar">
							<span className="system-name">GBA</span>
							<div className="progress-bar">
								<div className="progress-fill" style={{ width: '95%' }}></div>
							</div>
							<span className="system-percent">95%</span>
						</div>
					</div>
				</div>
			</div>

			<div className="dashboard-card">
				<h2 className="card-title">Quick Actions</h2>
				<div className="quick-actions">
					<QuickAction
						label="Start Scan"
						icon={faPlay}
						onClick={() => console.log('Start scan')}
					/>
					<QuickAction
						label="Update DATs"
						icon={faSync}
						onClick={() => console.log('Update DATs')}
					/>
					<QuickAction
						label="Organize"
						icon={faFolderOpen}
						onClick={() => console.log('Organize')}
					/>
					<QuickAction
						label="Settings"
						icon={faCog}
						onClick={() => console.log('Settings')}
					/>
				</div>
			</div>
		</div>
	);
};

export default Dashboard;
