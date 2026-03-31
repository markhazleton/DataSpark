# PR-9 Implementation Plan and Validation

## Scope
Address all findings in [pr-9.md](pr-9.md) for PR #9:
- C1, C2, C3, H1, H2

## Validation Results

| Issue | Validation | Evidence | Status |
|------|------------|----------|--------|
| C1 | Confirmed | Bivariate SVG logic existed in controller layer before fix | Fixed |
| C2 | Confirmed | `POST /api/Chart/Export` lacked anti-forgery validation | Fixed |
| C3 | Confirmed | `POST /api/files/bivariate-svg` lacked anti-forgery validation | Fixed |
| H1 | Confirmed | Public async methods in `OpenAIFileAnalysisService` lacked `CancellationToken` | Fixed |
| H2 | Confirmed | `Console.WriteLine` used in startup path | Fixed |

## Implementation Plan

1. Security hardening for flagged API POST endpoints
- Add `[ValidateAntiForgeryToken]` to flagged actions.
- Ensure token is present in associated views and included in fetch headers.

2. Clean Architecture remediation
- Move bivariate SVG generation logic out of web controller into `DataSpark.Core` service abstraction.
- Keep controller orchestration-only for input validation and HTTP response mapping.

3. Async discipline remediation
- Add `CancellationToken cancellationToken = default` to all public async methods in `OpenAIFileAnalysisService`.
- Propagate cancellation token to nested async methods and HTTP/file operations.
- Pass `HttpContext.RequestAborted` from MVC controller call sites.

4. Logging policy remediation
- Replace startup console output with structured logging (`Serilog.Log.Warning`).

5. Verification
- Build solution.
- Run tests.

## Applied Changes

### C1: Architecture
- Added `IBivariateSvgService` and `BivariateSvgService` in `DataSpark.Core/Services/Analysis`.
- Updated `FilesController` to delegate SVG generation to core service.
- Registered service in web DI container.

### C2 + C3: CSRF
- Added `[ValidateAntiForgeryToken]` to:
  - `DataSpark.Web/Controllers/api/ChartApiController.cs` export action.
  - `DataSpark.Web/Controllers/api/FilesController.cs` bivariate SVG action.
- Added anti-forgery token output and JS header propagation in:
  - `DataSpark.Web/Views/Chart/View.cshtml`
  - `DataSpark.Web/Views/Visualization/Bivariate.cshtml`

### H1: Cancellation
- Updated public async method signatures in `DataSpark.Core/Services/OpenAIFileAnalysisService.cs` to accept cancellation tokens.
- Propagated tokens through HTTP calls, file reads, polling delays, and helper methods.
- Updated `DataSpark.Web/Controllers/CsvAIProcessingController.cs` call sites to pass `HttpContext.RequestAborted`.

### H2: Logging
- Replaced `Console.WriteLine(...)` with `Log.Warning(...)` in `DataSpark.Web/Program.cs`.

## Verification Output
- `dotnet build DataSpark.sln --configuration Release`: PASS
- `dotnet test DataSpark.Tests/DataSpark.Tests.csproj --configuration Release --no-build`: PASS (119/119)

## Commit Plan
Single commit containing all remediation changes for PR #9 review findings.
