import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faBars, faMoon, faSun } from '@fortawesome/free-solid-svg-icons';
import { useThemeStore } from '@/stores/themeStore';
import styles from './Header.module.css';

interface HeaderProps {
	onToggleSidebar: () => void;
	sidebarCollapsed: boolean;
}

export function Header({ onToggleSidebar }: HeaderProps) {
	const { theme, toggleTheme } = useThemeStore();

	return (
		<header className={styles.header}>
			<button
				className={styles.menuButton}
				onClick={onToggleSidebar}
				aria-label="Toggle sidebar"
			>
				<FontAwesomeIcon icon={faBars} />
			</button>

			<div className={styles.spacer} />

			<button
				className={styles.themeButton}
				onClick={toggleTheme}
				aria-label={`Switch to ${theme === 'light' ? 'dark' : 'light'} theme`}
			>
				<FontAwesomeIcon icon={theme === 'light' ? faMoon : faSun} />
			</button>
		</header>
	);
}
