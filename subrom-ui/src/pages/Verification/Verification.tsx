import { useEffect, useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faCheckCircle,
	faQuestionCircle,
	faExclamationCircle,
} from '@fortawesome/free-solid-svg-icons';
import { verificationApi } from '@/api/verification';
import type { VerificationSummary, RomFile, Game, PagedResult } from '@/types/api';
import styles from './Verification.module.css';

type Tab = 'summary' | 'unknown' | 'missing';

export function Verification() {
	const [activeTab, setActiveTab] = useState<Tab>('summary');
	const [summary, setSummary] = useState<VerificationSummary | null>(null);
	const [unknownRoms, setUnknownRoms] = useState<PagedResult<RomFile> | null>(null);
	const [missingGames, setMissingGames] = useState<PagedResult<Game> | null>(null);
	const [loading, setLoading] = useState(true);

	useEffect(() => {
		loadData();
	}, [activeTab]);

	async function loadData() {
		setLoading(true);
		try {
			switch (activeTab) {
				case 'summary':
					setSummary(await verificationApi.getSummary());
					break;
				case 'unknown':
					setUnknownRoms(await verificationApi.getUnknownRoms());
					break;
				case 'missing':
					setMissingGames(await verificationApi.getMissingGames());
					break;
			}
		} catch {
			console.error('Failed to load data');
		} finally {
			setLoading(false);
		}
	}

	return (
		<div className={styles.page}>
			<h1>Verification</h1>
			<p className={styles.subtitle}>Check your ROMs against DAT files</p>

			<div className={styles.tabs}>
				<button
					className={`${styles.tab} ${activeTab === 'summary' ? styles.active : ''}`}
					onClick={() => setActiveTab('summary')}
				>
					Summary
				</button>
				<button
					className={`${styles.tab} ${activeTab === 'unknown' ? styles.active : ''}`}
					onClick={() => setActiveTab('unknown')}
				>
					Unknown ROMs
				</button>
				<button
					className={`${styles.tab} ${activeTab === 'missing' ? styles.active : ''}`}
					onClick={() => setActiveTab('missing')}
				>
					Missing Games
				</button>
			</div>

			<div className={styles.content}>
				{loading ? (
					<div className={styles.loading}>Loading...</div>
				) : (
					<>
						{activeTab === 'summary' && summary && (
							<div className={styles.summaryGrid}>
								<div className={styles.summaryCard} data-type="total">
									<FontAwesomeIcon icon={faCheckCircle} className={styles.summaryIcon} />
									<div className={styles.summaryValue}>{summary.totalRoms.toLocaleString()}</div>
									<div className={styles.summaryLabel}>Total ROMs</div>
								</div>
								<div className={styles.summaryCard} data-type="verified">
									<FontAwesomeIcon icon={faCheckCircle} className={styles.summaryIcon} />
									<div className={styles.summaryValue}>{summary.verifiedRoms.toLocaleString()}</div>
									<div className={styles.summaryLabel}>Verified</div>
								</div>
								<div className={styles.summaryCard} data-type="unknown">
									<FontAwesomeIcon icon={faQuestionCircle} className={styles.summaryIcon} />
									<div className={styles.summaryValue}>{summary.unknownRoms.toLocaleString()}</div>
									<div className={styles.summaryLabel}>Unknown</div>
								</div>
								<div className={styles.summaryCard} data-type="missing">
									<FontAwesomeIcon icon={faExclamationCircle} className={styles.summaryIcon} />
									<div className={styles.summaryValue}>{summary.missingGames.toLocaleString()}</div>
									<div className={styles.summaryLabel}>Missing Games</div>
								</div>
							</div>
						)}

						{activeTab === 'unknown' && unknownRoms && (
							<div className={styles.list}>
								{unknownRoms.items.length === 0 ? (
									<div className={styles.empty}>No unknown ROMs found</div>
								) : (
									unknownRoms.items.map((rom) => (
										<div key={rom.id} className={styles.listItem}>
											<span className={styles.itemName}>{rom.name}</span>
											<span className={styles.itemPath}>{rom.path}</span>
										</div>
									))
								)}
							</div>
						)}

						{activeTab === 'missing' && missingGames && (
							<div className={styles.list}>
								{missingGames.items.length === 0 ? (
									<div className={styles.empty}>No missing games found</div>
								) : (
									missingGames.items.map((game) => (
										<div key={game.id} className={styles.listItem}>
											<span className={styles.itemName}>{game.name}</span>
											<span className={styles.itemDesc}>{game.description}</span>
										</div>
									))
								)}
							</div>
						)}
					</>
				)}
			</div>
		</div>
	);
}
