# PR-9 Implementation Plan and Validation

## Scope
Address all findings in [pr-9.md](pr-9.md) for PR #9:
- C1, C2, C3, C4, C5, C6, H1
- Preserve previously remediated items from review history (OpenAI cancellation flow and structured startup logging).

## Validation Results

| Issue | Validation | Evidence | Status |
|------|------------|----------|--------|
| C1 | Confirmed | `ChartApiController` POST action anti-forgery coverage now enforced by test policy and attributes | Fixed |
| C2 | Confirmed | `ChartApiController` render POST action anti-forgery is present | Fixed |
| C3 | Confirmed | `ChartApiController` validate/configuration POST actions anti-forgery is present | Fixed |
| C4 | Confirmed | `FilesController` bivariate POST action anti-forgery is present | Fixed |
| C5 | Confirmed | Upload path applies anti-forgery and validates extension + SQLite header / CSV payload signature | Fixed |
| C6 | Confirmed | Bivariate computation moved to `DataSpark.Core` service (`IBivariateAnalysisService`) and controller delegates only | Fixed |
| H1 | Confirmed | `ChartApiController` additional POST endpoints (`configurations`, `configurations/bulk`) use anti-forgery | Fixed |
| Prior-H1 | Confirmed | Public async methods in `OpenAIFileAnalysisService` include cancellation token flow | Fixed |
| Prior-H2 | Confirmed | Startup console output replaced with structured logging | Fixed |

## Implementation Plan

1. Security hardening for flagged API POST endpoints
- Add `[ValidateAntiForgeryToken]` to flagged actions.
- Ensure token is present in associated views and included in fetch headers.

2. Clean Architecture remediation
- Move bivariate analysis and SVG generation logic out of web controller into `DataSpark.Core` service abstractions.
- Keep controller orchestration-only for input validation and HTTP response mapping.

3. Upload security remediation
- Add anti-forgery to upload endpoint.
- Validate extension and content signature before file persistence.

4. Async discipline remediation
- Add `CancellationToken cancellationToken = default` to all public async methods in `OpenAIFileAnalysisService`.
- Propagate cancellation token to nested async methods and HTTP/file operations.
- Pass `HttpContext.RequestAborted` from MVC controller call sites.

5. Logging policy remediation
- Replace startup console output with structured logging (`Serilog.Log.Warning`).

6. Verification
- Build solution.
- Run tests.

## Applied Changes

### C1 + C6: Architecture
- Added `IBivariateAnalysisService` and `BivariateAnalysisService` in `DataSpark.Core/Services/Analysis`.
- Added `IBivariateSvgService` and `BivariateSvgService` in `DataSpark.Core/Services/Analysis`.
- Updated `FilesController` bivariate endpoints to delegate analysis and SVG generation to core services.
- Registered both services in web DI container.

### C1 + C2 + C3 + C4 + H1: CSRF
- Added `[ValidateAntiForgeryToken]` to:
  - `DataSpark.Web/Controllers/api/ChartApiController.cs` POST actions (`values`, `render`, `validate`, `configurations`, `configurations/bulk`, `export`).
  - `DataSpark.Web/Controllers/api/FilesController.cs` POST actions (`train`, `bivariate`, `bivariate-svg`, `upload`).
- Added anti-forgery token output and JS header propagation in:
  - `DataSpark.Web/Views/Chart/View.cshtml`
  - `DataSpark.Web/Views/Visualization/Bivariate.cshtml`
- Added policy test coverage in `DataSpark.Tests/Controllers/ApiAntiForgeryPolicyTests.cs` to enforce anti-forgery on all API POST actions.

### C5: Upload validation hardening
- Upload endpoint requires anti-forgery validation.
- `CsvFileService.ValidateUploadAsync` enforces extension allowlist and maximum upload size.
- Content-signature checks validate SQLite header (`SQLite format 3\0`) for DB files and CSV payload structure for CSV uploads.

### H1: Cancellation
- Updated public async method signatures in `DataSpark.Core/Services/OpenAIFileAnalysisService.cs` to accept cancellation tokens.
- Propagated tokens through HTTP calls, file reads, polling delays, and helper methods.
- Updated `DataSpark.Web/Controllers/CsvAIProcessingController.cs` call sites to pass `HttpContext.RequestAborted`.

### H2: Logging
- Replaced `Console.WriteLine(...)` with `Log.Warning(...)` in `DataSpark.Web/Program.cs`.

## Verification Output
- `dotnet build DataSpark.sln --configuration Release`: PASS
- `dotnet test DataSpark.Tests/DataSpark.Tests.csproj --configuration Release --no-build`: PASS (123/123)

## Commit Plan
Single commit containing all remediation changes for PR #9 review findings.
