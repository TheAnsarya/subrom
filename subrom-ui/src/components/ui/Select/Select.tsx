import { type SelectHTMLAttributes, forwardRef } from 'react';
import styles from './Select.module.css';

export interface SelectOption {
	value: string;
	label: string;
	disabled?: boolean;
}

export interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'children'> {
	label?: string;
	error?: string;
	hint?: string;
	options: SelectOption[];
	placeholder?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
	{ label, error, hint, options, placeholder, className, id, ...props },
	ref
) {
	const selectId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);

	return (
		<div className={styles.wrapper}>
			{label && (
				<label htmlFor={selectId} className={styles.label}>
					{label}
				</label>
			)}
			<div className={styles.selectContainer}>
				<select
					ref={ref}
					id={selectId}
					className={`${styles.select} ${error ? styles.error : ''} ${className || ''}`}
					aria-invalid={error ? 'true' : undefined}
					aria-describedby={error ? `${selectId}-error` : hint ? `${selectId}-hint` : undefined}
					{...props}
				>
					{placeholder && (
						<option value="" disabled>
							{placeholder}
						</option>
					)}
					{options.map((option) => (
						<option key={option.value} value={option.value} disabled={option.disabled}>
							{option.label}
						</option>
					))}
				</select>
				<span className={styles.chevron} aria-hidden="true">
					▼
				</span>
			</div>
			{error && (
				<span id={`${selectId}-error`} className={styles.errorText} role="alert">
					{error}
				</span>
			)}
			{hint && !error && (
				<span id={`${selectId}-hint`} className={styles.hint}>
					{hint}
				</span>
			)}
		</div>
	);
});
