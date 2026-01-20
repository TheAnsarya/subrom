import { useEffect, useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSearch, faPlay, faStop } from '@fortawesome/free-solid-svg-icons';
import { romsApi } from '@/api/roms';
import { scanApi } from '@/api/scan';
import type { RomFile, PagedResult } from '@/types/api';
import styles from './RomFiles.module.css';

export function RomFiles() {
	const [roms, setRoms] = useState<PagedResult<RomFile> | null>(null);
	const [loading, setLoading] = useState(true);
	const [scanning, setScanning] = useState(false);
	const [searchQuery, setSearchQuery] = useState('');
	const [page, setPage] = useState(1);

	useEffect(() => {
		loadRoms();
	}, [page, searchQuery]);

	async function loadRoms() {
		setLoading(true);
		try {
			const data = searchQuery
				? await romsApi.search(searchQuery, page)
				: await romsApi.getAll(page);
			setRoms(data);
		} catch {
			console.error('Failed to load ROMs');
		} finally {
			setLoading(false);
		}
	}

	async function handleScan() {
		if (scanning) {
			await scanApi.stop();
			setScanning(false);
		} else {
			const path = prompt('Enter folder path to scan:');
			if (path) {
				await scanApi.start({ path, recursive: true });
				setScanning(true);
			}
		}
	}

	function handleSearch(e: React.FormEvent) {
		e.preventDefault();
		setPage(1);
		loadRoms();
	}

	const totalPages = roms ? Math.ceil(roms.totalCount / roms.pageSize) : 0;

	return (
		<div className={styles.page}>
			<div className={styles.header}>
				<div>
					<h1>ROM Files</h1>
					<p className={styles.subtitle}>
						{roms?.totalCount.toLocaleString() ?? 0} files in collection
					</p>
				</div>
				<button
					className={`${styles.scanButton} ${scanning ? styles.scanning : ''}`}
					onClick={handleScan}
				>
					<FontAwesomeIcon icon={scanning ? faStop : faPlay} />
					{scanning ? 'Stop Scan' : 'Start Scan'}
				</button>
			</div>

			<form className={styles.searchForm} onSubmit={handleSearch}>
				<div className={styles.searchInput}>
					<FontAwesomeIcon icon={faSearch} className={styles.searchIcon} />
					<input
						type="text"
						placeholder="Search ROMs..."
						value={searchQuery}
						onChange={(e) => setSearchQuery(e.target.value)}
					/>
				</div>
			</form>

			{loading ? (
				<div className={styles.loading}>Loading...</div>
			) : !roms || roms.items.length === 0 ? (
				<div className={styles.empty}>
					<p>No ROM files found.</p>
					<p>Click "Start Scan" to scan a folder for ROMs.</p>
				</div>
			) : (
				<>
					<div className={styles.table}>
						<div className={styles.tableHeader}>
							<span>Name</span>
							<span>Size</span>
							<span>Status</span>
							<span>Scanned</span>
						</div>
						{roms.items.map((rom) => (
							<div key={rom.id} className={styles.tableRow}>
								<span className={styles.name} title={rom.path}>
									{rom.name}
								</span>
								<span>{formatSize(rom.size)}</span>
								<span className={styles.status} data-status={rom.status}>
									{rom.status}
								</span>
								<span>{new Date(rom.scannedAt).toLocaleDateString()}</span>
							</div>
						))}
					</div>

					{totalPages > 1 && (
						<div className={styles.pagination}>
							<button
								disabled={page === 1}
								onClick={() => setPage(page - 1)}
							>
								Previous
							</button>
							<span>
								Page {page} of {totalPages}
							</span>
							<button
								disabled={page === totalPages}
								onClick={() => setPage(page + 1)}
							>
								Next
							</button>
						</div>
					)}
				</>
			)}
		</div>
	);
}

function formatSize(bytes: number): string {
	if (bytes < 1024) return `${bytes} B`;
	if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
	if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
	return `${(bytes / 1024 / 1024 / 1024).toFixed(1)} GB`;
}
