# AlpineJS Table Components Integration Guide

## Overview

This document explains how to integrate and use the custom AlpineJS table components in your ASP.NET Core application. These components provide DataTables-like functionality while maintaining perfect integration with your TailwindCSS design system.

## Architecture

### 🏗️ **Component Structure**

- **`src/js/table-components.js`**: Core table functionality (Alpine.js data/methods)
- **`Views/Shared/TableComponent.cshtml`**: Reusable table template
- **Enhanced CSS classes in `main.scss`**: Table-specific styling
- **No jQuery dependency**: Pure AlpineJS implementation

### 🎯 **Key Benefits**

- **Lightweight**: ~49KB total JS bundle (including AlpineJS)
- **Design System Integrated**: Uses your existing TailwindCSS classes
- **No Style Conflicts**: No competing CSS frameworks
- **Modern Architecture**: ES6 modules, no legacy dependencies

## Implementation Guide

### 1. Basic Usage

```html
<div x-data="tableData(yourData)" x-init="init()">
    <!-- Include the table component template -->
    @Html.Partial("TableComponent")
</div>
```

### 2. Data Structure

Your data should be an array of objects:

```javascript
const sampleData = [
    { 
        name: 'John Doe', 
        position: 'Developer', 
        office: 'Remote', 
        age: 30, 
        startDate: '2024-01-15',
        salary: '$75,000' 
    },
    // ... more records
];
```

### 3. Using in Razor Views

```razor
@{
    ViewData["Title"] = "My Data Table";
}

<div class="max-w-7xl mx-auto px-4 py-8">
    <h1 class="text-3xl font-bold mb-8">Employee Data</h1>
    
    <!-- Table Component -->
    <div x-data="tableData(employeeData)" x-init="init()">
        @Html.Partial("TableComponent")
    </div>
</div>

@section Scripts {
    <script>
        // Your data
        const employeeData = @Html.Raw(Json.Serialize(Model.Employees));
    </script>
}
```

### 4. Server-Side Integration

#### Controller Action

```csharp
public IActionResult Employees()
{
    var employees = _employeeService.GetAllEmployees();
    var model = new EmployeeListViewModel
    {
        Employees = employees.Select(e => new
        {
            name = e.FullName,
            position = e.JobTitle,
            office = e.Office.Name,
            age = e.Age,
            startDate = e.StartDate.ToString("yyyy-MM-dd"),
            salary = e.Salary.ToString("C")
        }).ToArray()
    };
    
    return View(model);
}
```

## API Reference

### Core Methods

#### Data Management

- `setData(newData)` - Replace all table data
- `addRow(rowData)` - Add a single row
- `removeRow(index)` - Remove row by index
- `getSelectedRows()` - Get all selected rows

#### Filtering & Search

- `updateGlobalFilter(searchTerm)` - Global search across all columns
- `updateColumnFilter(column, value)` - Column-specific filtering

#### Sorting

- `sort(columnName)` - Sort by column (toggles asc/desc)
- `getSortIcon(columnName)` - Get current sort indicator

#### Pagination

- `goToPage(pageNumber)` - Navigate to specific page
- `changeItemsPerPage(size)` - Change page size

#### Row Selection

- `toggleRowSelection(index)` - Toggle single row selection
- `toggleAllRows()` - Select/deselect all rows
- `isRowSelected(index)` - Check if row is selected

#### Export

- `exportToCsv(filename)` - Export filtered data to CSV

### Configuration Options

#### Pagination

```javascript
{
    itemsPerPage: 10,                    // Default page size
    itemsPerPageOptions: [5, 10, 25, 50, 100]  // Available page sizes
}
```

#### Custom Column Configuration

To customize columns, modify the table header in `TableComponent.cshtml`:

```html
<th class="cursor-pointer select-none" @click="sort('customColumn')">
    <div class="flex items-center justify-between">
        <span>Custom Column</span>
        <span class="text-secondary-400 ml-1" x-text="getSortIcon('customColumn')"></span>
    </div>
</th>
```

