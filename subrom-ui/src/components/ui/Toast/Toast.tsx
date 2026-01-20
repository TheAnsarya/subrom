import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
	faCheckCircle,
	faExclamationCircle,
	faExclamationTriangle,
	faInfoCircle,
	faTimes,
} from '@fortawesome/free-solid-svg-icons';
import { useToastStore } from '@/stores/toastStore';
import styles from './Toast.module.css';

const iconMap = {
	success: faCheckCircle,
	error: faExclamationCircle,
	warning: faExclamationTriangle,
	info: faInfoCircle,
};

export function ToastContainer() {
	const { toasts, removeToast } = useToastStore();

	if (toasts.length === 0) return null;

	return (
		<div className={styles.container} role="region" aria-label="Notifications">
			{toasts.map((toast) => (
				<div
					key={toast.id}
					className={`${styles.toast} ${styles[toast.type]}`}
					role="alert"
				>
					<span className={styles.icon}>
						<FontAwesomeIcon icon={iconMap[toast.type]} />
					</span>
					<div className={styles.content}>
						<span className={styles.title}>{toast.title}</span>
						{toast.message && <span className={styles.message}>{toast.message}</span>}
					</div>
					<button
						type="button"
						className={styles.closeButton}
						onClick={() => removeToast(toast.id)}
						aria-label="Dismiss notification"
					>
						<FontAwesomeIcon icon={faTimes} />
					</button>
				</div>
			))}
		</div>
	);
}
