import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout/Layout';
import Dashboard from './pages/Dashboard/Dashboard';
import DatManager from './pages/DatManager/DatManager';
import RomBrowser from './pages/RomBrowser/RomBrowser';
import DriveManager from './pages/DriveManager/DriveManager';
import Settings from './pages/Settings/Settings';
import './App.css';

function App() {
	return (
		<BrowserRouter>
			<Layout>
				<Routes>
					<Route path="/" element={<Dashboard />} />
					<Route path="/dats" element={<DatManager />} />
					<Route path="/roms" element={<RomBrowser />} />
					<Route path="/drives" element={<DriveManager />} />
					<Route path="/settings" element={<Settings />} />
					<Route path="*" element={<Navigate to="/" replace />} />
				</Routes>
			</Layout>
		</BrowserRouter>
	);
}

export default App;
