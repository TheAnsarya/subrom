import { useRef, useCallback, useMemo, type ReactNode, type CSSProperties } from 'react';
import { FixedSizeList as List, type ListChildComponentProps } from 'react-window';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSort, faSortUp, faSortDown, faSpinner } from '@fortawesome/free-solid-svg-icons';
import styles from './VirtualTable.module.css';

// ============================================================================
// Types
// ============================================================================

export interface VirtualColumn<T> {
	/** Unique key for the column */
	key: keyof T | string;
	/** Display header text */
	header: string;
	/** Whether column is sortable */
	sortable?: boolean;
	/** Fixed width in pixels or CSS value */
	width?: number | string;
	/** Minimum width in pixels */
	minWidth?: number;
	/** Flex grow factor (default 1 if no width specified) */
	flex?: number;
	/** Custom cell renderer */
	render?: (item: T, index: number) => ReactNode;
	/** Cell alignment */
	align?: 'left' | 'center' | 'right';
}

export interface VirtualTableProps<T> {
	/** Data items to display */
	data: T[];
	/** Column definitions */
	columns: VirtualColumn<T>[];
	/** Unique key field for each row */
	keyField: keyof T;
	/** Height of each row in pixels */
	rowHeight?: number;
	/** Height of the table container */
	height: number;
	/** Width of the table container (default: 100%) */
	width?: number | string;
	/** Loading state */
	loading?: boolean;
	/** Message when no data */
	emptyMessage?: string;
	/** Called when scrolling near bottom (for infinite scroll) */
	onLoadMore?: () => void;
	/** Threshold in pixels from bottom to trigger onLoadMore */
	loadMoreThreshold?: number;
	/** Whether more data is available */
	hasMore?: boolean;
	/** Loading more indicator */
	loadingMore?: boolean;
	/** Sort key */
	sortKey?: string;
	/** Sort direction */
	sortDirection?: 'asc' | 'desc';
	/** Sort change handler */
	onSort?: (key: string, direction: 'asc' | 'desc') => void;
	/** Row click handler */
	onRowClick?: (item: T, index: number) => void;
	/** Row double-click handler */
	onRowDoubleClick?: (item: T, index: number) => void;
	/** Selected row keys */
	selectedKeys?: Set<T[keyof T]>;
	/** Selection change handler */
	onSelectionChange?: (keys: Set<T[keyof T]>) => void;
	/** Whether to allow multi-select */
	multiSelect?: boolean;
	/** Custom row class name */
	rowClassName?: (item: T, index: number) => string | undefined;
	/** Overscan count for rendering extra rows */
	overscanCount?: number;
}

// ============================================================================
// Row Data Interface
// ============================================================================

interface RowData<T> {
	items: T[];
	columns: VirtualColumn<T>[];
	keyField: keyof T;
	selectedKeys?: Set<T[keyof T]>;
	onRowClick?: (item: T, index: number) => void;
	onRowDoubleClick?: (item: T, index: number) => void;
	rowClassName?: (item: T, index: number) => string | undefined;
	columnWidths: number[];
}

// ============================================================================
// Row Component
// ============================================================================

function Row<T>({ index, style, data }: ListChildComponentProps<RowData<T>>) {
	const { items, columns, keyField, selectedKeys, onRowClick, onRowDoubleClick, rowClassName, columnWidths } = data;
	const item = items[index];

	if (!item) {
		return (
			<div style={style} className={styles.row}>
				<div className={styles.loadingRow}>
					<FontAwesomeIcon icon={faSpinner} spin />
				</div>
			</div>
		);
	}

	const key = item[keyField];
	const isSelected = selectedKeys?.has(key as T[keyof T]);
	const customClass = rowClassName?.(item, index);

	const handleClick = () => onRowClick?.(item, index);
	const handleDoubleClick = () => onRowDoubleClick?.(item, index);

	return (
		<div
			style={style}
			className={`${styles.row} ${isSelected ? styles.selected : ''} ${onRowClick ? styles.clickable : ''} ${customClass || ''}`}
			onClick={handleClick}
			onDoubleClick={handleDoubleClick}
			role="row"
			aria-selected={isSelected}
		>
			{columns.map((column, colIndex) => {
				const cellValue = getCellValue(item, column, index);
				const width = columnWidths[colIndex];

				return (
					<div
						key={String(column.key)}
						className={styles.cell}
						style={{
							width,
							minWidth: column.minWidth,
							textAlign: column.align || 'left',
						}}
						role="cell"
					>
						{cellValue}
					</div>
				);
			})}
		</div>
	);
}

