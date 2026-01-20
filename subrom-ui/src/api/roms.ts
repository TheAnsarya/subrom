import { api } from './client';
import type { RomFile, PagedResult } from '@/types/api';

export const romsApi = {
	getAll(page = 1, pageSize = 50): Promise<PagedResult<RomFile>> {
		return api.get<PagedResult<RomFile>>(`/roms?page=${page}&pageSize=${pageSize}`);
	},

	getById(id: number): Promise<RomFile> {
		return api.get<RomFile>(`/roms/${id}`);
	},

	search(query: string, page = 1, pageSize = 50): Promise<PagedResult<RomFile>> {
		return api.get<PagedResult<RomFile>>(
			`/roms/search?query=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`
		);
	},
};
