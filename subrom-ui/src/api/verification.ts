import { api } from './client';
import type { VerificationSummary, RomFile, Game, PagedResult } from '@/types/api';

export const verificationApi = {
	getSummary(): Promise<VerificationSummary> {
		return api.get<VerificationSummary>('/verification/summary');
	},

	verifyRom(id: number): Promise<{ verified: boolean; matchedGame?: string }> {
		return api.get(`/verification/rom/${id}`);
	},

	getMatches(gameId: number): Promise<RomFile[]> {
		return api.get<RomFile[]>(`/verification/game/${gameId}/matches`);
	},

	getUnknownRoms(page = 1, pageSize = 50): Promise<PagedResult<RomFile>> {
		return api.get<PagedResult<RomFile>>(
			`/verification/unknown?page=${page}&pageSize=${pageSize}`
		);
	},

	getMissingGames(page = 1, pageSize = 50): Promise<PagedResult<Game>> {
		return api.get<PagedResult<Game>>(
			`/verification/missing?page=${page}&pageSize=${pageSize}`
		);
	},
};