// ============================================================================
// Helper Functions
// ============================================================================

function getCellValue<T>(item: T, column: VirtualColumn<T>, index: number): ReactNode {
	if (column.render) {
		return column.render(item, index);
	}

	const value = (item as Record<string, unknown>)[column.key as string];

	if (value === null || value === undefined) {
		return '—';
	}

	return String(value);
}

function calculateColumnWidths<T>(
	columns: VirtualColumn<T>[],
	containerWidth: number
): number[] {
	const fixedWidths: (number | null)[] = columns.map(col => {
		if (typeof col.width === 'number') return col.width;
		if (typeof col.width === 'string' && col.width.endsWith('px')) {
			return parseInt(col.width, 10);
		}
		return null;
	});

	const totalFixed = fixedWidths.reduce<number>((sum, w) => sum + (w || 0), 0);
	const flexColumns = columns.filter((_, i) => fixedWidths[i] === null);
	const totalFlex = flexColumns.reduce((sum, col) => sum + (col.flex ?? 1), 0);
	const availableWidth = Math.max(0, containerWidth - totalFixed);

	return columns.map((col, i) => {
		if (fixedWidths[i] !== null) return fixedWidths[i]!;
		const flex = col.flex ?? 1;
		const width = Math.floor((flex / totalFlex) * availableWidth);
		return Math.max(width, col.minWidth || 50);
	});
}

// ============================================================================
// Main Component
// ============================================================================

