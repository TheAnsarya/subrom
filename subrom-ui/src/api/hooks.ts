import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { drivesApi, datsApi, romsApi, scansApi, type Drive, type DatFile, type RomFile, type ScanJob, type RomStats, type PagedResult } from './client';

// Query Keys
export const queryKeys = {
	drives: ['drives'] as const,
	drive: (id: string) => ['drives', id] as const,
	dats: (params?: { provider?: string; enabled?: boolean }) => ['dats', params] as const,
	dat: (id: string) => ['dats', id] as const,
	datGames: (id: string, params?: { search?: string; page?: number }) => ['dats', id, 'games', params] as const,
	datProviders: ['dats', 'providers'] as const,
	roms: (params?: { driveId?: string; search?: string; online?: boolean; verified?: boolean; page?: number }) => ['roms', params] as const,
	rom: (id: string) => ['roms', id] as const,
	romStats: ['roms', 'stats'] as const,
	scans: (params?: { status?: string; limit?: number }) => ['scans', params] as const,
	scan: (id: string) => ['scans', id] as const,
};

// Drives Hooks
export function useDrives() {
	return useQuery({
		queryKey: queryKeys.drives,
		queryFn: drivesApi.getAll,
	});
}

export function useDrive(id: string) {
	return useQuery({
		queryKey: queryKeys.drive(id),
		queryFn: () => drivesApi.get(id),
		enabled: !!id,
	});
}

export function useCreateDrive() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: drivesApi.create,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: queryKeys.drives });
		},
	});
}

export function useUpdateDrive() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: ({ id, ...data }: { id: string; label?: string; isEnabled?: boolean }) =>
			drivesApi.update(id, data),
		onSuccess: (_, { id }) => {
			queryClient.invalidateQueries({ queryKey: queryKeys.drives });
			queryClient.invalidateQueries({ queryKey: queryKeys.drive(id) });
		},
	});
}

export function useRefreshDrive() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: drivesApi.refresh,
		onSuccess: (data) => {
			queryClient.invalidateQueries({ queryKey: queryKeys.drives });
			queryClient.setQueryData(queryKeys.drive(data.id), data);
		},
	});
}

export function useDeleteDrive() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: drivesApi.delete,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: queryKeys.drives });
			queryClient.invalidateQueries({ queryKey: queryKeys.romStats });
		},
	});
}

// DATs Hooks
export function useDats(params?: { provider?: string; enabled?: boolean }) {
	return useQuery({
		queryKey: queryKeys.dats(params),
		queryFn: () => datsApi.getAll(params),
	});
}

export function useDat(id: string) {
	return useQuery({
		queryKey: queryKeys.dat(id),
		queryFn: () => datsApi.get(id),
		enabled: !!id,
	});
}

export function useDatProviders() {
	return useQuery({
		queryKey: queryKeys.datProviders,
		queryFn: datsApi.getProviders,
	});
}

export function useToggleDat() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: datsApi.toggle,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['dats'] });
		},
	});
}

export function useDeleteDat() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: datsApi.delete,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['dats'] });
		},
	});
}

// ROMs Hooks
export function useRoms(params?: { driveId?: string; search?: string; online?: boolean; verified?: boolean; page?: number; pageSize?: number }) {
	return useQuery({
		queryKey: queryKeys.roms(params),
		queryFn: () => romsApi.getAll(params),
	});
}

export function useRom(id: string) {
	return useQuery({
		queryKey: queryKeys.rom(id),
		queryFn: () => romsApi.get(id),
		enabled: !!id,
	});
}

export function useRomStats() {
	return useQuery({
		queryKey: queryKeys.romStats,
		queryFn: romsApi.getStats,
	});
}

// Scans Hooks
export function useScans(params?: { status?: string; limit?: number }) {
	return useQuery({
		queryKey: queryKeys.scans(params),
		queryFn: () => scansApi.getAll(params),
		refetchInterval: (query) => {
			// Poll more frequently if there are running scans
			const data = query.state.data;
			if (data?.some((s: ScanJob) => s.status === 'Running')) {
				return 2000; // 2 seconds
			}
			return false;
		},
	});
}

export function useScan(id: string) {
	return useQuery({
		queryKey: queryKeys.scan(id),
		queryFn: () => scansApi.get(id),
		enabled: !!id,
		refetchInterval: (query) => {
			const data = query.state.data;
			if (data?.status === 'Running') {
				return 1000; // 1 second for active scan
			}
			return false;
		},
	});
}

export function useCreateScan() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: scansApi.create,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['scans'] });
		},
	});
}

export function useCancelScan() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: scansApi.cancel,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['scans'] });
		},
	});
}

// Re-export types
export type { Drive, DatFile, RomFile, ScanJob, RomStats, PagedResult };
