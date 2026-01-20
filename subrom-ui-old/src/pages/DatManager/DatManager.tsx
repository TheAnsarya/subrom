import { useState, useMemo } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faDatabase,
	faUpload,
	faSync,
	faSearch,
	faChevronRight,
	faCheckCircle,
	faExclamationTriangle,
	faEllipsisV,
	faSpinner,
	faToggleOn,
	faToggleOff,
	faTrash
} from '@fortawesome/free-solid-svg-icons';
import { useDats, useDatProviders, useToggleDat, useDeleteDat, type DatFile } from '../../api';
import './DatManager.css';

function formatDate(dateString: string): string {
	const date = new Date(dateString);
	const now = new Date();
	const diffMs = now.getTime() - date.getTime();
	const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

	if (diffDays === 0) return 'Today';
	if (diffDays === 1) return 'Yesterday';
	if (diffDays < 7) return `${diffDays} days ago`;
	if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
	if (diffDays < 365) return `${Math.floor(diffDays / 30)} months ago`;
	return date.toLocaleDateString();
}

export default function DatManager() {
	const [searchTerm, setSearchTerm] = useState('');
	const [selectedProvider, setSelectedProvider] = useState('all');
	const [selectedDat, setSelectedDat] = useState<DatFile | null>(null);

	const { data: dats, isLoading, error, refetch } = useDats();
	const { data: providers } = useDatProviders();
	const toggleDat = useToggleDat();
	const deleteDat = useDeleteDat();

	const providerList = useMemo(() => {
		const list = ['all'];
		if (providers) {
			list.push(...providers.map(p => p.provider || 'Unknown'));
		}
		return [...new Set(list)];
	}, [providers]);

	const filteredDats = useMemo(() => {
		return (dats ?? []).filter(dat => {
			const matchesSearch = dat.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
				(dat.description?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false);
			const matchesProvider = selectedProvider === 'all' || (dat.provider ?? 'Unknown') === selectedProvider;
			return matchesSearch && matchesProvider;
		});
	}, [dats, searchTerm, selectedProvider]);

	const handleToggle = (dat: DatFile) => {
		toggleDat.mutate(dat.id);
	};

	const handleDelete = (dat: DatFile) => {
		if (window.confirm(`Are you sure you want to delete "${dat.name}"?`)) {
			deleteDat.mutate(dat.id, {
				onSuccess: () => {
					if (selectedDat?.id === dat.id) {
						setSelectedDat(null);
					}
				},
			});
		}
	};

	const getStatusIcon = (dat: DatFile) => {
		if (!dat.isEnabled) {
			return <FontAwesomeIcon icon={faToggleOff} className="status-icon disabled" title="Disabled" />;
		}
		return <FontAwesomeIcon icon={faCheckCircle} className="status-icon current" title="Enabled" />;
	};

	if (error) {
		return (
			<div className="dat-manager">
				<div className="error-message">
					<FontAwesomeIcon icon={faExclamationTriangle} />
					Failed to load DAT files: {error.message}
				</div>
			</div>
		);
	}

	return (
		<div className="dat-manager">
			<div className="page-header">
				<div className="header-content">
					<h1>DAT File Manager</h1>
					<p className="page-subtitle">Manage your ROM verification databases</p>
				</div>
				<div className="header-actions">
					<button className="btn btn-secondary">
						<FontAwesomeIcon icon={faUpload} /> Import
					</button>
					<button className="btn btn-primary" onClick={() => refetch()} disabled={isLoading}>
						<FontAwesomeIcon icon={isLoading ? faSpinner : faSync} spin={isLoading} /> Refresh
					</button>
				</div>
			</div>

			<div className="filters-bar">
				<div className="search-box">
					<FontAwesomeIcon icon={faSearch} className="search-icon" />
					<input
						type="text"
						placeholder="Search DAT files..."
						value={searchTerm}
						onChange={(e) => setSearchTerm(e.target.value)}
					/>
				</div>
				<div className="filter-group">
					<label>Provider:</label>
					<select
						value={selectedProvider}
						onChange={(e) => setSelectedProvider(e.target.value)}
					>
						{providerList.map(p => (
							<option key={p} value={p}>
								{p === 'all' ? 'All Providers' : p}
							</option>
						))}
					</select>
				</div>
			</div>

			<div className="dat-content">
				{isLoading ? (
					<div className="loading-container">
						<FontAwesomeIcon icon={faSpinner} spin size="2x" />
						<p>Loading DAT files...</p>
					</div>
				) : filteredDats.length > 0 ? (
					<div className="dat-list">
						<table className="dat-table">
							<thead>
								<tr>
									<th>Provider</th>
									<th>DAT Name</th>
									<th>Version</th>
									<th>Games</th>
									<th>Status</th>
									<th></th>
								</tr>
							</thead>
							<tbody>
								{filteredDats.map(dat => (
									<tr
										key={dat.id}
										className={selectedDat?.id === dat.id ? 'selected' : ''}
										onClick={() => setSelectedDat(dat)}
									>
										<td className="provider-cell">
											<span className={`provider-badge ${(dat.provider ?? 'unknown').toLowerCase().replace('-', '')}`}>
												{dat.provider ?? 'Unknown'}
											</span>
										</td>
										<td className="name-cell">{dat.name}</td>
										<td className="version-cell">{dat.version ?? '-'}</td>
										<td className="games-cell">{dat.gameCount.toLocaleString()}</td>
										<td className="status-cell">{getStatusIcon(dat)}</td>
										<td className="actions-cell">
											<button className="icon-btn" onClick={(e) => { e.stopPropagation(); handleToggle(dat); }}>
												<FontAwesomeIcon icon={dat.isEnabled ? faToggleOn : faToggleOff} />
											</button>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				) : (
					<div className="empty-state">
						<FontAwesomeIcon icon={faDatabase} size="3x" />
						<h3>No DAT Files</h3>
						<p>{searchTerm || selectedProvider !== 'all' ? 'No DAT files match your filters.' : 'Import DAT files to start verifying your ROMs.'}</p>
					</div>
				)}

				{selectedDat && (
					<div className="dat-details">
						<h3>{selectedDat.name}</h3>
						<div className="detail-grid">
							<div className="detail-item">
								<label>Provider</label>
								<span>{selectedDat.provider ?? 'Unknown'}</span>
							</div>
							<div className="detail-item">
								<label>Version</label>
								<span>{selectedDat.version ?? '-'}</span>
							</div>
							<div className="detail-item">
								<label>Games</label>
								<span>{selectedDat.gameCount.toLocaleString()}</span>
							</div>
							<div className="detail-item">
								<label>ROMs</label>
								<span>{selectedDat.romCount.toLocaleString()}</span>
							</div>
							<div className="detail-item">
								<label>Imported</label>
								<span>{formatDate(selectedDat.importedAt)}</span>
							</div>
							<div className="detail-item">
								<label>Status</label>
								<span>{selectedDat.isEnabled ? 'Enabled' : 'Disabled'}</span>
							</div>
						</div>
						{selectedDat.description && (
							<div className="detail-description">
								<label>Description</label>
								<p>{selectedDat.description}</p>
							</div>
						)}
						<div className="detail-actions">
							<button className="btn btn-secondary" onClick={() => handleToggle(selectedDat)}>
								<FontAwesomeIcon icon={selectedDat.isEnabled ? faToggleOff : faToggleOn} />
								{selectedDat.isEnabled ? 'Disable' : 'Enable'}
							</button>
							<button className="btn btn-danger" onClick={() => handleDelete(selectedDat)}>
								<FontAwesomeIcon icon={faTrash} />
								Delete
							</button>
						</div>
					</div>
				)}
			</div>
		</div>
	);
}
