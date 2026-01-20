import React from 'react';
import { BrowserRouter as Router, Switch, Route, Redirect } from 'react-router-dom';
import Layout from './components/Layout/Layout';
import Dashboard from './pages/Dashboard/Dashboard';
import DatManager from './pages/DatManager/DatManager';
import RomBrowser from './pages/RomBrowser/RomBrowser';
import DriveManager from './pages/DriveManager/DriveManager';
import Settings from './pages/Settings/Settings';
import './App.css';

function App() {
	return (
		<Router>
			<Layout>
				<Switch>
					<Route exact path="/" component={Dashboard} />
					<Route path="/dats" component={DatManager} />
					<Route path="/roms" component={RomBrowser} />
					<Route path="/drives" component={DriveManager} />
					<Route path="/settings" component={Settings} />
					<Redirect to="/" />
				</Switch>
			</Layout>
		</Router>
	);
}

export default App;