export function VirtualTable<T extends Record<string, unknown>>({
	data,
	columns,
	keyField,
	rowHeight = 40,
	height,
	width = '100%',
	loading = false,
	emptyMessage = 'No data available',
	onLoadMore,
	loadMoreThreshold = 200,
	hasMore = false,
	loadingMore = false,
	sortKey,
	sortDirection,
	onSort,
	onRowClick,
	onRowDoubleClick,
	selectedKeys,
	onSelectionChange,
	multiSelect = false,
	rowClassName,
	overscanCount = 5,
}: VirtualTableProps<T>) {
	const listRef = useRef<List>(null);
	const containerRef = useRef<HTMLDivElement>(null);

	// Calculate container width for column sizing
	const containerWidth = useMemo(() => {
		if (typeof width === 'number') return width;
		// Default to a reasonable width, will be updated on resize
		return 800;
	}, [width]);

	const columnWidths = useMemo(
		() => calculateColumnWidths(columns, containerWidth),
		[columns, containerWidth]
	);

	// Handle scroll for infinite loading
	const handleScroll = useCallback(
		({ scrollOffset, scrollUpdateWasRequested }: { scrollOffset: number; scrollUpdateWasRequested: boolean }) => {
			if (scrollUpdateWasRequested || !onLoadMore || !hasMore || loadingMore) return;

			const totalHeight = data.length * rowHeight;
			const scrollBottom = totalHeight - scrollOffset - height;

			if (scrollBottom < loadMoreThreshold) {
				onLoadMore();
			}
		},
		[data.length, rowHeight, height, loadMoreThreshold, onLoadMore, hasMore, loadingMore]
	);

	// Handle sort click
	const handleSort = useCallback(
		(key: string) => {
			if (!onSort) return;
			const newDirection = sortKey === key && sortDirection === 'asc' ? 'desc' : 'asc';
			onSort(key, newDirection);
		},
		[sortKey, sortDirection, onSort]
	);

	// Handle row selection
	const handleRowClick = useCallback(
		(item: T, index: number) => {
			if (onSelectionChange) {
				const key = item[keyField] as T[keyof T];
				const newSelection = new Set(multiSelect ? selectedKeys : undefined);

				if (selectedKeys?.has(key)) {
					newSelection.delete(key);
				} else {
					newSelection.add(key);
				}

				onSelectionChange(newSelection);
			}

			onRowClick?.(item, index);
		},
		[keyField, multiSelect, selectedKeys, onSelectionChange, onRowClick]
	);

	// Render sort icon
	const renderSortIcon = (column: VirtualColumn<T>) => {
		if (!column.sortable) return null;

		const key = String(column.key);
		if (sortKey !== key) {
			return <FontAwesomeIcon icon={faSort} className={styles.sortIcon} />;
		}

		return (
			<FontAwesomeIcon
				icon={sortDirection === 'asc' ? faSortUp : faSortDown}
				className={styles.sortIconActive}
			/>
		);
	};

	// Row data for virtualization
	const rowData: RowData<T> = useMemo(
		() => ({
			items: data,
			columns,
			keyField,
			selectedKeys,
			onRowClick: onSelectionChange ? handleRowClick : onRowClick,
			onRowDoubleClick,
			rowClassName,
			columnWidths,
		}),
		[data, columns, keyField, selectedKeys, handleRowClick, onRowClick, onRowDoubleClick, rowClassName, columnWidths, onSelectionChange]
	);

	// Calculate total items including loading row
	const itemCount = hasMore ? data.length + 1 : data.length;

	// Container style
	const containerStyle: CSSProperties = {
		width,
		height,
	};

	if (loading) {
		return (
			<div className={styles.container} style={containerStyle}>
				<div className={styles.loading}>
					<FontAwesomeIcon icon={faSpinner} spin size="2x" />
					<span>Loading...</span>
				</div>
			</div>
		);
	}

	if (data.length === 0 && !hasMore) {
		return (
			<div className={styles.container} style={containerStyle}>
				<div className={styles.header} role="row">
					{columns.map((column, i) => (
						<div
							key={String(column.key)}
							className={styles.headerCell}
							style={{ width: columnWidths[i], minWidth: column.minWidth }}
							role="columnheader"
						>
							{column.header}
						</div>
					))}
				</div>
				<div className={styles.empty}>{emptyMessage}</div>
			</div>
		);
	}

	return (
		<div className={styles.container} style={containerStyle} ref={containerRef} role="table">
			{/* Header */}
			<div className={styles.header} role="row">
				{columns.map((column, i) => (
					<div
						key={String(column.key)}
						className={`${styles.headerCell} ${column.sortable ? styles.sortable : ''}`}
						style={{
							width: columnWidths[i],
							minWidth: column.minWidth,
							textAlign: column.align || 'left',
						}}
						onClick={column.sortable ? () => handleSort(String(column.key)) : undefined}
						role="columnheader"
						aria-sort={sortKey === String(column.key) ? (sortDirection === 'asc' ? 'ascending' : 'descending') : undefined}
					>
						<span className={styles.headerContent}>
							{column.header}
							{renderSortIcon(column)}
						</span>
					</div>
				))}
			</div>

			{/* Virtualized Body */}
			<List
				ref={listRef}
				height={height - rowHeight} // Subtract header height
				width={width}
				itemCount={itemCount}
				itemSize={rowHeight}
				itemData={rowData}
				onScroll={handleScroll}
				overscanCount={overscanCount}
				className={styles.list}
			>
				{Row as React.ComponentType<ListChildComponentProps<RowData<T>>>}
			</List>

			{/* Loading More Indicator */}
			{loadingMore && (
				<div className={styles.loadingMore}>
					<FontAwesomeIcon icon={faSpinner} spin />
					<span>Loading more...</span>
				</div>
			)}
		</div>
	);
}


