import { ReactNode, useState } from 'react';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import styles from './Layout.module.css';

interface LayoutProps {
	children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
	const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

	return (
		<div className={styles.layout} data-collapsed={sidebarCollapsed}>
			<Sidebar collapsed={sidebarCollapsed} />
			<div className={styles.main}>
				<Header
					onToggleSidebar={() => setSidebarCollapsed(!sidebarCollapsed)}
					sidebarCollapsed={sidebarCollapsed}
				/>
				<main className={styles.content}>
					{children}
				</main>
			</div>
		</div>
	);
}
