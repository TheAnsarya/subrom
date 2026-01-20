import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type Theme = 'light' | 'dark';

interface ThemeState {
	theme: Theme;
	toggleTheme: () => void;
	setTheme: (theme: Theme) => void;
}

export const useThemeStore = create<ThemeState>()(
	persist(
		(set, get) => ({
			theme: 'light',
			toggleTheme: () => {
				const newTheme = get().theme === 'light' ? 'dark' : 'light';
				document.documentElement.setAttribute('data-theme', newTheme);
				set({ theme: newTheme });
			},
			setTheme: (theme) => {
				document.documentElement.setAttribute('data-theme', theme);
				set({ theme });
			},
		}),
		{
			name: 'subrom-theme',
			onRehydrateStorage: () => (state) => {
				if (state) {
					document.documentElement.setAttribute('data-theme', state.theme);
				}
			},
		}
	)
);
