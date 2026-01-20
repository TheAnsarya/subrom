import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faCog,
	faFolder,
	faDatabase,
	faPalette,
} from '@fortawesome/free-solid-svg-icons';
import { useThemeStore } from '@/stores/themeStore';
import styles from './Settings.module.css';

type Section = 'general' | 'scanning' | 'database' | 'appearance';

const sections = [
	{ id: 'general' as const, label: 'General', icon: faCog },
	{ id: 'scanning' as const, label: 'Scanning', icon: faFolder },
	{ id: 'database' as const, label: 'Database', icon: faDatabase },
	{ id: 'appearance' as const, label: 'Appearance', icon: faPalette },
];

export function Settings() {
	const [activeSection, setActiveSection] = useState<Section>('general');
	const { theme, setTheme } = useThemeStore();

	return (
		<div className={styles.page}>
			<h1>Settings</h1>
			<p className={styles.subtitle}>Configure your preferences</p>

			<div className={styles.container}>
				<nav className={styles.nav}>
					{sections.map((section) => (
						<button
							key={section.id}
							className={`${styles.navItem} ${activeSection === section.id ? styles.active : ''}`}
							onClick={() => setActiveSection(section.id)}
						>
							<FontAwesomeIcon icon={section.icon} />
							<span>{section.label}</span>
						</button>
					))}
				</nav>

				<div className={styles.content}>
					{activeSection === 'general' && (
						<div className={styles.section}>
							<h2>General Settings</h2>
							<div className={styles.setting}>
								<label>Auto-scan on startup</label>
								<input type="checkbox" />
							</div>
							<div className={styles.setting}>
								<label>Show notifications</label>
								<input type="checkbox" defaultChecked />
							</div>
						</div>
					)}

					{activeSection === 'scanning' && (
						<div className={styles.section}>
							<h2>Scanning Settings</h2>
							<div className={styles.setting}>
								<label>Default scan folders</label>
								<p className={styles.hint}>Folders to scan for ROMs automatically</p>
								<div className={styles.folderList}>
									<p className={styles.empty}>No folders configured</p>
								</div>
								<button className={styles.addButton}>Add Folder</button>
							</div>
							<div className={styles.setting}>
								<label>Scan archives (ZIP, 7z)</label>
								<input type="checkbox" defaultChecked />
							</div>
							<div className={styles.setting}>
								<label>Calculate all hashes (MD5, SHA1, CRC)</label>
								<input type="checkbox" defaultChecked />
							</div>
						</div>
					)}

					{activeSection === 'database' && (
						<div className={styles.section}>
							<h2>Database Settings</h2>
							<div className={styles.setting}>
								<label>Database location</label>
								<p className={styles.path}>%LocalAppData%\Subrom\subrom.db</p>
							</div>
							<div className={styles.actions}>
								<button className={styles.actionButton}>Export Database</button>
								<button className={styles.actionButton}>Import Database</button>
								<button className={`${styles.actionButton} ${styles.danger}`}>
									Clear All Data
								</button>
							</div>
						</div>
					)}

					{activeSection === 'appearance' && (
						<div className={styles.section}>
							<h2>Appearance Settings</h2>
							<div className={styles.setting}>
								<label>Theme</label>
								<select
									value={theme}
									onChange={(e) => setTheme(e.target.value as 'light' | 'dark')}
								>
									<option value="light">Light</option>
									<option value="dark">Dark</option>
								</select>
							</div>
						</div>
					)}
				</div>
			</div>
		</div>
	);
}
