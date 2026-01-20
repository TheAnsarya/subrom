import { type ChangeEvent, type DragEvent, useRef, useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCloudUploadAlt, faFile, faTimes } from '@fortawesome/free-solid-svg-icons';
import styles from './FileUpload.module.css';

export interface FileUploadProps {
	onFilesSelected: (files: File[]) => void;
	accept?: string;
	multiple?: boolean;
	maxSize?: number; // in bytes
	disabled?: boolean;
	label?: string;
	hint?: string;
}

export function FileUpload({
	onFilesSelected,
	accept,
	multiple = false,
	maxSize,
	disabled = false,
	label = 'Drop files here or click to browse',
	hint,
}: FileUploadProps) {
	const [isDragging, setIsDragging] = useState(false);
	const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
	const [error, setError] = useState<string | null>(null);
	const inputRef = useRef<HTMLInputElement>(null);

	const validateFiles = (files: File[]): { valid: File[]; errors: string[] } => {
		const valid: File[] = [];
		const errors: string[] = [];

		for (const file of files) {
			if (maxSize && file.size > maxSize) {
				errors.push(`${file.name} exceeds maximum size of ${formatSize(maxSize)}`);
				continue;
			}

			if (accept) {
				const acceptedTypes = accept.split(',').map((t) => t.trim());
				const fileType = file.type;
				const fileExtension = `.${file.name.split('.').pop()?.toLowerCase()}`;

				const isValid = acceptedTypes.some((accepted) => {
					if (accepted.startsWith('.')) {
						return fileExtension === accepted.toLowerCase();
					}
					if (accepted.endsWith('/*')) {
						return fileType.startsWith(accepted.replace('/*', '/'));
					}
					return fileType === accepted;
				});

				if (!isValid) {
					errors.push(`${file.name} is not an accepted file type`);
					continue;
				}
			}

			valid.push(file);
		}

		return { valid, errors };
	};

	const handleFiles = (files: FileList | null) => {
		if (!files || files.length === 0) return;

		const fileArray = Array.from(files);
		const { valid, errors } = validateFiles(fileArray);

		if (errors.length > 0) {
			setError(errors.join('; '));
		} else {
			setError(null);
		}

		if (valid.length > 0) {
			const newFiles = multiple ? [...selectedFiles, ...valid] : valid;
			setSelectedFiles(newFiles);
			onFilesSelected(newFiles);
		}
	};

	const handleDragOver = (e: DragEvent) => {
		e.preventDefault();
		if (!disabled) {
			setIsDragging(true);
		}
	};

	const handleDragLeave = (e: DragEvent) => {
		e.preventDefault();
		setIsDragging(false);
	};

	const handleDrop = (e: DragEvent) => {
		e.preventDefault();
		setIsDragging(false);

		if (!disabled) {
			handleFiles(e.dataTransfer.files);
		}
	};

	const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
		handleFiles(e.target.files);
		// Reset input so same file can be selected again
		if (inputRef.current) {
			inputRef.current.value = '';
		}
	};

	const handleRemoveFile = (index: number) => {
		const newFiles = selectedFiles.filter((_, i) => i !== index);
		setSelectedFiles(newFiles);
		onFilesSelected(newFiles);
	};

	const handleClick = () => {
		if (!disabled) {
			inputRef.current?.click();
		}
	};

	return (
		<div className={styles.wrapper}>
			<div
				className={`${styles.dropzone} ${isDragging ? styles.dragging : ''} ${disabled ? styles.disabled : ''}`}
				onDragOver={handleDragOver}
				onDragLeave={handleDragLeave}
				onDrop={handleDrop}
				onClick={handleClick}
				role="button"
				tabIndex={disabled ? -1 : 0}
				onKeyDown={(e) => e.key === 'Enter' && handleClick()}
				aria-label={label}
			>
				<FontAwesomeIcon icon={faCloudUploadAlt} className={styles.icon} />
				<span className={styles.label}>{label}</span>
				{hint && <span className={styles.hint}>{hint}</span>}
				<input
					ref={inputRef}
					type="file"
					accept={accept}
					multiple={multiple}
					onChange={handleChange}
					disabled={disabled}
					className={styles.input}
				/>
			</div>

			{error && (
				<span className={styles.error} role="alert">
					{error}
				</span>
			)}

			{selectedFiles.length > 0 && (
				<ul className={styles.fileList}>
					{selectedFiles.map((file, index) => (
						<li key={`${file.name}-${index}`} className={styles.fileItem}>
							<FontAwesomeIcon icon={faFile} className={styles.fileIcon} />
							<span className={styles.fileName}>{file.name}</span>
							<span className={styles.fileSize}>{formatSize(file.size)}</span>
							<button
								type="button"
								className={styles.removeButton}
								onClick={() => handleRemoveFile(index)}
								aria-label={`Remove ${file.name}`}
							>
								<FontAwesomeIcon icon={faTimes} />
							</button>
						</li>
					))}
				</ul>
			)}
		</div>
	);
}

function formatSize(bytes: number): string {
	if (bytes === 0) return '0 B';
	const k = 1024;
	const sizes = ['B', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}
