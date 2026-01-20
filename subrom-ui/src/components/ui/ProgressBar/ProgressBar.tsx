import styles from './ProgressBar.module.css';

export interface ProgressBarProps {
	value: number;
	max?: number;
	label?: string;
	showValue?: boolean;
	size?: 'small' | 'medium' | 'large';
	variant?: 'primary' | 'success' | 'warning' | 'danger';
	animated?: boolean;
}

export function ProgressBar({
	value,
	max = 100,
	label,
	showValue = false,
	size = 'medium',
	variant = 'primary',
	animated = false,
}: ProgressBarProps) {
	const percentage = Math.min(100, Math.max(0, (value / max) * 100));

	return (
		<div className={styles.wrapper}>
			{(label || showValue) && (
				<div className={styles.header}>
					{label && <span className={styles.label}>{label}</span>}
					{showValue && (
						<span className={styles.value}>
							{value.toLocaleString()} / {max.toLocaleString()}
						</span>
					)}
				</div>
			)}
			<div
				className={`${styles.track} ${styles[size]}`}
				role="progressbar"
				aria-valuenow={value}
				aria-valuemin={0}
				aria-valuemax={max}
				aria-label={label}
			>
				<div
					className={`${styles.fill} ${styles[variant]} ${animated ? styles.animated : ''}`}
					style={{ width: `${percentage}%` }}
				/>
			</div>
		</div>
	);
}
