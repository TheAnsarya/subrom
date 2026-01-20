import { type InputHTMLAttributes, forwardRef } from 'react';
import styles from './Input.module.css';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
	label?: string;
	error?: string;
	hint?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
	{ label, error, hint, className, id, ...props },
	ref
) {
	const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

	return (
		<div className={styles.wrapper}>
			{label && (
				<label htmlFor={inputId} className={styles.label}>
					{label}
				</label>
			)}
			<input
				ref={ref}
				id={inputId}
				className={`${styles.input} ${error ? styles.error : ''} ${className || ''}`}
				aria-invalid={error ? 'true' : undefined}
				aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
				{...props}
			/>
			{error && (
				<span id={`${inputId}-error`} className={styles.errorText} role="alert">
					{error}
				</span>
			)}
			{hint && !error && (
				<span id={`${inputId}-hint`} className={styles.hint}>
					{hint}
				</span>
			)}
		</div>
	);
});
