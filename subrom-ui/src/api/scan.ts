import { api } from './client';

export interface ScanRequest {
	path: string;
	recursive?: boolean;
}

export const scanApi = {
	start(request: ScanRequest): Promise<void> {
		return api.post('/scan/start', request);
	},

	stop(): Promise<void> {
		return api.post('/scan/stop');
	},

	getStatus(): Promise<{ isScanning: boolean }> {
		return api.get('/scan/status');
	},
};
