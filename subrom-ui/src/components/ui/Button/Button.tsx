import { type ButtonHTMLAttributes, type ReactNode } from 'react';
import styles from './Button.module.css';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';
export type ButtonSize = 'small' | 'medium' | 'large';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
	variant?: ButtonVariant;
	size?: ButtonSize;
	loading?: boolean;
	icon?: ReactNode;
	children?: ReactNode;
}

export function Button({
	variant = 'primary',
	size = 'medium',
	loading = false,
	icon,
	children,
	className,
	disabled,
	...props
}: ButtonProps) {
	const classNames = [
		styles.button,
		styles[variant],
		styles[size],
		loading && styles.loading,
		className
	].filter(Boolean).join(' ');

	return (
		<button
			className={classNames}
			disabled={disabled || loading}
			{...props}
		>
			{loading ? (
				<span className={styles.spinner} aria-hidden="true" />
			) : icon ? (
				<span className={styles.icon}>{icon}</span>
			) : null}
			{children && <span className={styles.text}>{children}</span>}
		</button>
	);
}
