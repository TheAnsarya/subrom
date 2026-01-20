import { type ReactNode, useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTimes } from '@fortawesome/free-solid-svg-icons';
import styles from './Modal.module.css';

export interface ModalProps {
	isOpen: boolean;
	onClose: () => void;
	title?: string;
	children: ReactNode;
	size?: 'small' | 'medium' | 'large' | 'fullscreen';
	closeOnOverlay?: boolean;
	closeOnEscape?: boolean;
	footer?: ReactNode;
}

export function Modal({
	isOpen,
	onClose,
	title,
	children,
	size = 'medium',
	closeOnOverlay = true,
	closeOnEscape = true,
	footer,
}: ModalProps) {
	const dialogRef = useRef<HTMLDivElement>(null);

	useEffect(() => {
		if (!isOpen) return;

		const handleEscape = (event: KeyboardEvent) => {
			if (event.key === 'Escape' && closeOnEscape) {
				onClose();
			}
		};

		document.addEventListener('keydown', handleEscape);
		document.body.style.overflow = 'hidden';

		// Focus trap
		dialogRef.current?.focus();

		return () => {
			document.removeEventListener('keydown', handleEscape);
			document.body.style.overflow = '';
		};
	}, [isOpen, onClose, closeOnEscape]);

	if (!isOpen) return null;

	const handleOverlayClick = (event: React.MouseEvent) => {
		if (event.target === event.currentTarget && closeOnOverlay) {
			onClose();
		}
	};

	return createPortal(
		<div className={styles.overlay} onClick={handleOverlayClick}>
			<div
				ref={dialogRef}
				className={`${styles.modal} ${styles[size]}`}
				role="dialog"
				aria-modal="true"
				aria-labelledby={title ? 'modal-title' : undefined}
				tabIndex={-1}
			>
				{title && (
					<div className={styles.header}>
						<h2 id="modal-title" className={styles.title}>
							{title}
						</h2>
						<button
							type="button"
							className={styles.closeButton}
							onClick={onClose}
							aria-label="Close modal"
						>
							<FontAwesomeIcon icon={faTimes} />
						</button>
					</div>
				)}
				<div className={styles.content}>{children}</div>
				{footer && <div className={styles.footer}>{footer}</div>}
			</div>
		</div>,
		document.body
	);
}
