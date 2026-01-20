import React, { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faDatabase,
	faUpload,
	faSync,
	faSearch,
	faChevronRight,
	faCheckCircle,
	faExclamationTriangle,
	faEllipsisV
} from '@fortawesome/free-solid-svg-icons';
import './DatManager.css';

interface DatFile {
	id: string;
	name: string;
	provider: string;
	version: string;
	games: number;
	roms: number;
	lastUpdated: string;
	status: 'current' | 'outdated' | 'unknown';
}

const DatManager: React.FC = () => {
	const [searchTerm, setSearchTerm] = useState('');
	const [selectedProvider, setSelectedProvider] = useState('all');
	const [selectedDat, setSelectedDat] = useState<DatFile | null>(null);

	// Mock data
	const datFiles: DatFile[] = [
		{
			id: '1',
			name: 'Nintendo - Nintendo Entertainment System',
			provider: 'No-Intro',
			version: '2026-01-15',
			games: 2847,
			roms: 3456,
			lastUpdated: '3 days ago',
			status: 'current'
		},
		{
			id: '2',
			name: 'Nintendo - Super Nintendo Entertainment System',
			provider: 'No-Intro',
			version: '2026-01-10',
			games: 3521,
			roms: 4102,
			lastUpdated: '8 days ago',
			status: 'current'
		},
		{
			id: '3',
			name: 'Nintendo - Game Boy',
			provider: 'No-Intro',
			version: '2025-12-20',
			games: 1456,
			roms: 1589,
			lastUpdated: '29 days ago',
			status: 'outdated'
		},
		{
			id: '4',
			name: 'Commodore - C64',
			provider: 'TOSEC',
			version: '2025-11-01',
			games: 8234,
			roms: 12456,
			lastUpdated: '2 months ago',
			status: 'current'
		},
		{
			id: '5',
			name: 'Sony - PlayStation',
			provider: 'Redump',
			version: '2026-01-12',
			games: 4521,
			roms: 4521,
			lastUpdated: '6 days ago',
			status: 'current'
		},
	];

	const providers = ['all', 'No-Intro', 'TOSEC', 'Redump', 'GoodSets'];

	const filteredDats = datFiles.filter(dat => {
		const matchesSearch = dat.name.toLowerCase().includes(searchTerm.toLowerCase());
		const matchesProvider = selectedProvider === 'all' || dat.provider === selectedProvider;
		return matchesSearch && matchesProvider;
	});

	const getStatusIcon = (status: string) => {
		switch (status) {
			case 'current':
				return <FontAwesomeIcon icon={faCheckCircle} className="status-icon current" />;
			case 'outdated':
				return <FontAwesomeIcon icon={faExclamationTriangle} className="status-icon outdated" />;
			default:
				return null;
		}
	};

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
					<button className="btn btn-primary">
						<FontAwesomeIcon icon={faSync} /> Update All
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
						{providers.map(p => (
							<option key={p} value={p}>
								{p === 'all' ? 'All Providers' : p}
							</option>
						))}
					</select>
				</div>
			</div>

			<div className="dat-content">
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
										<span className={`provider-badge ${dat.provider.toLowerCase().replace('-', '')}`}>
											{dat.provider}
										</span>
									</td>
									<td className="name-cell">{dat.name}</td>
									<td className="version-cell">{dat.version}</td>
									<td className="games-cell">{dat.games.toLocaleString()}</td>
									<td className="status-cell">{getStatusIcon(dat.status)}</td>
									<td className="actions-cell">
										<button className="icon-btn">
											<FontAwesomeIcon icon={faEllipsisV} />
										</button>
									</td>
								</tr>
							))}
						</tbody>
					</table>
				</div>

				{selectedDat && (
					<div className="dat-details">
						<h3>{selectedDat.name}</h3>
						<div className="detail-grid">
							<div className="detail-item">
								<label>Provider</label>
								<span>{selectedDat.provider}</span>
							</div>
							<div className="detail-item">
								<label>Version</label>
								<span>{selectedDat.version}</span>
							</div>
							<div className="detail-item">
								<label>Games</label>
								<span>{selectedDat.games.toLocaleString()}</span>
							</div>
							<div className="detail-item">
								<label>ROMs</label>
								<span>{selectedDat.roms.toLocaleString()}</span>
							</div>
							<div className="detail-item">
								<label>Last Updated</label>
								<span>{selectedDat.lastUpdated}</span>
							</div>
						</div>
						<div className="detail-actions">
							<button className="btn btn-secondary">View Games</button>
							<button className="btn btn-secondary">Check for Updates</button>
							<button className="btn btn-danger">Delete</button>
						</div>
					</div>
				)}
			</div>
		</div>
	);
};

export default DatManager;
