export interface DatFile {
	id: number;
	name: string;
	description?: string;
	version?: string;
	author?: string;
	gameCount: number;
	romCount: number;
	importedAt: string;
}

export interface RomFile {
	id: number;
	path: string;
	name: string;
	size: number;
	crc?: string;
	md5?: string;
	sha1?: string;
	scannedAt: string;
	status: 'unknown' | 'verified' | 'missing' | 'mismatch';
}

export interface VerificationSummary {
	totalRoms: number;
	verifiedRoms: number;
	unknownRoms: number;
	missingGames: number;
}

export interface Game {
	id: number;
	datId: number;
	name: string;
	description?: string;
	romCount: number;
}

export interface ScanProgress {
	status: 'idle' | 'scanning' | 'completed' | 'error';
	currentFile?: string;
	filesScanned: number;
	totalFiles: number;
	errorMessage?: string;
}

export interface PagedResult<T> {
	items: T[];
	totalCount: number;
	page: number;
	pageSize: number;
}
