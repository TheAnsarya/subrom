import React, { useState } from 'react';
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
	faFolderOpen
} from '@fortawesome/free-solid-svg-icons';
import './RomBrowser.css';

interface SystemNode {
	id: string;
	name: string;
	icon?: any;
	children?: SystemNode[];
	romCount?: number;
	completePercent?: number;
}

interface Rom {
	id: string;
	name: string;
	status: 'verified' | 'missing' | 'bad' | 'unknown';
	size?: string;
	crc?: string;
	region?: string;
}

const RomBrowser: React.FC = () => {
	const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set(['nintendo']));
	const [selectedSystem, setSelectedSystem] = useState<string | null>('nes');
	const [searchTerm, setSearchTerm] = useState('');
	const [statusFilter, setStatusFilter] = useState('all');

	// Mock system tree
	const systemTree: SystemNode[] = [
		{
			id: 'nintendo',
			name: 'Nintendo',
			children: [
				{ id: 'nes', name: 'NES', romCount: 2847, completePercent: 92 },
				{ id: 'snes', name: 'SNES', romCount: 3521, completePercent: 88 },
				{ id: 'n64', name: 'N64', romCount: 387, completePercent: 76 },
				{ id: 'gb', name: 'Game Boy', romCount: 1456, completePercent: 95 },
				{ id: 'gba', name: 'GBA', romCount: 2531, completePercent: 89 },
			]
		},
		{
			id: 'sega',
			name: 'Sega',
			children: [
				{ id: 'genesis', name: 'Genesis', romCount: 1856, completePercent: 78 },
				{ id: 'mastersystem', name: 'Master System', romCount: 567, completePercent: 82 },
				{ id: 'gamegear', name: 'Game Gear', romCount: 432, completePercent: 91 },
			]
		},
		{
			id: 'sony',
			name: 'Sony',
			children: [
				{ id: 'ps1', name: 'PlayStation', romCount: 4521, completePercent: 45 },
				{ id: 'psp', name: 'PSP', romCount: 2156, completePercent: 38 },
			]
		},
	];

	// Mock ROMs
	const roms: Rom[] = [
		{ id: '1', name: 'Super Mario Bros. (USA)', status: 'verified', size: '40 KB', crc: 'D445F698', region: 'USA' },
		{ id: '2', name: 'Super Mario Bros. 2 (USA)', status: 'verified', size: '128 KB', crc: 'E0CA425F', region: 'USA' },
		{ id: '3', name: 'Super Mario Bros. 3 (USA)', status: 'bad', size: '384 KB', crc: '00000000', region: 'USA' },
		{ id: '4', name: 'Legend of Zelda, The (USA)', status: 'verified', size: '128 KB', crc: 'A12B3C4D', region: 'USA' },
		{ id: '5', name: 'Zelda II - The Adventure of Link (USA)', status: 'missing', region: 'USA' },
		{ id: '6', name: 'Metroid (USA)', status: 'verified', size: '128 KB', crc: 'B5C6D7E8', region: 'USA' },
		{ id: '7', name: 'Mega Man (USA)', status: 'verified', size: '128 KB', crc: 'F9A0B1C2', region: 'USA' },
		{ id: '8', name: 'Mega Man 2 (USA)', status: 'missing', region: 'USA' },
		{ id: '9', name: 'Castlevania (USA)', status: 'verified', size: '128 KB', crc: 'D3E4F5A6', region: 'USA' },
		{ id: '10', name: 'Contra (USA)', status: 'verified', size: '128 KB', crc: '78901234', region: 'USA' },
	];

	const toggleNode = (id: string) => {
		setExpandedNodes(prev => {
			const next = new Set(prev);
			if (next.has(id)) {
				next.delete(id);
			} else {
				next.add(id);
			}
			return next;
		});
	};

	const getStatusIcon = (status: string) => {
		switch (status) {
			case 'verified':
				return <FontAwesomeIcon icon={faCheckCircle} className="rom-status verified" />;
			case 'missing':
				return <FontAwesomeIcon icon={faTimesCircle} className="rom-status missing" />;
			case 'bad':
				return <FontAwesomeIcon icon={faExclamationTriangle} className="rom-status bad" />;
			default:
				return null;
		}
	};

	const renderSystemNode = (node: SystemNode, depth = 0) => {
		const hasChildren = node.children && node.children.length > 0;
		const isExpanded = expandedNodes.has(node.id);
		const isSelected = selectedSystem === node.id;

		return (
			<div key={node.id} className="tree-node">
				<div
					className={`tree-item ${isSelected ? 'selected' : ''}`}
					style={{ paddingLeft: `${depth * 16 + 8}px` }}
					onClick={() => {
						if (hasChildren) {
							toggleNode(node.id);
						} else {
							setSelectedSystem(node.id);
						}
					}}
				>
					{hasChildren ? (
						<FontAwesomeIcon
							icon={isExpanded ? faChevronDown : faChevronRight}
							className="tree-toggle"
						/>
					) : (
						<span className="tree-toggle-spacer" />
					)}
					<FontAwesomeIcon
						icon={hasChildren ? (isExpanded ? faFolderOpen : faFolder) : faGamepad}
						className="tree-icon"
					/>
					<span className="tree-label">{node.name}</span>
					{node.completePercent !== undefined && (
						<span className="tree-percent">{node.completePercent}%</span>
					)}
				</div>
				{hasChildren && isExpanded && (
					<div className="tree-children">
						{node.children!.map(child => renderSystemNode(child, depth + 1))}
					</div>
				)}
			</div>
		);
	};

	const filteredRoms = roms.filter(rom => {
		const matchesSearch = rom.name.toLowerCase().includes(searchTerm.toLowerCase());
		const matchesStatus = statusFilter === 'all' || rom.status === statusFilter;
		return matchesSearch && matchesStatus;
	});

	return (
		<div className="rom-browser">
			<div className="page-header">
				<h1>ROM Collection</h1>
				<p className="page-subtitle">Browse and verify your ROM files</p>
			</div>

			<div className="browser-content">
				<aside className="system-sidebar">
					<div className="sidebar-header">
						<h3>Systems</h3>
					</div>
					<div className="system-tree">
						{systemTree.map(node => renderSystemNode(node))}
					</div>
				</aside>

				<main className="rom-list-container">
					<div className="rom-filters">
						<div className="search-box">
							<FontAwesomeIcon icon={faSearch} className="search-icon" />
							<input
								type="text"
								placeholder="Search ROMs..."
								value={searchTerm}
								onChange={(e) => setSearchTerm(e.target.value)}
							/>
						</div>
						<select
							value={statusFilter}
							onChange={(e) => setStatusFilter(e.target.value)}
							className="status-filter"
						>
							<option value="all">All Status</option>
							<option value="verified">Verified</option>
							<option value="missing">Missing</option>
							<option value="bad">Bad Dump</option>
						</select>
					</div>

					<div className="rom-list">
						{filteredRoms.map(rom => (
							<div key={rom.id} className={`rom-item ${rom.status}`}>
								<div className="rom-icon">
									{getStatusIcon(rom.status)}
								</div>
								<div className="rom-info">
									<div className="rom-name">{rom.name}</div>
									<div className="rom-meta">
										{rom.size && <span className="rom-size">{rom.size}</span>}
										{rom.crc && <span className="rom-crc">CRC: {rom.crc}</span>}
									</div>
								</div>
								<div className="rom-region">
									{rom.region}
								</div>
							</div>
						))}
					</div>
				</main>
			</div>
		</div>
	);
};

export default RomBrowser;
