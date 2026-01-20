import { NavLink } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faHome,
	faDatabase,
	faHardDrive,
	faCheckCircle,
	faCog,
} from '@fortawesome/free-solid-svg-icons';
import styles from './Sidebar.module.css';

interface SidebarProps {
	collapsed: boolean;
}

const navItems = [
	{ to: '/', icon: faHome, label: 'Dashboard' },
	{ to: '/dats', icon: faDatabase, label: 'DAT Files' },
	{ to: '/roms', icon: faHardDrive, label: 'ROM Files' },
	{ to: '/verification', icon: faCheckCircle, label: 'Verification' },
	{ to: '/settings', icon: faCog, label: 'Settings' },
];

export function Sidebar({ collapsed }: SidebarProps) {
	return (
		<aside className={styles.sidebar} data-collapsed={collapsed}>
			<div className={styles.logo}>
				<span className={styles.logoIcon}>S</span>
				{!collapsed && <span className={styles.logoText}>Subrom</span>}
			</div>
			<nav className={styles.nav}>
				{navItems.map((item) => (
					<NavLink
						key={item.to}
						to={item.to}
						className={({ isActive }) =>
							`${styles.navItem} ${isActive ? styles.active : ''}`
						}
						title={collapsed ? item.label : undefined}
					>
						<FontAwesomeIcon icon={item.icon} className={styles.navIcon} />
						{!collapsed && <span className={styles.navLabel}>{item.label}</span>}
					</NavLink>
				))}
			</nav>
		</aside>
	);
}
