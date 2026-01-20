import { useState, useEffect, useCallback, useRef } from 'react';

export interface UseApiOptions<T> {
	onSuccess?: (data: T) => void;
	onError?: (error: Error) => void;
	enabled?: boolean;
}

export interface UseApiResult<T> {
	data: T | null;
	loading: boolean;
	error: Error | null;
	refetch: () => Promise<void>;
}

/**
 * Hook for fetching data from an API endpoint
 */
export function useApi<T>(
	fetcher: () => Promise<T>,
	deps: unknown[] = [],
	options: UseApiOptions<T> = {}
): UseApiResult<T> {
	const { onSuccess, onError, enabled = true } = options;

	const [data, setData] = useState<T | null>(null);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<Error | null>(null);

	const fetcherRef = useRef(fetcher);
	fetcherRef.current = fetcher;

	const fetch = useCallback(async () => {
		if (!enabled) return;

		setLoading(true);
		setError(null);

		try {
			const result = await fetcherRef.current();
			setData(result);
			onSuccess?.(result);
		} catch (err) {
			const error = err instanceof Error ? err : new Error(String(err));
			setError(error);
			onError?.(error);
		} finally {
			setLoading(false);
		}
	}, [enabled, onSuccess, onError]);

	useEffect(() => {
		fetch();
	}, [fetch, ...deps]);

	return { data, loading, error, refetch: fetch };
}

export interface UseMutationOptions<TData, TVariables> {
	onSuccess?: (data: TData, variables: TVariables) => void;
	onError?: (error: Error, variables: TVariables) => void;
	onSettled?: (data: TData | null, error: Error | null, variables: TVariables) => void;
}

export interface UseMutationResult<TData, TVariables> {
	data: TData | null;
	loading: boolean;
	error: Error | null;
	mutate: (variables: TVariables) => Promise<TData | null>;
	reset: () => void;
}

/**
 * Hook for mutations (POST, PUT, DELETE operations)
 */
export function useMutation<TData, TVariables>(
	mutationFn: (variables: TVariables) => Promise<TData>,
	options: UseMutationOptions<TData, TVariables> = {}
): UseMutationResult<TData, TVariables> {
	const { onSuccess, onError, onSettled } = options;

	const [data, setData] = useState<TData | null>(null);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<Error | null>(null);

	const mutationFnRef = useRef(mutationFn);
	mutationFnRef.current = mutationFn;

	const mutate = useCallback(
		async (variables: TVariables): Promise<TData | null> => {
			setLoading(true);
			setError(null);

			try {
				const result = await mutationFnRef.current(variables);
				setData(result);
				onSuccess?.(result, variables);
				onSettled?.(result, null, variables);
				return result;
			} catch (err) {
				const error = err instanceof Error ? err : new Error(String(err));
				setError(error);
				onError?.(error, variables);
				onSettled?.(null, error, variables);
				return null;
			} finally {
				setLoading(false);
			}
		},
		[onSuccess, onError, onSettled]
	);

	const reset = useCallback(() => {
		setData(null);
		setError(null);
		setLoading(false);
	}, []);

	return { data, loading, error, mutate, reset };
}

/**
 * Hook for polling data at regular intervals
 */
export function usePolling<T>(
	fetcher: () => Promise<T>,
	intervalMs: number,
	options: UseApiOptions<T> & { enabled?: boolean } = {}
): UseApiResult<T> {
	const { enabled = true } = options;
	const result = useApi(fetcher, [], { ...options, enabled: false });
	const intervalRef = useRef<number | null>(null);

	useEffect(() => {
		if (!enabled) {
			if (intervalRef.current) {
				clearInterval(intervalRef.current);
				intervalRef.current = null;
			}
			return;
		}

		// Initial fetch
		result.refetch();

		// Set up polling
		intervalRef.current = window.setInterval(() => {
			result.refetch();
		}, intervalMs);

		return () => {
			if (intervalRef.current) {
				clearInterval(intervalRef.current);
				intervalRef.current = null;
			}
		};
	}, [enabled, intervalMs]);

	return result;
}
