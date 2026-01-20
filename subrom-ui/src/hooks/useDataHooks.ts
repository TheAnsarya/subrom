import { useApi, useMutation, usePolling } from './useApi';
import { datsApi } from '@/api/dats';
import { romsApi } from '@/api/roms';
import { scanApi, type ScanRequest } from '@/api/scan';
import { verificationApi } from '@/api/verification';
import type { DatFile, RomFile, PagedResult, VerificationSummary } from '@/types/api';
import { toast } from '@/components/ui';

// ============== DAT Files ==============

export function useDatFiles() {
	return useApi<DatFile[]>(datsApi.getAll, []);
}

export function useDatFile(id: number) {
	return useApi<DatFile>(() => datsApi.getById(id), [id], {
		enabled: id > 0,
	});
}

export function useImportDat() {
	return useMutation(
		async (file: File) => {
			return datsApi.import(file);
		},
		{
			onSuccess: () => {
				toast.success('DAT Imported', 'DAT file imported successfully');
			},
			onError: (error) => {
				toast.error('Import Failed', error.message);
			},
		}
	);
}

export function useDeleteDat() {
	return useMutation(
		async (id: number) => {
			return datsApi.delete(id);
		},
		{
			onSuccess: () => {
				toast.success('DAT Deleted', 'DAT file deleted successfully');
			},
			onError: (error) => {
				toast.error('Delete Failed', error.message);
			},
		}
	);
}

// ============== ROM Files ==============

export interface RomFilesParams {
	page?: number;
	pageSize?: number;
	search?: string;
}

export function useRomFiles(params: RomFilesParams = {}) {
	const { page = 1, pageSize = 50, search } = params;

	return useApi<PagedResult<RomFile>>(
		() => search ? romsApi.search(search, page, pageSize) : romsApi.getAll(page, pageSize),
		[page, pageSize, search]
	);
}

export function useRomFile(id: number) {
	return useApi<RomFile>(() => romsApi.getById(id), [id], {
		enabled: id > 0,
	});
}

// ============== Scanning ==============

export function useScanStatus(enabled = true) {
	return usePolling(scanApi.getStatus, 1000, { enabled });
}

export function useStartScan() {
	return useMutation(
		async (request: ScanRequest) => {
			return scanApi.start(request);
		},
		{
			onSuccess: () => {
				toast.info('Scan Started', 'ROM scanning has begun');
			},
			onError: (error) => {
				toast.error('Scan Failed', error.message);
			},
		}
	);
}

export function useStopScan() {
	return useMutation(
		async () => {
			return scanApi.stop();
		},
		{
			onSuccess: () => {
				toast.info('Scan Stopped', 'ROM scanning has been stopped');
			},
			onError: (error) => {
				toast.error('Stop Failed', error.message);
			},
		}
	);
}

// ============== Verification ==============

export function useVerificationSummary() {
	return useApi<VerificationSummary>(verificationApi.getSummary, []);
}

export function useUnknownRoms(page = 1, pageSize = 50) {
	return useApi<PagedResult<RomFile>>(
		() => verificationApi.getUnknownRoms(page, pageSize),
		[page, pageSize]
	);
}

export function useMissingGames(page = 1, pageSize = 50) {
	return useApi(
		() => verificationApi.getMissingGames(page, pageSize),
		[page, pageSize]
	);
}

export function useVerifyRom() {
	return useMutation(
		async (romId: number) => {
			return verificationApi.verifyRom(romId);
		},
		{
			onSuccess: () => {
				toast.success('Verification Complete', 'ROM has been verified');
			},
			onError: (error) => {
				toast.error('Verification Failed', error.message);
			},
		}
	);
}
