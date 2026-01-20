const API_BASE = '/api';

export class ApiError extends Error {
	constructor(
		public status: number,
		message: string
	) {
		super(message);
		this.name = 'ApiError';
	}
}

async function handleResponse<T>(response: Response): Promise<T> {
	if (!response.ok) {
		const message = await response.text().catch(() => 'Unknown error');
		throw new ApiError(response.status, message);
	}

	if (response.status === 204) {
		return undefined as T;
	}

	return response.json();
}

export const api = {
	async get<T>(path: string): Promise<T> {
		const response = await fetch(`${API_BASE}${path}`);
		return handleResponse<T>(response);
	},

	async post<T>(path: string, body?: unknown): Promise<T> {
		const response = await fetch(`${API_BASE}${path}`, {
			method: 'POST',
			headers: body ? { 'Content-Type': 'application/json' } : {},
			body: body ? JSON.stringify(body) : undefined,
		});
		return handleResponse<T>(response);
	},

	async postForm<T>(path: string, formData: FormData): Promise<T> {
		const response = await fetch(`${API_BASE}${path}`, {
			method: 'POST',
			body: formData,
		});
		return handleResponse<T>(response);
	},

	async put<T>(path: string, body?: unknown): Promise<T> {
		const response = await fetch(`${API_BASE}${path}`, {
			method: 'PUT',
			headers: body ? { 'Content-Type': 'application/json' } : {},
			body: body ? JSON.stringify(body) : undefined,
		});
		return handleResponse<T>(response);
	},

	async delete<T>(path: string): Promise<T> {
		const response = await fetch(`${API_BASE}${path}`, {
			method: 'DELETE',
		});
		return handleResponse<T>(response);
	},
};
