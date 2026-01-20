import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

interface Settings {
	theme: 'light' | 'dark' | 'system';
	scanOptions: {
		recursive: boolean;
		verifyHashes: boolean;
		skipExisting: boolean;
	};
	display: {
		pageSize: number;
		showOfflineFiles: boolean;
		confirmDeletes: boolean;
	};
}

interface AppState {
	settings: Settings;
	activeScanId: string | null;
	selectedDriveId: string | null;
	selectedDatId: string | null;

	// Actions
	setTheme: (theme: Settings['theme']) => void;
	setScanOptions: (options: Partial<Settings['scanOptions']>) => void;
	setDisplaySettings: (settings: Partial<Settings['display']>) => void;
	setActiveScan: (id: string | null) => void;
	setSelectedDrive: (id: string | null) => void;
	setSelectedDat: (id: string | null) => void;
}

const defaultSettings: Settings = {
	theme: 'system',
	scanOptions: {
		recursive: true,
		verifyHashes: true,
		skipExisting: false,
	},
	display: {
		pageSize: 50,
		showOfflineFiles: true,
		confirmDeletes: true,
	},
};

export const useAppStore = create<AppState>()(
	persist(
		(set) => ({
			settings: defaultSettings,
			activeScanId: null,
			selectedDriveId: null,
			selectedDatId: null,

			setTheme: (theme) =>
				set((state) => ({
					settings: { ...state.settings, theme },
				})),

			setScanOptions: (options) =>
				set((state) => ({
					settings: {
						...state.settings,
						scanOptions: { ...state.settings.scanOptions, ...options },
					},
				})),

			setDisplaySettings: (display) =>
				set((state) => ({
					settings: {
						...state.settings,
						display: { ...state.settings.display, ...display },
					},
				})),

			setActiveScan: (activeScanId) => set({ activeScanId }),
			setSelectedDrive: (selectedDriveId) => set({ selectedDriveId }),
			setSelectedDat: (selectedDatId) => set({ selectedDatId }),
		}),
		{
			name: 'subrom-storage',
			storage: createJSONStorage(() => localStorage),
			partialize: (state) => ({ settings: state.settings }),
		}
	)
);

// Selectors
export const useTheme = () => useAppStore((state) => state.settings.theme);
export const useScanOptions = () => useAppStore((state) => state.settings.scanOptions);
export const useDisplaySettings = () => useAppStore((state) => state.settings.display);
