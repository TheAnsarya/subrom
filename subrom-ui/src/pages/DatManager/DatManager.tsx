import { useEffect, useState, useRef } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faUpload, faTrash, faSpinner } from '@fortawesome/free-solid-svg-icons';
import { datsApi } from '@/api/dats';
import type { DatFile } from '@/types/api';
import styles from './DatManager.module.css';

export function DatManager() {
	const [dats, setDats] = useState<DatFile[]>([]);
	const [loading, setLoading] = useState(true);
	const [uploading, setUploading] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const fileInputRef = useRef<HTMLInputElement>(null);

	useEffect(() => {
		loadDats();
	}, []);

	async function loadDats() {
		try {
			const data = await datsApi.getAll();
			setDats(data);
		} catch {
			setError('Failed to load DAT files');
		} finally {
			setLoading(false);
		}
	}

	async function handleFileSelect(event: React.ChangeEvent<HTMLInputElement>) {
		const file = event.target.files?.[0];
		if (!file) return;

		setUploading(true);
		setError(null);

		try {
			const newDat = await datsApi.import(file);
			setDats((prev) => [...prev, newDat]);
		} catch {
			setError('Failed to import DAT file');
		} finally {
			setUploading(false);
			if (fileInputRef.current) {
				fileInputRef.current.value = '';
			}
		}
	}

	async function handleDelete(id: number) {
		if (!confirm('Are you sure you want to delete this DAT file?')) return;

		try {
			await datsApi.delete(id);
			setDats((prev) => prev.filter((d) => d.id !== id));
		} catch {
			setError('Failed to delete DAT file');
		}
	}

	return (
		<div className={styles.page}>
			<div className={styles.header}>
				<div>
					<h1>DAT Files</h1>
					<p className={styles.subtitle}>Manage your ROM verification databases</p>
				</div>
				<label className={styles.uploadButton}>
					<FontAwesomeIcon
						icon={uploading ? faSpinner : faUpload}
						spin={uploading}
					/>
					{uploading ? 'Importing...' : 'Import DAT'}
					<input
						ref={fileInputRef}
						type="file"
						accept=".dat,.xml"
						onChange={handleFileSelect}
						disabled={uploading}
						hidden
					/>
				</label>
			</div>

			{error && (
				<div className={styles.error}>{error}</div>
			)}

			{loading ? (
				<div className={styles.loading}>Loading...</div>
			) : dats.length === 0 ? (
				<div className={styles.empty}>
					<p>No DAT files imported yet.</p>
					<p>Click "Import DAT" to add a verification database.</p>
				</div>
			) : (
				<div className={styles.table}>
					<div className={styles.tableHeader}>
						<span>Name</span>
						<span>Games</span>
						<span>ROMs</span>
						<span>Imported</span>
						<span></span>
					</div>
					{dats.map((dat) => (
						<div key={dat.id} className={styles.tableRow}>
							<span className={styles.name}>
								{dat.name}
								{dat.version && (
									<span className={styles.version}>v{dat.version}</span>
								)}
							</span>
							<span>{dat.gameCount.toLocaleString()}</span>
							<span>{dat.romCount.toLocaleString()}</span>
							<span>{new Date(dat.importedAt).toLocaleDateString()}</span>
							<button
								className={styles.deleteButton}
								onClick={() => handleDelete(dat.id)}
								title="Delete"
							>
								<FontAwesomeIcon icon={faTrash} />
							</button>
						</div>
					))}
				</div>
			)}
		</div>
	);
}
