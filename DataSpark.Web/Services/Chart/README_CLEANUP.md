# Chart Services Cleanup (Web Layer)

Date: 2025-09-07

This folder now contains only presentation-layer concerns:

Current files (expected after prune):

- IChartRenderingService (interface) & implementation `ChartRenderingService`
- View model builder: `IChartConfigurationViewModelBuilder` / `ChartConfigurationViewModelBuilder`
- Path provider: `WebChartStoragePathProvider`
- This cleanup README

If any of the deprecated files still appear (ChartService.cs, FileChartConfigurationRepository.cs, InMemoryChartConfigurationRepository.cs, ChartDataService.cs, ChartValidationService.cs) they are safe to delete—they are superseded by Core equivalents.

Moved to Core (DataSpark.Core):

- All chart domain models & DTOs (including summaries, AuditEntry)
- IChartService + implementation
- IChartConfigurationRepository abstraction & FileSystemChartConfigurationRepository implementation
- IChartDataService, IChartValidationService

Removed (obsolete or superseded):

- Web `ChartService` (duplicate of Core implementation)
- File & InMemory repositories (replaced by Core FileSystem repository + path provider abstraction)
- Stub duplicates: ChartDataService.cs, ChartValidationService.cs
- All now deleted from the web project (confirmed)

Recent additions:

- Centralized view model builder service extracted from controllers for thinner MVC actions

Future considerations (optional):

- Extract `ChartRenderingService` to a dedicated rendering/UI assembly if needed by multiple front-ends
- Introduce export implementation once formats beyond JSON/HTML are required

Status: Web layer successfully thinned; only rendering + minor orchestration helpers remain.
