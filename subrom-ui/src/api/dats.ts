import { api } from './client';
import type { DatFile } from '@/types/api';

export const datsApi = {
	getAll(): Promise<DatFile[]> {
		return api.get<DatFile[]>('/dats');
	},

	getById(id: number): Promise<DatFile> {
		return api.get<DatFile>(`/dats/${id}`);
	},

	import(file: File): Promise<DatFile> {
		const formData = new FormData();
		formData.append('file', file);
		return api.postForm<DatFile>('/dats/import', formData);
	},

	delete(id: number): Promise<void> {
		return api.delete(`/dats/${id}`);
	},
};
