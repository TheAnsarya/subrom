import { create } from 'zustand';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
	id: string;
	type: ToastType;
	title: string;
	message?: string;
	duration?: number;
}

interface ToastState {
	toasts: Toast[];
	addToast: (toast: Omit<Toast, 'id'>) => void;
	removeToast: (id: string) => void;
	clearToasts: () => void;
}

export const useToastStore = create<ToastState>((set) => ({
	toasts: [],
	addToast: (toast) => {
		const id = crypto.randomUUID();
		const newToast: Toast = { ...toast, id };

		set((state) => ({
			toasts: [...state.toasts, newToast],
		}));

		// Auto-remove after duration (default 5s)
		const duration = toast.duration ?? 5000;
		if (duration > 0) {
			setTimeout(() => {
				set((state) => ({
					toasts: state.toasts.filter((t) => t.id !== id),
				}));
			}, duration);
		}
	},
	removeToast: (id) =>
		set((state) => ({
			toasts: state.toasts.filter((t) => t.id !== id),
		})),
	clearToasts: () => set({ toasts: [] }),
}));

// Helper functions for easier usage
export const toast = {
	success: (title: string, message?: string) =>
		useToastStore.getState().addToast({ type: 'success', title, message }),
	error: (title: string, message?: string) =>
		useToastStore.getState().addToast({ type: 'error', title, message }),
	warning: (title: string, message?: string) =>
		useToastStore.getState().addToast({ type: 'warning', title, message }),
	info: (title: string, message?: string) =>
		useToastStore.getState().addToast({ type: 'info', title, message }),
};
