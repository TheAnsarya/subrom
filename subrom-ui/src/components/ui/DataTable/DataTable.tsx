import { useState, useMemo, type ReactNode } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSort, faSortUp, faSortDown, faChevronLeft, faChevronRight } from '@fortawesome/free-solid-svg-icons';
import styles from './DataTable.module.css';

export interface Column<T> {
	key: keyof T | string;
	header: string;
	sortable?: boolean;
	width?: string;
	render?: (item: T, index: number) => ReactNode;
}

export interface DataTableProps<T> {
	data: T[];
	columns: Column<T>[];
	keyField: keyof T;
	loading?: boolean;
	emptyMessage?: string;
	pagination?: {
		page: number;
		pageSize: number;
		totalCount: number;
		onPageChange: (page: number) => void;
	};
	onSort?: (key: string, direction: 'asc' | 'desc') => void;
	sortKey?: string;
	sortDirection?: 'asc' | 'desc';
	onRowClick?: (item: T) => void;
	selectedRows?: Set<T[keyof T]>;
	onSelectionChange?: (selection: Set<T[keyof T]>) => void;
}

export function DataTable<T extends Record<string, unknown>>({
	data,
	columns,
	keyField,
	loading = false,
	emptyMessage = 'No data available',
	pagination,
	onSort,
	sortKey,
	sortDirection,
	onRowClick,
	selectedRows,
	onSelectionChange,
}: DataTableProps<T>) {
	const [localSortKey, setLocalSortKey] = useState<string | null>(null);
	const [localSortDirection, setLocalSortDirection] = useState<'asc' | 'desc'>('asc');

	const effectiveSortKey = sortKey ?? localSortKey;
	const effectiveSortDirection = sortDirection ?? localSortDirection;

	const handleSort = (key: string) => {
		const newDirection =
			effectiveSortKey === key && effectiveSortDirection === 'asc' ? 'desc' : 'asc';

		if (onSort) {
			onSort(key, newDirection);
		} else {
			setLocalSortKey(key);
			setLocalSortDirection(newDirection);
		}
	};

	const sortedData = useMemo(() => {
		if (!effectiveSortKey || onSort) return data;

		return [...data].sort((a, b) => {
			const aVal = a[effectiveSortKey as keyof T];
			const bVal = b[effectiveSortKey as keyof T];

			if (aVal === bVal) return 0;
			if (aVal === null || aVal === undefined) return 1;
			if (bVal === null || bVal === undefined) return -1;

			const comparison = aVal < bVal ? -1 : 1;
			return effectiveSortDirection === 'asc' ? comparison : -comparison;
		});
	}, [data, effectiveSortKey, effectiveSortDirection, onSort]);

	const handleRowClick = (item: T) => {
		onRowClick?.(item);
	};

	const handleSelectAll = (checked: boolean) => {
		if (!onSelectionChange) return;

		if (checked) {
			const allKeys = new Set(sortedData.map((item) => item[keyField] as T[keyof T]));
			onSelectionChange(allKeys);
		} else {
			onSelectionChange(new Set());
		}
	};

	const handleSelectRow = (item: T, checked: boolean) => {
		if (!onSelectionChange || !selectedRows) return;

		const newSelection = new Set(selectedRows);
		const key = item[keyField] as T[keyof T];

		if (checked) {
			newSelection.add(key);
		} else {
			newSelection.delete(key);
		}

		onSelectionChange(newSelection);
	};

	const renderSortIcon = (column: Column<T>) => {
		if (!column.sortable) return null;

		const key = String(column.key);
		if (effectiveSortKey !== key) {
			return <FontAwesomeIcon icon={faSort} className={styles.sortIcon} />;
		}

		return (
			<FontAwesomeIcon
				icon={effectiveSortDirection === 'asc' ? faSortUp : faSortDown}
				className={styles.sortIconActive}
			/>
		);
	};

	const totalPages = pagination
		? Math.ceil(pagination.totalCount / pagination.pageSize)
		: 1;

	const getCellValue = (item: T, column: Column<T>): ReactNode => {
		if (column.render) {
			return column.render(item, sortedData.indexOf(item));
		}
		const value = item[column.key as keyof T];
		if (value === null || value === undefined) return '—';
		return String(value);
	};

	return (
		<div className={styles.wrapper}>
			<div className={styles.tableContainer}>
				<table className={styles.table}>
					<thead>
						<tr>
							{onSelectionChange && (
								<th className={styles.checkboxCell}>
									<input
										type="checkbox"
										checked={selectedRows?.size === sortedData.length && sortedData.length > 0}
										onChange={(e) => handleSelectAll(e.target.checked)}
										aria-label="Select all rows"
									/>
								</th>
							)}
							{columns.map((column) => (
								<th
									key={String(column.key)}
									className={column.sortable ? styles.sortableHeader : undefined}
									style={{ width: column.width }}
									onClick={column.sortable ? () => handleSort(String(column.key)) : undefined}
								>
									<span className={styles.headerContent}>
										{column.header}
										{renderSortIcon(column)}
									</span>
								</th>
							))}
						</tr>
					</thead>
					<tbody>
						{loading ? (
							<tr>
								<td colSpan={columns.length + (onSelectionChange ? 1 : 0)} className={styles.loadingCell}>
									<div className={styles.loadingSpinner} />
									<span>Loading...</span>
								</td>
							</tr>
						) : sortedData.length === 0 ? (
							<tr>
								<td colSpan={columns.length + (onSelectionChange ? 1 : 0)} className={styles.emptyCell}>
									{emptyMessage}
								</td>
							</tr>
						) : (
							sortedData.map((item) => {
								const key = item[keyField];
								const isSelected = selectedRows?.has(key as T[keyof T]);

								return (
									<tr
										key={String(key)}
										className={`${onRowClick ? styles.clickableRow : ''} ${isSelected ? styles.selectedRow : ''}`}
										onClick={() => handleRowClick(item)}
									>
										{onSelectionChange && (
											<td
												className={styles.checkboxCell}
												onClick={(e) => e.stopPropagation()}
											>
												<input
													type="checkbox"
													checked={isSelected ?? false}
													onChange={(e) => handleSelectRow(item, e.target.checked)}
													aria-label={`Select row ${String(key)}`}
												/>
											</td>
										)}
										{columns.map((column) => (
											<td key={String(column.key)}>{getCellValue(item, column)}</td>
										))}
									</tr>
								);
							})
						)}
					</tbody>
				</table>
			</div>

			{pagination && totalPages > 1 && (
				<div className={styles.pagination}>
					<span className={styles.paginationInfo}>
						Showing {((pagination.page - 1) * pagination.pageSize) + 1} to{' '}
						{Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of{' '}
						{pagination.totalCount.toLocaleString()}
					</span>
					<div className={styles.paginationControls}>
						<button
							type="button"
							className={styles.paginationButton}
							disabled={pagination.page <= 1}
							onClick={() => pagination.onPageChange(pagination.page - 1)}
							aria-label="Previous page"
						>
							<FontAwesomeIcon icon={faChevronLeft} />
						</button>
						<span className={styles.pageInfo}>
							Page {pagination.page} of {totalPages}
						</span>
						<button
							type="button"
							className={styles.paginationButton}
							disabled={pagination.page >= totalPages}
							onClick={() => pagination.onPageChange(pagination.page + 1)}
							aria-label="Next page"
						>
							<FontAwesomeIcon icon={faChevronRight} />
						</button>
					</div>
				</div>
			)}
		</div>
	);
}
