// Enhanced Table Components with AlpineJS + TailwindCSS
// This provides DataTables-like functionality with perfect TailwindCSS integration

export function initializeTables() {
    // Enhanced Table Component with sorting, filtering, and pagination
    window.tableData = function(initialData = []) {
        return {
            // Data management
            originalData: initialData,
            filteredData: initialData,
            currentData: [],
            
            // Pagination
            currentPage: 1,
            itemsPerPage: 10,
            itemsPerPageOptions: [5, 10, 25, 50, 100],
            
            // Filtering
            globalFilter: '',
            columnFilters: {},
            
            // Sorting
            sortColumn: '',
            sortDirection: 'asc',
            
            // UI State
            loading: false,
            selectedRows: new Set(),
            
            // Initialize
            init() {
                this.updateFilteredData();
                this.updateCurrentData();
            },
            
            // Data methods
            setData(newData) {
                this.originalData = newData;
                this.filteredData = newData;
                this.selectedRows.clear();
                this.currentPage = 1;
                this.updateCurrentData();
            },
            
            addRow(row) {
                this.originalData.push(row);
                this.updateFilteredData();
                this.updateCurrentData();
            },
            
            removeRow(index) {
                this.originalData.splice(index, 1);
                this.updateFilteredData();
                this.updateCurrentData();
            },
            
            // Filtering
            updateGlobalFilter(value) {
                this.globalFilter = value;
                this.currentPage = 1;
                this.updateFilteredData();
                this.updateCurrentData();
            },
            
            updateColumnFilter(column, value) {
                if (value) {
                    this.columnFilters[column] = value;
                } else {
                    delete this.columnFilters[column];
                }
                this.currentPage = 1;
                this.updateFilteredData();
                this.updateCurrentData();
            },
            
            updateFilteredData() {
                let data = [...this.originalData];
                
                // Global filter
                if (this.globalFilter) {
                    const filter = this.globalFilter.toLowerCase();
                    data = data.filter(row => 
                        Object.values(row).some(value => 
                            String(value).toLowerCase().includes(filter)
                        )
                    );
                }
                
                // Column filters
                Object.entries(this.columnFilters).forEach(([column, filter]) => {
                    if (filter) {
                        const filterLower = filter.toLowerCase();
                        data = data.filter(row => 
                            String(row[column]).toLowerCase().includes(filterLower)
                        );
                    }
                });
                
                this.filteredData = data;
            },
            
            // Sorting
            sort(column) {
                if (this.sortColumn === column) {
                    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
                } else {
                    this.sortColumn = column;
                    this.sortDirection = 'asc';
                }
                
                this.filteredData.sort((a, b) => {
                    const aVal = a[column];
                    const bVal = b[column];
                    
                    // Handle different data types
                    let comparison = 0;
                    if (typeof aVal === 'number' && typeof bVal === 'number') {
                        comparison = aVal - bVal;
                    } else if (aVal instanceof Date && bVal instanceof Date) {
                        comparison = aVal.getTime() - bVal.getTime();
                    } else {
                        comparison = String(aVal).localeCompare(String(bVal));
                    }
                    
                    return this.sortDirection === 'asc' ? comparison : -comparison;
                });
                
                this.updateCurrentData();
            },
            
            getSortIcon(column) {
                if (this.sortColumn !== column) return '↕️';
                return this.sortDirection === 'asc' ? '↑' : '↓';
            },
            
            // Pagination
            updateCurrentData() {
                const start = (this.currentPage - 1) * this.itemsPerPage;
                const end = start + this.itemsPerPage;
                this.currentData = this.filteredData.slice(start, end);
            },
            
            get totalPages() {
                return Math.ceil(this.filteredData.length / this.itemsPerPage);
            },
            
            get startRecord() {
                return ((this.currentPage - 1) * this.itemsPerPage) + 1;
            },
            
            get endRecord() {
                return Math.min(this.currentPage * this.itemsPerPage, this.filteredData.length);
            },
            
            goToPage(page) {
                if (page >= 1 && page <= this.totalPages) {
                    this.currentPage = page;
                    this.updateCurrentData();
                }
            },
            
            changeItemsPerPage(newSize) {
                this.itemsPerPage = parseInt(newSize);
                this.currentPage = 1;
                this.updateCurrentData();
            },
            
            get paginationRange() {
                const delta = 2;
                const range = [];
                const rangeWithDots = [];
                let l;
                
                for (let i = Math.max(2, this.currentPage - delta);
                     i <= Math.min(this.totalPages - 1, this.currentPage + delta);
                     i++) {
                    range.push(i);
                }
                
                if (this.currentPage - delta > 2) {
                    rangeWithDots.push(1, '...');
                } else {
                    rangeWithDots.push(1);
                }
                
                rangeWithDots.push(...range);
                
                if (this.currentPage + delta < this.totalPages - 1) {
                    rangeWithDots.push('...', this.totalPages);
                } else {
                    rangeWithDots.push(this.totalPages);
                }
                
                return rangeWithDots;
            },
            
            // Row selection
            toggleRowSelection(rowIndex) {
                const globalIndex = ((this.currentPage - 1) * this.itemsPerPage) + rowIndex;
                if (this.selectedRows.has(globalIndex)) {
                    this.selectedRows.delete(globalIndex);
                } else {
                    this.selectedRows.add(globalIndex);
                }
            },
            
            isRowSelected(rowIndex) {
                const globalIndex = ((this.currentPage - 1) * this.itemsPerPage) + rowIndex;
                return this.selectedRows.has(globalIndex);
            },
            
            toggleAllRows() {
                if (this.isAllRowsSelected()) {
                    this.selectedRows.clear();
                } else {
                    this.filteredData.forEach((_, index) => {
                        this.selectedRows.add(index);
                    });
                }
            },
            
            isAllRowsSelected() {
                return this.filteredData.length > 0 && this.selectedRows.size === this.filteredData.length;
            },
            
            getSelectedRows() {
                return Array.from(this.selectedRows).map(index => this.originalData[index]);
            },
            
            // Export functionality
            exportToCsv(filename = 'table-data.csv') {
                const headers = Object.keys(this.originalData[0] || {});
                const csvContent = [
                    headers.join(','),
                    ...this.filteredData.map(row => 
                        headers.map(header => `"${String(row[header]).replace(/"/g, '""')}"`).join(',')
                    )
                ].join('\n');
                
                const blob = new Blob([csvContent], { type: 'text/csv' });
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
            }
        };
    };

    // Database Table Component with server-side data loading and row details
    window.databaseTableData = function(columnsArg = null, tableNameArg = null, filePathArg = null) {
        return {
            // Table configuration
            columns: columnsArg || window.tableColumns || [],
            tableName: tableNameArg || window.tableName || '',
            // retained for backward compatibility; not sent to server anymore
            filePath: filePathArg || window.filePath || null,
            
            // Data management
            currentData: [],
            totalRecords: 0,
            
            // Pagination
            currentPage: 1,
            itemsPerPage: 25,
            itemsPerPageOptions: [10, 25, 50, 100],
            
            // Filtering & Search
            globalFilter: '',
            searchTimeout: null,
            
            // Sorting
            sortColumn: '',
            sortDirection: 'asc',
            
            // UI State
            loading: false,
            error: false,
            errorMessage: '',
            
            // Row Details Modal
            showRowDetails: false,
            selectedRowData: null,
            selectedRowIndex: -1,
            // Abort controller for in-flight fetch
            _abortController: null,
            
            // Initialize
            init() {
                this.loadData();
            },
            
            // Data loading
            async loadData() {
                this.loading = true;
                this.error = false;
                this.errorMessage = '';

                // Basic validation
                if (!this.tableName) {
                    console.warn('databaseTableData: tableName missing');
                    this.error = true;
                    this.errorMessage = 'Table name missing';
                    this.loading = false;
                    return;
                }

                // Abort previous request if any
                if (this._abortController) {
                    this._abortController.abort();
                }
                this._abortController = new AbortController();
                
                try {
                    const formData = new FormData();
                    formData.append('tableName', this.tableName);
                    formData.append('Draw', Math.random()); // Required by DataTables
                    formData.append('Start', (this.currentPage - 1) * this.itemsPerPage);
                    formData.append('Length', this.itemsPerPage);
                    formData.append('SearchValue', this.globalFilter);
                    
                    if (this.sortColumn) {
                        formData.append('Order[0].Column', this.getColumnIndex(this.sortColumn));
                        formData.append('Order[0].Dir', this.sortDirection);
                    }
                    
                    const response = await fetch('/Home/GetTableData', {
                        method: 'POST',
                        body: formData,
                        signal: this._abortController.signal
                    });
                    
                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }
                    
                    const result = await response.json();
                    
                    if (result.error) {
                        throw new Error(result.error);
                    }
                    
                    this.currentData = Array.isArray(result.data) ? result.data : [];
                    // Prefer recordsFiltered; fallback to recordsTotal; else length
                    const filtered = (typeof result.recordsFiltered === 'number') ? result.recordsFiltered : null;
                    const total = (typeof result.recordsTotal === 'number') ? result.recordsTotal : null;
                    this.totalRecords = filtered ?? total ?? this.currentData.length;
                    
                } catch (err) {
                    if (err.name === 'AbortError') {
                        // Fetch was intentionally aborted due to a newer request; treat as benign
                        console.warn('Table data request aborted (superseded by a newer request).');
                    } else {
                        console.error('Error loading table data:', err);
                        this.error = true;
                        this.errorMessage = err.message;
                        this.currentData = [];
                        this.totalRecords = 0;
                    }
                } finally {
                    this.loading = false;
                }
            },
            
            // Helper methods
            getColumnIndex(columnName) {
                return this.columns.findIndex(col => col.name === columnName);
            },
            
            truncateText(text, maxLength) {
                if (!text) return '';
                const str = String(text);
                return str.length > maxLength ? str.substring(0, maxLength) + '...' : str;
            },
            
            // Search with debouncing
            debounceSearch() {
                clearTimeout(this.searchTimeout);
                this.searchTimeout = setTimeout(() => {
                    this.currentPage = 1;
                    this.loadData();
                }, 500);
            },
            
            // Sorting
            sort(column) {
                if (this.sortColumn === column) {
                    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
                } else {
                    this.sortColumn = column;
                    this.sortDirection = 'asc';
                }
                
                this.currentPage = 1;
                this.loadData();
            },
            
            getSortIcon(column) {
                if (this.sortColumn !== column) return '↕️';
                return this.sortDirection === 'asc' ? '↑' : '↓';
            },
            
            // Pagination
            get totalPages() {
                return Math.ceil(this.totalRecords / this.itemsPerPage);
            },
            
            get startRecord() {
                if (this.totalRecords === 0) return 0;
                return ((this.currentPage - 1) * this.itemsPerPage) + 1;
            },
            
            get endRecord() {
                if (this.totalRecords === 0) return 0;
                return Math.min(this.currentPage * this.itemsPerPage, this.totalRecords);
            },
            
            goToPage(page) {
                if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
                    this.currentPage = page;
                    this.loadData();
                }
            },
            
            changeItemsPerPage(newSize) {
                this.itemsPerPage = parseInt(newSize);
                this.currentPage = 1;
                this.loadData();
            },
            
            get paginationRange() {
                const delta = 2;
                const range = [];
                const rangeWithDots = [];
                
                if (this.totalPages <= 1) return [1];
                
                for (let i = Math.max(2, this.currentPage - delta);
                     i <= Math.min(this.totalPages - 1, this.currentPage + delta);
                     i++) {
                    range.push(i);
                }
                
                if (this.currentPage - delta > 2) {
                    rangeWithDots.push(1, '...');
                } else {
                    rangeWithDots.push(1);
                }
                
                rangeWithDots.push(...range);
                
                if (this.currentPage + delta < this.totalPages - 1) {
                    rangeWithDots.push('...', this.totalPages);
                } else if (this.totalPages > 1) {
                    rangeWithDots.push(this.totalPages);
                }
                
                return rangeWithDots;
            },
            
            // Row Details Modal
            viewRowDetails(row, index) {
                this.selectedRowData = row;
                this.selectedRowIndex = index;
                this.showRowDetails = true;
                document.body.style.overflow = 'hidden'; // Prevent background scrolling
            },
            
            closeRowDetails() {
                this.showRowDetails = false;
                this.selectedRowData = null;
                this.selectedRowIndex = -1;
                document.body.style.overflow = ''; // Restore scrolling
            },
            
            previousRow() {
                if (this.selectedRowIndex > 0) {
                    this.selectedRowIndex--;
                    this.selectedRowData = this.currentData[this.selectedRowIndex];
                }
            },
            
            nextRow() {
                if (this.selectedRowIndex < this.currentData.length - 1) {
                    this.selectedRowIndex++;
                    this.selectedRowData = this.currentData[this.selectedRowIndex];
                }
            },
            
            copyRowData() {
                if (!this.selectedRowData) return;
                
                const dataString = this.columns.map(col => 
                    `${col.name}: ${this.selectedRowData[col.name] || 'NULL'}`
                ).join('\n');
                
                navigator.clipboard.writeText(dataString).then(() => {
                    this.showNotification('Row data copied to clipboard!', 'success');
                }).catch(err => {
                    console.error('Failed to copy: ', err);
                    this.showNotification('Failed to copy data', 'error');
                });
            },
            
            // Utility methods
            refreshData() {
                this.loadData();
            },
            
            showNotification(message, type = 'info') {
                const notification = document.createElement('div');
                notification.className = `fixed top-4 right-4 z-50 p-4 rounded-lg shadow-lg transition-all duration-300 transform translate-x-full ${this.getNotificationClasses(type)}`;
                notification.innerHTML = `
                    <div class="flex items-center">
                        <span class="mr-2">${this.getNotificationIcon(type)}</span>
                        <span>${message}</span>
                        <button onclick="this.parentElement.parentElement.remove()" class="ml-4 text-sm opacity-70 hover:opacity-100">×</button>
                    </div>
                `;
                
                document.body.appendChild(notification);
                
                setTimeout(() => notification.classList.remove('translate-x-full'), 100);
                setTimeout(() => {
                    notification.classList.add('translate-x-full');
                    setTimeout(() => notification.remove(), 300);
                }, 3000);
            },
            
            getNotificationClasses(type) {
                switch (type) {
                    case 'success': return 'bg-green-500 text-white';
                    case 'error': return 'bg-red-500 text-white';
                    case 'warning': return 'bg-yellow-500 text-white';
                    default: return 'bg-blue-500 text-white';
                }
            },
            
            getNotificationIcon(type) {
                switch (type) {
                    case 'success': return '✓';
                    case 'error': return '✗';
                    case 'warning': return '⚠';
                    default: return 'ℹ';
                }
            }
        };
    };
}