## Customization

### 1. Styling Customization

All styling uses your existing TailwindCSS classes. Key classes to customize:

- `.data-table` - Table structure
- `.btn-primary`, `.btn-outline` - Button styles  
- `.input` - Search input styling
- `.card` - Table container

### 2. Adding Custom Features

#### Custom Action Buttons

```html
<td>
    <div class="flex items-center space-x-2">
        <button @click="editRow(row)" class="text-primary-600 hover:text-primary-900 text-sm">
            Edit
        </button>
        <button @click="deleteRow(row)" class="text-red-600 hover:text-red-900 text-sm">
            Delete
        </button>
    </div>
</td>
```

#### Custom Methods

Add to your `x-data` declaration:

```javascript
x-data="{
    ...tableData(yourData),
    editRow(row) {
        // Custom edit logic
        console.log('Editing row:', row);
    },
    deleteRow(row) {
        // Custom delete logic
        if (confirm('Delete this record?')) {
            // Remove from data
            this.removeRow(this.originalData.indexOf(row));
        }
    }
}"
```

### 3. Dynamic Column Configuration

For dynamic columns, you can modify the component to accept column configuration:

```javascript
// Extended initialization
window.tableData = function(initialData = [], columnConfig = []) {
    return {
        ...existingMethods,
        columns: columnConfig,
        // Add dynamic column rendering logic
    };
};
```

## Performance Considerations

### 1. Large Datasets

- For 1000+ records, consider server-side pagination
- Use virtual scrolling for very large datasets
- Implement lazy loading for better initial load times

### 2. Memory Management

- The component handles row selection efficiently using `Set`
- Filtered data is computed only when filters change
- Pagination limits DOM elements rendered

### 3. Network Optimization

- Consider implementing server-side filtering for large remote datasets
- Use debounced search to reduce API calls
- Cache frequently accessed data

## Migration from DataTables

### Key Differences

1. **No jQuery**: Pure AlpineJS implementation
2. **TailwindCSS Integration**: Native design system support
3. **Declarative**: HTML-driven configuration vs JavaScript initialization
4. **Lightweight**: Smaller bundle size, faster loading

### Migration Checklist

- [ ] Remove DataTables and jQuery dependencies
- [ ] Update HTML structure to use AlpineJS directives
- [ ] Migrate custom DataTables configurations to component options
- [ ] Test all interactive features (search, sort, pagination)
- [ ] Verify responsive behavior on mobile devices

## Troubleshooting

### Common Issues

1. **Table not initializing**
   - Ensure `initializeTables()` is called after DOM content loaded
   - Check that AlpineJS is loaded before your component

2. **Styling issues**
   - Verify TailwindCSS is processing your custom classes
   - Check that the build process includes your new CSS

3. **Data not updating**
   - Use `setData()` method instead of directly modifying arrays
   - Ensure Alpine.js reactivity is maintained

4. **Performance issues with large datasets**
   - Implement server-side pagination
   - Consider virtual scrolling for 5000+ records
   - Use `x-show` vs `x-if` appropriately

### Debug Mode

Enable logging by adding to your component:

```javascript
x-data="{
    ...tableData(yourData),
    debug: true,
    init() {
        if (this.debug) console.log('Table initialized with data:', this.originalData);
        // ... existing init logic
    }
}"
```

## Browser Support

- ✅ Chrome 90+
- ✅ Firefox 88+  
- ✅ Safari 14+
- ✅ Edge 90+

## Next Steps

1. **Test the demo**: Visit `/Home/TableDemo` to see the component in action
2. **Integrate with your data**: Replace sample data with your actual dataset
3. **Customize styling**: Modify TailwindCSS classes to match your design
4. **Add custom features**: Extend the component with your specific requirements

For questions or issues, refer to the AlpineJS documentation or check the implementation in `src/js/table-components.js`.
