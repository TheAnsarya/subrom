import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import {
	faCog,
	faDatabase,
	faFolder,
	faPalette,
	faDownload,
	faBell,
	faShieldAlt,
	faSave,
	faUndo
} from '@fortawesome/free-solid-svg-icons';
import { useAppStore, useTheme, useScanOptions, useDisplaySettings } from '../../store';
import './Settings.css';

interface SettingSection {
	id: string;
	title: string;
	icon: IconDefinition;
}

export default function Settings() {
	const [activeSection, setActiveSection] = useState('general');
	const theme = useTheme();
	const scanOptions = useScanOptions();
	const displaySettings = useDisplaySettings();
	const { setTheme, setScanOptions, setDisplaySettings } = useAppStore();

	const sections: SettingSection[] = [
		{ id: 'general', title: 'General', icon: faCog },
		{ id: 'database', title: 'Database', icon: faDatabase },
		{ id: 'scanning', title: 'Scanning', icon: faFolder },
		{ id: 'providers', title: 'DAT Providers', icon: faDownload },
		{ id: 'appearance', title: 'Appearance', icon: faPalette },
		{ id: 'notifications', title: 'Notifications', icon: faBell },
		{ id: 'advanced', title: 'Advanced', icon: faShieldAlt },
	];

	const renderGeneralSettings = () => (
		<div className="settings-group">
			<h3>General Settings</h3>
			
			<div className="setting-item">
				<label>
					<span className="setting-label">Theme</span>
					<span className="setting-description">Choose your preferred color scheme</span>
				</label>
				<select
					value={theme}
					onChange={(e) => setTheme(e.target.value as 'light' | 'dark' | 'system')}
				>
					<option value="system">System Default</option>
					<option value="light">Light</option>
					<option value="dark">Dark</option>
				</select>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Page Size</span>
					<span className="setting-description">Number of items per page in lists</span>
				</label>
				<select
					value={displaySettings.pageSize}
					onChange={(e) => setDisplaySettings({ pageSize: parseInt(e.target.value) })}
				>
					<option value="25">25</option>
					<option value="50">50</option>
					<option value="100">100</option>
					<option value="200">200</option>
				</select>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Show Offline Files</span>
					<span className="setting-description">Display ROMs from offline drives</span>
				</label>
				<input
					type="checkbox"
					checked={displaySettings.showOfflineFiles}
					onChange={(e) => setDisplaySettings({ showOfflineFiles: e.target.checked })}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Confirm Deletes</span>
					<span className="setting-description">Ask for confirmation before deleting items</span>
				</label>
				<input
					type="checkbox"
					checked={displaySettings.confirmDeletes}
					onChange={(e) => setDisplaySettings({ confirmDeletes: e.target.checked })}
					className="toggle"
				/>
			</div>
		</div>
	);

	const renderScanningSettings = () => (
		<div className="settings-group">
			<h3>Scanning Settings</h3>
			
			<div className="setting-item">
				<label>
					<span className="setting-label">Recursive Scanning</span>
					<span className="setting-description">Scan all subdirectories</span>
				</label>
				<input
					type="checkbox"
					checked={scanOptions.recursive}
					onChange={(e) => setScanOptions({ recursive: e.target.checked })}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Verify Hashes</span>
					<span className="setting-description">Compute CRC32, MD5, and SHA1 for each file</span>
				</label>
				<input
					type="checkbox"
					checked={scanOptions.verifyHashes}
					onChange={(e) => setScanOptions({ verifyHashes: e.target.checked })}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Skip Existing Files</span>
					<span className="setting-description">Don't re-scan files already in database</span>
				</label>
				<input
					type="checkbox"
					checked={scanOptions.skipExisting}
					onChange={(e) => setScanOptions({ skipExisting: e.target.checked })}
					className="toggle"
				/>
			</div>
		</div>
	);

	const renderDatabaseSettings = () => (
		<div className="settings-group">
			<h3>Database Settings</h3>
			
			<div className="setting-item">
				<label>
					<span className="setting-label">Database Location</span>
					<span className="setting-description">SQLite database is stored locally</span>
				</label>
				<div className="input-readonly">
					<span>subrom.db (local storage)</span>
				</div>
			</div>

			<div className="info-box">
				<p>
					<strong>Offline Drive Protection:</strong> ROM records are never automatically deleted 
					when drives go offline. Your collection database remains intact even if storage 
					is temporarily unavailable.
				</p>
			</div>
		</div>
	);

	const renderProviderSettings = () => (
		<div className="settings-group">
			<h3>DAT Provider Information</h3>
			
			<div className="provider-list">
				<div className="provider-item">
					<div className="provider-info">
						<span className="provider-name">No-Intro</span>
						<span className="provider-description">Cartridge-based systems (NES, SNES, N64, GBA, etc.)</span>
					</div>
				</div>
				
				<div className="provider-item">
					<div className="provider-info">
						<span className="provider-name">Redump</span>
						<span className="provider-description">Optical media (PlayStation, Sega CD, etc.)</span>
					</div>
				</div>
				
				<div className="provider-item">
					<div className="provider-info">
						<span className="provider-name">TOSEC</span>
						<span className="provider-description">Comprehensive preservation project</span>
					</div>
				</div>
			</div>

			<div className="info-box">
				<p>Import DAT files from the DAT Manager page to add verification databases.</p>
			</div>
		</div>
	);

	const renderContent = () => {
		switch (activeSection) {
			case 'general':
				return renderGeneralSettings();
			case 'database':
				return renderDatabaseSettings();
			case 'scanning':
				return renderScanningSettings();
			case 'providers':
				return renderProviderSettings();
			default:
				return (
					<div className="settings-group">
						<h3>{sections.find(s => s.id === activeSection)?.title} Settings</h3>
						<p className="placeholder-text">Settings for this section coming soon...</p>
					</div>
				);
		}
	};
		</div>
	);

	return (
		<div className="settings-page">
			<div className="page-header">
				<h1>Settings</h1>
				<p className="page-subtitle">Configure Subrom preferences</p>
			</div>

			<div className="settings-container">
				<nav className="settings-nav">
					{sections.map(section => (
						<button
							key={section.id}
							className={`nav-item ${activeSection === section.id ? 'active' : ''}`}
							onClick={() => setActiveSection(section.id)}
						>
							<FontAwesomeIcon icon={section.icon} />
							<span>{section.title}</span>
						</button>
					))}
				</nav>

				<div className="settings-content">
					{renderContent()}
				</div>
			</div>
		</div>
	);
}
