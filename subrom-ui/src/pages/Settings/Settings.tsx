import React, { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
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
import './Settings.css';

interface SettingSection {
	id: string;
	title: string;
	icon: any;
}

const Settings: React.FC = () => {
	const [activeSection, setActiveSection] = useState('general');
	const [hasChanges, setHasChanges] = useState(false);

	// Settings state
	const [settings, setSettings] = useState({
		// General
		darkMode: true,
		language: 'en',
		autoSave: true,
		
		// Database
		dbLocation: 'C:\\Users\\me\\AppData\\Local\\Subrom\\subrom.db',
		autoBackup: true,
		backupInterval: 'daily',
		keepBackups: 7,
		
		// Scanning
		defaultScanPath: 'D:\\ROMs',
		scanArchives: true,
		scanSubfolders: true,
		excludePatterns: '*.txt, *.nfo, *.jpg',
		hashAlgorithms: ['crc32', 'md5', 'sha1'],
		
		// DAT Providers
		enableNoIntro: true,
		enableRedump: true,
		enableTosec: true,
		enableGoodsets: false,
		autoUpdateDats: true,
		updateInterval: 'weekly',
		
		// Notifications
		notifyOnComplete: true,
		notifyOnError: true,
		notifyOnNewDat: false,
		
		// Advanced
		maxConcurrency: 4,
		enableLogging: true,
		logLevel: 'info',
	});

	const sections: SettingSection[] = [
		{ id: 'general', title: 'General', icon: faCog },
		{ id: 'database', title: 'Database', icon: faDatabase },
		{ id: 'scanning', title: 'Scanning', icon: faFolder },
		{ id: 'providers', title: 'DAT Providers', icon: faDownload },
		{ id: 'appearance', title: 'Appearance', icon: faPalette },
		{ id: 'notifications', title: 'Notifications', icon: faBell },
		{ id: 'advanced', title: 'Advanced', icon: faShieldAlt },
	];

	const handleChange = (key: string, value: any) => {
		setSettings(prev => ({ ...prev, [key]: value }));
		setHasChanges(true);
	};

	const handleSave = () => {
		// TODO: Save settings to backend
		setHasChanges(false);
	};

	const handleReset = () => {
		// TODO: Reset to defaults
		setHasChanges(false);
	};

	const renderGeneralSettings = () => (
		<div className="settings-group">
			<h3>General Settings</h3>
			
			<div className="setting-item">
				<label>
					<span className="setting-label">Dark Mode</span>
					<span className="setting-description">Use dark theme throughout the application</span>
				</label>
				<input
					type="checkbox"
					checked={settings.darkMode}
					onChange={(e) => handleChange('darkMode', e.target.checked)}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Language</span>
					<span className="setting-description">Select your preferred language</span>
				</label>
				<select
					value={settings.language}
					onChange={(e) => handleChange('language', e.target.value)}
				>
					<option value="en">English</option>
					<option value="es">Español</option>
					<option value="de">Deutsch</option>
					<option value="fr">Français</option>
					<option value="ja">日本語</option>
				</select>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Auto-Save</span>
					<span className="setting-description">Automatically save changes</span>
				</label>
				<input
					type="checkbox"
					checked={settings.autoSave}
					onChange={(e) => handleChange('autoSave', e.target.checked)}
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
					<span className="setting-description">Path to the SQLite database file</span>
				</label>
				<div className="input-with-button">
					<input
						type="text"
						value={settings.dbLocation}
						onChange={(e) => handleChange('dbLocation', e.target.value)}
					/>
					<button className="btn btn-secondary btn-small">Browse</button>
				</div>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Automatic Backup</span>
					<span className="setting-description">Regularly backup the database</span>
				</label>
				<input
					type="checkbox"
					checked={settings.autoBackup}
					onChange={(e) => handleChange('autoBackup', e.target.checked)}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Backup Interval</span>
					<span className="setting-description">How often to create backups</span>
				</label>
				<select
					value={settings.backupInterval}
					onChange={(e) => handleChange('backupInterval', e.target.value)}
					disabled={!settings.autoBackup}
				>
					<option value="hourly">Hourly</option>
					<option value="daily">Daily</option>
					<option value="weekly">Weekly</option>
				</select>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Keep Backups</span>
					<span className="setting-description">Number of backup copies to retain</span>
				</label>
				<input
					type="number"
					min="1"
					max="30"
					value={settings.keepBackups}
					onChange={(e) => handleChange('keepBackups', parseInt(e.target.value))}
					disabled={!settings.autoBackup}
				/>
			</div>
		</div>
	);

	const renderScanningSettings = () => (
		<div className="settings-group">
			<h3>Scanning Settings</h3>
			
			<div className="setting-item">
				<label>
					<span className="setting-label">Default Scan Path</span>
					<span className="setting-description">Default directory for ROM scanning</span>
				</label>
				<div className="input-with-button">
					<input
						type="text"
						value={settings.defaultScanPath}
						onChange={(e) => handleChange('defaultScanPath', e.target.value)}
					/>
					<button className="btn btn-secondary btn-small">Browse</button>
				</div>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Scan Archives</span>
					<span className="setting-description">Scan inside ZIP, 7z, and RAR files</span>
				</label>
				<input
					type="checkbox"
					checked={settings.scanArchives}
					onChange={(e) => handleChange('scanArchives', e.target.checked)}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Scan Subfolders</span>
					<span className="setting-description">Recursively scan subdirectories</span>
				</label>
				<input
					type="checkbox"
					checked={settings.scanSubfolders}
					onChange={(e) => handleChange('scanSubfolders', e.target.checked)}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Exclude Patterns</span>
					<span className="setting-description">File patterns to skip (comma-separated)</span>
				</label>
				<input
					type="text"
					value={settings.excludePatterns}
					onChange={(e) => handleChange('excludePatterns', e.target.value)}
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Hash Algorithms</span>
					<span className="setting-description">Which hashes to compute for each file</span>
				</label>
				<div className="checkbox-group">
					<label className="checkbox-label">
						<input
							type="checkbox"
							checked={settings.hashAlgorithms.includes('crc32')}
							onChange={() => {}}
						/>
						CRC32
					</label>
					<label className="checkbox-label">
						<input
							type="checkbox"
							checked={settings.hashAlgorithms.includes('md5')}
							onChange={() => {}}
						/>
						MD5
					</label>
					<label className="checkbox-label">
						<input
							type="checkbox"
							checked={settings.hashAlgorithms.includes('sha1')}
							onChange={() => {}}
						/>
						SHA-1
					</label>
				</div>
			</div>
		</div>
	);

	const renderProviderSettings = () => (
		<div className="settings-group">
			<h3>DAT Provider Settings</h3>
			
			<div className="provider-list">
				<div className="provider-item">
					<div className="provider-info">
						<span className="provider-name">No-Intro</span>
						<span className="provider-description">Cartridge-based systems (NES, SNES, N64, GBA, etc.)</span>
					</div>
					<input
						type="checkbox"
						checked={settings.enableNoIntro}
						onChange={(e) => handleChange('enableNoIntro', e.target.checked)}
						className="toggle"
					/>
				</div>
				
				<div className="provider-item">
					<div className="provider-info">
						<span className="provider-name">Redump</span>
						<span className="provider-description">Optical media (PlayStation, Sega CD, etc.)</span>
					</div>
					<input
						type="checkbox"
						checked={settings.enableRedump}
						onChange={(e) => handleChange('enableRedump', e.target.checked)}
						className="toggle"
					/>
				</div>
				
				<div className="provider-item">
					<div className="provider-info">
						<span className="provider-name">TOSEC</span>
						<span className="provider-description">Comprehensive preservation project</span>
					</div>
					<input
						type="checkbox"
						checked={settings.enableTosec}
						onChange={(e) => handleChange('enableTosec', e.target.checked)}
						className="toggle"
					/>
				</div>
				
				<div className="provider-item">
					<div className="provider-info">
						<span className="provider-name">GoodSets</span>
						<span className="provider-description">Legacy ROM sets (discontinued)</span>
					</div>
					<input
						type="checkbox"
						checked={settings.enableGoodsets}
						onChange={(e) => handleChange('enableGoodsets', e.target.checked)}
						className="toggle"
					/>
				</div>
			</div>

			<div className="setting-item" style={{ marginTop: '1.5rem' }}>
				<label>
					<span className="setting-label">Auto-Update DATs</span>
					<span className="setting-description">Automatically download new DAT versions</span>
				</label>
				<input
					type="checkbox"
					checked={settings.autoUpdateDats}
					onChange={(e) => handleChange('autoUpdateDats', e.target.checked)}
					className="toggle"
				/>
			</div>

			<div className="setting-item">
				<label>
					<span className="setting-label">Update Interval</span>
					<span className="setting-description">How often to check for DAT updates</span>
				</label>
				<select
					value={settings.updateInterval}
					onChange={(e) => handleChange('updateInterval', e.target.value)}
					disabled={!settings.autoUpdateDats}
				>
					<option value="daily">Daily</option>
					<option value="weekly">Weekly</option>
					<option value="monthly">Monthly</option>
				</select>
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

					{hasChanges && (
						<div className="settings-actions">
							<button className="btn btn-secondary" onClick={handleReset}>
								<FontAwesomeIcon icon={faUndo} />
								Discard Changes
							</button>
							<button className="btn btn-primary" onClick={handleSave}>
								<FontAwesomeIcon icon={faSave} />
								Save Changes
							</button>
						</div>
					)}
				</div>
			</div>
		</div>
	);
};

export default Settings;
