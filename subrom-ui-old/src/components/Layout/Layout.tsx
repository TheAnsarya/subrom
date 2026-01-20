import { useState, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faHome,
	faDatabase,
	faGamepad,
	faHdd,
	faCog,
	faBars
} from '@fortawesome/free-solid-svg-icons';
import './Layout.css';

interface LayoutProps {
	children: ReactNode;
}

const navItems = [
	{ path: '/', icon: faHome, label: 'Dashboard' },
	{ path: '/dats', icon: faDatabase, label: 'DAT Files' },
	{ path: '/roms', icon: faGamepad, label: 'ROM Collection' },
	{ path: '/drives', icon: faHdd, label: 'Drives' },
	{ path: '/settings', icon: faCog, label: 'Settings' },
] as const;

export default function Layout({ children }: LayoutProps) {
	const location = useLocation();
	const [sidebarOpen, setSidebarOpen] = useState(true);

	return (
		<div className="layout">
			<header className="header">
				<button
					className="menu-toggle"
					onClick={() => setSidebarOpen(!sidebarOpen)}
					aria-label="Toggle sidebar"
				>
					<FontAwesomeIcon icon={faBars} />
				</button>
				<div className="logo">
					<span className="logo-text">Subrom</span>
					<span className="logo-subtitle">ROM Manager</span>
				</div>
				<div className="header-search">
					<input
						type="text"
						placeholder="Search ROMs, DATs, games..."
						className="search-input"
					/>
				</div>
				<div className="header-actions">
					{/* Future: notifications, user menu */}
				</div>
			</header>

			<aside className={`sidebar ${sidebarOpen ? 'open' : 'collapsed'}`}>
				<nav className="sidebar-nav">
					{navItems.map((item) => (
						<Link
							key={item.path}
							to={item.path}
							className={`nav-item ${location.pathname === item.path ? 'active' : ''}`}
						>
							<FontAwesomeIcon icon={item.icon} className="nav-icon" />
							{sidebarOpen && <span className="nav-label">{item.label}</span>}
						</Link>
					))}
				</nav>
			</aside>

			<main className={`main-content ${sidebarOpen ? '' : 'expanded'}`}>
				{children}
			</main>
		</div>
	);
}
