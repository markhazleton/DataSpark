# DataSpark.Web - Comprehensive Functional Specification

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture & Technology Stack](#architecture--technology-stack)
3. [Core Modules & Functionality](#core-modules--functionality)
4. [User Interface & Navigation](#user-interface--navigation)
5. [API Documentation](#api-documentation)
6. [Data Processing & Analysis Features](#data-processing--analysis-features)
7. [Security & Configuration](#security--configuration)
8. [Deployment & Infrastructure](#deployment--infrastructure)

---

## Project Overview

**DataSpark.Web** is a comprehensive ASP.NET Core 9.0 web application designed for CSV data analysis, visualization, and exploration. It provides a modern, user-friendly interface for performing exploratory data analysis (EDA), creating interactive charts, building pivot tables, and leveraging AI-powered insights through OpenAI integration.

### Key Features
- **CSV File Management**: Upload, process, and manage CSV datasets
- **Interactive Charts**: Create and configure various chart types with real-time previews
- **Pivot Tables**: Drag-and-drop pivot table creation with multiple visualization options
- **AI-Powered Analysis**: OpenAI integration for intelligent data insights
- **Statistical Analysis**: Univariate and bivariate analysis with detailed statistics
- **RESTful API**: Complete API for programmatic access to all functionality
- **Modern UI**: Responsive design with Bootstrap 5 and Bootswatch themes

---

## Architecture & Technology Stack

### Core Technologies
- **Framework**: ASP.NET Core 9.0 (MVC Pattern)
- **Language**: C# 12 with nullable reference types
- **Frontend**: HTML5, CSS3, JavaScript, Bootstrap 5
- **Data Processing**: CsvHelper, Microsoft.Data.Analysis, Microsoft.ML
- **Charts**: ScottPlot, Chart.js, PivotTable.js, Plotly.js
- **AI Integration**: OpenAI API with Assistants v2
- **Themes**: WebSpark.Bootswatch for dynamic theme switching

### Project Structure
```
DataSpark.Web/
├── Controllers/           # MVC Controllers
│   ├── api/              # API Controllers
│   ├── BaseController.cs # Shared controller functionality
│   └── ...
├── Models/               # Data models and view models
│   ├── Chart/           # Chart-specific models
│   └── ...
├── Services/            # Business logic services
│   ├── Chart/          # Chart-related services
│   └── ...
├── Views/               # Razor views and templates
│   ├── Chart/          # Chart views
│   ├── Shared/         # Shared layouts and partials
│   └── ...
└── wwwroot/            # Static web assets
```

### Dependencies
- **CsvHelper** (33.1.0) - CSV parsing and processing
- **Microsoft.ML** (4.0.2) - Machine learning capabilities
- **Microsoft.Data.Analysis** (0.22.2) - Data analysis operations
- **ScottPlot** (5.0.55) - Scientific plotting
- **WebSpark.Bootswatch** (1.20.1) - Theme management
- **Serilog.AspNetCore** (8.0.4) - Structured logging

---

## Core Modules & Functionality

### 1. Home Module (`HomeController`)

**Purpose**: Main entry point, file upload, and overview functionality

**Features**:
- **File Upload**: Drag-and-drop CSV file upload with validation
- **File Management**: View uploaded files with metadata
- **Complete Analysis**: Comprehensive data analysis with fallback processing
- **Landing Page**: Introduction to features and capabilities

**Key Actions**:
- `Index()` - Main landing page with file listing
- `UploadCSV(IFormFile)` - Handle file uploads with validation
- `CompleteAnalysis(string fileName)` - Full data analysis report
- `Files()` - File management interface

### 2. Chart Module (`ChartController`)

**Purpose**: Advanced charting and visualization system

**Features**:
- **Chart Configuration**: Create and edit chart configurations
- **Multiple Chart Types**: Column, Bar, Line, Area, Pie, Scatter, Bubble, etc.
- **Real-time Preview**: Live chart preview during configuration
- **Data Filtering**: Apply filters to chart data
- **Export Options**: PNG, JPEG, PDF, SVG, CSV, Excel
- **Embed Support**: Generate embeddable chart code

**Key Actions**:
- `Index(string dataSource)` - Chart listing and management
- `Configure(int? id, string dataSource)` - Chart configuration interface
- `View(int id)` - Display configured chart
- `Preview([FromBody] ChartPreviewRequest)` - AJAX chart preview
- `Delete(int id)` - Remove chart configuration
- `Duplicate(int id, string newName)` - Clone chart configuration
- `Embed(int id)` - Embeddable chart view

**Chart Types Supported**:
- Column/Bar Charts (2D/3D)
- Line Charts (single/multi-series)
- Area Charts (stacked/unstacked)
- Pie/Doughnut Charts
- Scatter Plots
- Bubble Charts
- Combination Charts

### 3. Pivot Table Module (`PivotTableController`)

**Purpose**: Interactive pivot table creation and analysis

**Features**:
- **Drag-and-Drop Interface**: Visual pivot table builder
- **Multiple Renderers**: Table, Heatmap, Bar Chart, Line Chart
- **Export Capabilities**: CSV, TSV, JSON, Excel
- **Configuration Saving**: Save and load pivot configurations
- **Full-Screen Mode**: Dedicated full-screen pivot interface

**Key Actions**:
- `Index()` - Main pivot table interface
- `FullPage()` - Full-screen pivot mode
- `Results(string fileName, List<string> columns)` - Pivot results view
- `LoadCsvData([FromBody] LoadCsvDataRequest)` - Load data for pivot
- `SaveConfiguration([FromBody] SaveConfigurationRequest)` - Save pivot config
- `Export(string format, string configuration)` - Export pivot data

**Supported Renderers**:
- Table with sorting and filtering
- Heatmap visualizations
- Bar charts and histograms
- Line charts for trends
- C3.js and Plotly.js integrations

### 4. AI Processing Module (`CsvAIProcessingController`)

**Purpose**: OpenAI-powered intelligent data analysis

**Features**:
- **File Analysis**: AI-powered CSV analysis with insights
- **File Registration**: Upload files to OpenAI for persistent analysis
- **Multi-file Analysis**: Compare and analyze multiple datasets
- **Custom Prompts**: User-defined analysis questions
- **Diagnostic Tools**: Configuration validation and troubleshooting

**Key Actions**:
- `Index(string fileName)` - Main AI processing interface
- `AnalyzeFile(string fileName, string customPrompt)` - Single file analysis
- `UploadAndRegisterFile(string fileName)` - Register file with OpenAI
- `AnalyzeUploadedFiles(List<string> selectedFileIds, string customPrompt)` - Multi-file analysis
- `AnalyzeAllFiles(string customPrompt)` - Analyze all uploaded files
- `RemoveFile(string fileId)` - Remove file from OpenAI
- `ClearAllFiles()` - Clear all uploaded files
- `RunDiagnostics()` - System diagnostics

### 5. Univariate Analysis Module (`UnivariateController`)

**Purpose**: Single-variable statistical analysis

**Features**:
- **Statistical Summaries**: Mean, median, mode, standard deviation
- **Distribution Analysis**: Histograms, box plots, Q-Q plots
- **Descriptive Statistics**: Min, max, quartiles, skewness, kurtosis
- **Missing Value Analysis**: Null counts and patterns
- **Visual Generation**: SVG-based statistical plots

**Key Actions**:
- `Index(string fileName)` - Select file and column for analysis
- `Analyze(string fileName, string columnName)` - Generate univariate analysis

### 6. Visualization Module (`VisualizationController`)

**Purpose**: General data visualization and exploration

**Features**:
- **Data Preview**: View CSV data structure and sample records
- **Column Analysis**: Examine individual column characteristics
- **Bivariate Exploration**: Two-variable relationship analysis
- **Interactive Charts**: Dynamic chart generation
- **Pivot Integration**: Access to pivot table functionality

**Key Actions**:
- `Index()` - Main visualization dashboard
- `Data(string fileName)` - Get CSV data for visualization
- `Bivariate()` - Bivariate analysis interface
- `Pivot()` - Pivot table access
- `Univariate()` - Univariate analysis access

### 7. Sanity Check Module (`SanityCheckController`)

**Purpose**: Data quality assessment and validation

**Features**:
- **Data Structure Validation**: Check file format and structure
- **Missing Value Detection**: Identify null and empty values
- **Sample Data Preview**: Display first few rows
- **Basic Statistics**: Row counts, column counts
- **Quality Indicators**: Data completeness metrics

**Key Actions**:
- `Index(string fileName)` - Perform sanity check on selected file

---

## User Interface & Navigation

### Layout System

**Primary Layout** (`_Layout.cshtml`):
- Bootstrap 5 responsive design
- Dynamic theme switching with Bootswatch
- Collapsible navigation for mobile devices
- FontAwesome icons throughout interface

**Alternative Layout** (`_LayoutAlternative.cshtml`):
- Sidebar navigation for power users
- Full-screen content area
- Quick links to major features

### Navigation Structure

**Main Navigation**:
- **Home**: File upload and management
- **Charts**: Chart creation and management
- **Pivot Tables**: Interactive pivot analysis
- **Analysis**: Statistical analysis tools
  - Univariate Analysis
  - Bivariate Analysis
  - Complete Analysis
- **AI Processing**: OpenAI-powered insights
- **Visualization**: General visualization tools
- **Sanity Check**: Data quality assessment

### Theme System

**Bootswatch Integration**:
- 20+ Bootstrap themes available
- Real-time theme switching
- Dark/light mode support
- Persistent theme preferences
- Mobile-optimized responsive design

---

## API Documentation

### Chart API (`/api/Chart`)

**Data Endpoints**:
- `GET /api/Chart/data/{dataSource}` - Get processed chart data
- `GET /api/Chart/columns/{dataSource}` - Get column information
- `GET /api/Chart/values/{dataSource}/{column}` - Get unique column values
- `POST /api/Chart/values/{dataSource}` - Get values for multiple columns
- `GET /api/Chart/summary/{dataSource}` - Get data summary statistics

**Configuration Endpoints**:
- `GET /api/Chart/configurations` - List chart configurations
- `GET /api/Chart/configurations/{id}` - Get specific configuration
- `POST /api/Chart/configurations` - Save chart configuration
- `DELETE /api/Chart/configurations/{id}` - Delete configuration

**Rendering Endpoints**:
- `POST /api/Chart/render` - Render chart from configuration
- `POST /api/Chart/validate` - Validate chart configuration

**Utility Endpoints**:
- `GET /api/Chart/datasources` - Get available data sources
- `GET /api/Chart/charttypes` - Get available chart types
- `GET /api/Chart/palettes` - Get color palettes

### Files API (`/api/Files`)

**File Management**:
- `POST /api/Files/upload` - Upload new CSV file
- `GET /api/Files/list` - List files with EDA
- `GET /api/Files/summary` - Get files summary
- `GET /api/Files/exists?fileName={name}` - Check file existence

**Data Access**:
- `GET /api/Files/headers?fileName={name}` - Get CSV headers
- `GET /api/Files/data?fileName={name}&skip={n}&take={n}` - Get paginated data
- `GET /api/Files/json?fileName={name}` - Convert CSV to JSON
- `GET /api/Files/processed?fileName={name}` - Get processed CSV analysis
- `GET /api/Files/visualization?fileName={name}` - Get visualization data

**Analysis Endpoints**:
- `GET /api/Files/eda?fileName={name}` - Exploratory data analysis
- `POST /api/Files/bivariate` - Bivariate analysis
- `GET /api/Files/column-stats?fileName={name}&columnName={col}` - Column statistics
- `POST /api/Files/train` - Train ML model

---

## Data Processing & Analysis Features

### CSV Processing Pipeline

**File Upload & Validation**:
1. File format validation (CSV, TXT with delimiters)
2. Size limits and security checks
3. Encoding detection and conversion
4. Header validation and parsing

**Data Processing Steps**:
1. **Type Detection**: Automatic detection of numeric, categorical, and date columns
2. **Statistical Analysis**: Calculate descriptive statistics for all columns
3. **Quality Assessment**: Missing value detection, duplicate identification
4. **Correlation Analysis**: Compute correlation matrices for numeric data
5. **Distribution Analysis**: Generate histograms, box plots, and summary statistics

### Statistical Analysis Capabilities

**Univariate Analysis**:
- Descriptive statistics (mean, median, mode, std dev)
- Distribution analysis (histograms, density plots)
- Outlier detection using IQR and standard deviation methods
- Normality testing and distribution fitting
- Missing value patterns and imputation suggestions

**Bivariate Analysis**:
- Correlation analysis (Pearson, Spearman)
- Scatter plots with regression lines
- Contingency tables for categorical variables
- Cross-tabulation and chi-square tests
- Regression analysis (linear, polynomial)

**Multivariate Analysis**:
- Principal Component Analysis (PCA)
- Correlation heatmaps
- Clustering analysis
- Feature importance ranking

### Machine Learning Integration

**Microsoft ML.NET Integration**:
- Automatic model selection (regression vs. classification)
- Data preprocessing and feature engineering
- Model training with cross-validation
- Performance metrics calculation
- Model evaluation and comparison

**Supported Algorithms**:
- Linear/Logistic Regression
- Decision Trees
- Random Forest
- Support Vector Machines
- Neural Networks (via ML.NET)

### Chart Generation System

**Chart Types & Configurations**:
- **Column/Bar Charts**: Vertical and horizontal orientations, stacked options
- **Line Charts**: Single and multi-series, with trend lines
- **Area Charts**: Filled area plots, stacked areas
- **Pie/Doughnut Charts**: With percentage labels and legends
- **Scatter Plots**: With regression lines and confidence intervals
- **Bubble Charts**: Three-dimensional data representation
- **Combination Charts**: Mixed chart types in single visualization

**Chart Customization**:
- Color palettes and themes
- Axis configuration and scaling
- Legend positioning and styling
- Title and subtitle customization
- Background images and watermarks
- Animation and interaction settings

---

## Security & Configuration

### Authentication & Authorization

**Current Implementation**:
- No authentication required (designed for internal/demo use)
- File upload restrictions and validation
- Input sanitization and validation throughout

**Security Measures**:
- CSRF protection on form submissions
- File type and size validation
- Path traversal prevention
- Input encoding and sanitization
- HTTPS enforcement in production

### Configuration Management

**Application Settings** (`appsettings.json`):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "CsvOutputFolder": "c:\\websites\\WebSpark\\CsvOutput",
  "HttpRequestResultPollyOptions": {
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 1,
    "CircuitBreakerThreshold": 3,
    "CircuitBreakerDurationSeconds": 10
  },
  "AllowedHosts": "*"
}
```

**User Secrets** (Development):
- OpenAI API Key configuration
- Assistant ID for AI processing
- Other sensitive configuration values

**Environment Variables**:
- Production database connection strings
- External service API keys
- Feature flags and toggles

### OpenAI Integration Configuration

**Required Settings**:
- `OpenAI:ApiKey` - OpenAI API authentication key
- `OpenAI:AssistantId` - Configured assistant for CSV analysis

**Features**:
- File upload to OpenAI for analysis
- Conversation threading for context
- Custom prompt support
- Multi-file analysis capabilities
- Automatic cleanup of temporary files

---

## Deployment & Infrastructure

### Development Environment

**Requirements**:
- .NET 10.0 SDK
- Visual Studio 2022 or VS Code
- Node.js (for frontend package management)
- Git for version control

**Setup Steps**:
1. Clone repository
2. Configure user secrets for OpenAI
3. Restore NuGet packages
4. Run database migrations (if applicable)
5. Start application with `dotnet run`

### Production Deployment

**Hosting Options**:
- Azure App Service (recommended)
- IIS on Windows Server
- Linux with Nginx reverse proxy
- Docker containers

**Configuration Requirements**:
- SSL certificate for HTTPS
- File storage location configuration
- OpenAI API key configuration
- Logging and monitoring setup

### Performance Considerations

**Optimization Features**:
- Memory caching for frequent data access
- Async/await throughout for scalability
- Efficient CSV parsing with streaming
- Lazy loading of chart data
- Client-side caching of static assets

**Scalability Features**:
- Stateless design for horizontal scaling
- Database-agnostic chart configuration storage
- CDN support for static assets
- API rate limiting and throttling

### Monitoring & Logging

**Serilog Integration**:
- Structured logging throughout application
- Log levels: Debug, Information, Warning, Error, Fatal
- File and console output sinks
- Correlation IDs for request tracking

**Health Checks**:
- Application health monitoring
- Dependency health checks (OpenAI API)
- Performance counter collection
- Custom metrics and alerts

---

## Future Enhancements

### Planned Features

**Enhanced AI Integration**:
- Multiple AI provider support (Azure OpenAI, Google AI)
- Custom model fine-tuning
- Automated insight generation
- Natural language query interface

**Advanced Analytics**:
- Time series analysis and forecasting
- Advanced statistical testing
- Machine learning model deployment
- Real-time data streaming support

**Collaboration Features**:
- User authentication and authorization
- Shared dashboards and reports
- Comment and annotation system
- Version control for configurations

**Enterprise Features**:
- Database connectivity (SQL Server, PostgreSQL, MySQL)
- Active Directory integration
- Audit logging and compliance
- Role-based access control

### Technical Improvements

**Performance Optimizations**:
- Background job processing
- Distributed caching with Redis
- Database query optimization
- Progressive web app (PWA) support

**User Experience Enhancements**:
- Advanced chart editor with drag-and-drop
- Mobile-first responsive design improvements
- Keyboard shortcuts and accessibility
- Multi-language support (i18n)

---

## Conclusion

DataSpark.Web represents a comprehensive solution for CSV data analysis and visualization, combining modern web technologies with powerful data processing capabilities. The modular architecture allows for easy extension and customization, while the intuitive user interface makes advanced analytics accessible to users of all skill levels.

The application successfully integrates multiple analysis approaches - from traditional statistical methods to AI-powered insights - providing a versatile platform for data exploration and visualization. With its RESTful API and responsive design, DataSpark.Web can serve both as a standalone analysis tool and as a component in larger data processing workflows.

---

*This specification document covers the complete functional scope of DataSpark.Web as of the current implementation. For technical implementation details, refer to the source code and inline documentation.*
