const API_BASE = '/api';

async function fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
	const response = await fetch(`${API_BASE}${endpoint}`, {
		...options,
		headers: {
			'Content-Type': 'application/json',
			...options?.headers,
		},
	});

	if (!response.ok) {
		const error = await response.text();
		throw new Error(error || `HTTP ${response.status}`);
	}

	return response.json();
}

// Types
export interface Drive {
	id: string;
	label: string;
	path: string;
	isOnline: boolean;
	lastSeen: string;
	lastScanned: string | null;
	totalCapacity: number;
	freeSpace: number;
	romCount: number;
	isEnabled: boolean;
}

export interface DatFile {
	id: string;
	name: string;
	description: string | null;
	provider: string | null;
	version: string | null;
	importedAt: string;
	gameCount: number;
	romCount: number;
	isEnabled: boolean;
}

export interface RomFile {
	id: string;
	driveId: string;
	fileName: string;
	path: string;
	size: number;
	crc32: string | null;
	md5: string | null;
	sha1: string | null;
	isOnline: boolean;
	verifiedAt: string | null;
	modifiedAt: string;
}

export interface ScanJob {
	id: string;
	driveId: string | null;
	rootPath: string;
	status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';
	startedAt: string | null;
	completedAt: string | null;
	totalFiles: number;
	processedFiles: number;
	verifiedFiles: number;
	unknownFiles: number;
	errorFiles: number;
	currentFile: string | null;
	progress: number;
}

export interface RomStats {
	totalRoms: number;
	onlineRoms: number;
	verifiedRoms: number;
	totalSize: number;
}

export interface PagedResult<T> {
	items: T[];
	totalCount: number;
	page: number;
	pageSize: number;
	totalPages: number;
	hasNext: boolean;
	hasPrevious: boolean;
}

// Drives API
export const drivesApi = {
	getAll: () => fetchApi<Drive[]>('/drives'),
	get: (id: string) => fetchApi<Drive>(`/drives/${id}`),
	create: (data: { label: string; path: string }) =>
		fetchApi<Drive>('/drives', { method: 'POST', body: JSON.stringify(data) }),
	update: (id: string, data: { label?: string; isEnabled?: boolean }) =>
		fetchApi<void>(`/drives/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
	refresh: (id: string) => fetchApi<Drive>(`/drives/${id}/refresh`, { method: 'POST' }),
	delete: (id: string) => fetchApi<void>(`/drives/${id}`, { method: 'DELETE' }),
};

// DATs API
export const datsApi = {
	getAll: (params?: { provider?: string; enabled?: boolean }) => {
		const query = new URLSearchParams();
		if (params?.provider) query.set('provider', params.provider);
		if (params?.enabled !== undefined) query.set('enabled', String(params.enabled));
		return fetchApi<DatFile[]>(`/dats?${query}`);
	},
	get: (id: string) => fetchApi<DatFile>(`/dats/${id}`),
	getGames: (id: string, params?: { search?: string; page?: number; pageSize?: number }) => {
		const query = new URLSearchParams();
		if (params?.search) query.set('search', params.search);
		if (params?.page) query.set('page', String(params.page));
		if (params?.pageSize) query.set('pageSize', String(params.pageSize));
		return fetchApi<PagedResult<unknown>>(`/dats/${id}/games?${query}`);
	},
	toggle: (id: string) => fetchApi<void>(`/dats/${id}/toggle`, { method: 'POST' }),
	delete: (id: string) => fetchApi<void>(`/dats/${id}`, { method: 'DELETE' }),
	getProviders: () => fetchApi<{ provider: string; datCount: number; gameCount: number; romCount: number }[]>('/dats/providers'),
};

// ROMs API
export const romsApi = {
	getAll: (params?: { driveId?: string; search?: string; online?: boolean; verified?: boolean; page?: number; pageSize?: number }) => {
		const query = new URLSearchParams();
		if (params?.driveId) query.set('driveId', params.driveId);
		if (params?.search) query.set('search', params.search);
		if (params?.online !== undefined) query.set('online', String(params.online));
		if (params?.verified !== undefined) query.set('verified', String(params.verified));
		if (params?.page) query.set('page', String(params.page));
		if (params?.pageSize) query.set('pageSize', String(params.pageSize));
		return fetchApi<PagedResult<RomFile>>(`/roms?${query}`);
	},
	get: (id: string) => fetchApi<RomFile>(`/roms/${id}`),
	getStats: () => fetchApi<RomStats>('/roms/stats'),
	getByHash: (params: { sha1?: string; md5?: string; crc32?: string }) => {
		const query = new URLSearchParams();
		if (params.sha1) query.set('sha1', params.sha1);
		if (params.md5) query.set('md5', params.md5);
		if (params.crc32) query.set('crc32', params.crc32);
		return fetchApi<RomFile[]>(`/roms/by-hash?${query}`);
	},
	delete: (id: string) => fetchApi<void>(`/roms/${id}`, { method: 'DELETE' }),
};

// Scans API
export const scansApi = {
	getAll: (params?: { status?: string; limit?: number }) => {
		const query = new URLSearchParams();
		if (params?.status) query.set('status', params.status);
		if (params?.limit) query.set('limit', String(params.limit));
		return fetchApi<ScanJob[]>(`/scans?${query}`);
	},
	get: (id: string) => fetchApi<ScanJob>(`/scans/${id}`),
	create: (data: { rootPath: string; driveId?: string; recursive?: boolean; verifyHashes?: boolean }) =>
		fetchApi<ScanJob>('/scans', { method: 'POST', body: JSON.stringify(data) }),
	cancel: (id: string) => fetchApi<void>(`/scans/${id}/cancel`, { method: 'POST' }),
	delete: (id: string) => fetchApi<void>(`/scans/${id}`, { method: 'DELETE' }),
};
