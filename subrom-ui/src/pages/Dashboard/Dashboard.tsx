import { useEffect, useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faDatabase,
	faHardDrive,
	faCheckCircle,
	faQuestionCircle,
} from '@fortawesome/free-solid-svg-icons';
import { verificationApi } from '@/api/verification';
import { datsApi } from '@/api/dats';
import type { VerificationSummary, DatFile } from '@/types/api';
import styles from './Dashboard.module.css';

export function Dashboard() {
	const [summary, setSummary] = useState<VerificationSummary | null>(null);
	const [dats, setDats] = useState<DatFile[]>([]);
	const [loading, setLoading] = useState(true);

	useEffect(() => {
		async function loadData() {
			try {
				const [summaryData, datsData] = await Promise.all([
					verificationApi.getSummary().catch(() => null),
					datsApi.getAll().catch(() => []),
				]);
				setSummary(summaryData);
				setDats(datsData);
			} finally {
				setLoading(false);
			}
		}
		loadData();
	}, []);

	if (loading) {
		return <div className={styles.loading}>Loading...</div>;
	}

	const stats = [
		{
			label: 'DAT Files',
			value: dats.length,
			icon: faDatabase,
			color: 'blue',
		},
		{
			label: 'Total ROMs',
			value: summary?.totalRoms ?? 0,
			icon: faHardDrive,
			color: 'purple',
		},
		{
			label: 'Verified',
			value: summary?.verifiedRoms ?? 0,
			icon: faCheckCircle,
			color: 'green',
		},
		{
			label: 'Unknown',
			value: summary?.unknownRoms ?? 0,
			icon: faQuestionCircle,
			color: 'orange',
		},
	];

	return (
		<div className={styles.dashboard}>
			<h1>Dashboard</h1>
			<p className={styles.subtitle}>Welcome to Subrom ROM Manager</p>

			<div className={styles.statsGrid}>
				{stats.map((stat) => (
					<div key={stat.label} className={styles.statCard} data-color={stat.color}>
						<div className={styles.statIcon}>
							<FontAwesomeIcon icon={stat.icon} />
						</div>
						<div className={styles.statContent}>
							<span className={styles.statValue}>{stat.value.toLocaleString()}</span>
							<span className={styles.statLabel}>{stat.label}</span>
						</div>
					</div>
				))}
			</div>

			<div className={styles.section}>
				<h2>Quick Actions</h2>
				<div className={styles.actions}>
					<a href="/dats" className={styles.actionButton}>
						Import DAT File
					</a>
					<a href="/roms" className={styles.actionButton}>
						Scan for ROMs
					</a>
					<a href="/verification" className={styles.actionButton}>
						Run Verification
					</a>
				</div>
			</div>

			{dats.length > 0 && (
				<div className={styles.section}>
					<h2>Recent DAT Files</h2>
					<div className={styles.datList}>
						{dats.slice(0, 5).map((dat) => (
							<div key={dat.id} className={styles.datItem}>
								<span className={styles.datName}>{dat.name}</span>
								<span className={styles.datGames}>{dat.gameCount} games</span>
							</div>
						))}
					</div>
				</div>
			)}
		</div>
	);
}
