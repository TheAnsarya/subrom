import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { Layout } from './Layout';

describe('Layout', () => {
	it('renders navigation links', () => {
		render(
			<BrowserRouter>
				<Layout>
					<div>Test Content</div>
				</Layout>
			</BrowserRouter>
		);

		// Check for navigation links
		expect(screen.getByText('Dashboard')).toBeInTheDocument();
		expect(screen.getByText('DAT Files')).toBeInTheDocument();
		expect(screen.getByText('ROM Files')).toBeInTheDocument();
		expect(screen.getByText('Verification')).toBeInTheDocument();
		expect(screen.getByText('Settings')).toBeInTheDocument();
	});

	it('renders children content', () => {
		render(
			<BrowserRouter>
				<Layout>
					<div>Test Content</div>
				</Layout>
			</BrowserRouter>
		);

		expect(screen.getByText('Test Content')).toBeInTheDocument();
	});
});
