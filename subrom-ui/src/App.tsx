import { Routes, Route } from 'react-router-dom';
import { Layout } from '@/components/Layout/Layout';
import { Dashboard } from '@/pages/Dashboard/Dashboard';
import { DatManager } from '@/pages/DatManager/DatManager';
import { RomFiles } from '@/pages/RomFiles/RomFiles';
import { Verification } from '@/pages/Verification/Verification';
import { Settings } from '@/pages/Settings/Settings';

export function App() {
	return (
		<Layout>
			<Routes>
				<Route path="/" element={<Dashboard />} />
				<Route path="/dats" element={<DatManager />} />
				<Route path="/roms" element={<RomFiles />} />
				<Route path="/verification" element={<Verification />} />
				<Route path="/settings" element={<Settings />} />
			</Routes>
		</Layout>
	);
}
