# Sql2Csv Web Application

A modern, responsive web application for SQLite database analysis and CSV export built with .NET 9.0 and Tailwind CSS.

## Features

- **Secure File Upload**: Upload SQLite database files with validation and size limits
- **Database Analysis**: Comprehensive schema analysis with table and column details
- **CSV Export**: Export individual tables or entire databases to CSV format
- **Code Generation**: Generate C# model classes from database schema
- **Modern UI**: Built with Tailwind CSS and Alpine.js for a responsive, interactive experience

## Technology Stack

### Backend

- .NET 9.0
- ASP.NET Core MVC
- Microsoft.Data.Sqlite
- Dependency Injection

### Frontend

- Tailwind CSS 3.4
- SASS preprocessing
- Alpine.js for interactivity
- Webpack for asset bundling
- Google Fonts (Inter, JetBrains Mono)

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Node.js (for frontend build tools)

### Installation

1. Install npm dependencies:

```bash
npm install
```

2. Build frontend assets:

```bash
npm run build
```

3. Build the application:

```bash
dotnet build
```

4. Run the application:

```bash
dotnet run
```

### Development

For development with automatic CSS rebuilding:

```bash
npm run dev
```

This will watch for changes in your SCSS files and rebuild the CSS automatically.

## Project Structure

```
sql2csv.web/
├── Controllers/           # MVC Controllers
├── Models/               # View Models
├── Views/               # Razor Views
├── Services/            # Business Logic Services
├── wwwroot/            # Static web assets
├── src/                # Source files for build pipeline
│   ├── css/           # SASS/CSS source files
│   └── js/            # JavaScript source files
├── package.json        # Node.js dependencies
├── webpack.config.js   # Webpack configuration
├── tailwind.config.js  # Tailwind CSS configuration
└── postcss.config.js   # PostCSS configuration
```

## Build Pipeline

The application uses a sophisticated build pipeline:

1. **SASS Compilation**: SCSS files are compiled to CSS
2. **Tailwind CSS Processing**: Utility classes are generated and purged
3. **PostCSS Processing**: Autoprefixer and other optimizations
4. **Webpack Bundling**: JavaScript and CSS assets are bundled
5. **Asset Optimization**: Files are minified for production

## Security Features

- File type validation (only SQLite files accepted)
- File size limits (50MB maximum)
- SQLite file validation before processing
- Temporary file cleanup
- No permanent file storage

## Deployment

The application is designed for deployment to IIS on Windows Server:

1. Build for production:

```bash
npm run build
dotnet publish -c Release
```

2. Copy the published files to your web server
3. Configure IIS to serve the application

## API Endpoints

- `GET /` - Main upload page
- `POST /Upload` - Handle file upload
- `GET /Analyze` - Display database analysis
- `POST /ExportTables` - Export selected tables to CSV
- `POST /GenerateCode` - Generate C# model classes

## Configuration

The application can be configured through `appsettings.json`:

- File upload limits
- Temporary file storage location
- Logging configuration

## Contributing

This project follows the same patterns as the main Sql2Csv solution. When making changes:

1. Update both backend and frontend code as needed
2. Run tests to ensure compatibility
3. Update documentation for any new features

## License

This project is part of the Sql2Csv solution and follows the same licensing terms.
