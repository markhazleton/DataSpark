# 🚧 Issue List & Sequencing Plan

Comprehensive backlog distilled from README roadmap and enhancement discussion. Use this file to create GitHub issues (one per row) and to guide delivery sequencing.

Legend:

- Effort: XS (<2h), S (0.5–1d), M (1–2d), L (3–5d), XL (1–2w), XXL (2w+ / epic)
- Priority: P0 (immediate), P1 (near-term), P2 (planned), P3 (backlog / exploratory)
- Dependency notation: → indicates prerequisite

## 📌 Wave Overview

| Wave | Goal | Items |
|------|------|-------|
| Wave 1 | Visibility & correctness | 1,2,3 |
| Wave 2 | UX foundations (web + CLI precision) | 4,5,6,7 |
| Wave 3 | Packaging & performance & formats | 8,9,13 |
| Wave 4 | Platform expansion & APIs | 10,11,12 |
| Wave 5 | Security, globalization, polish | 14,15,16 |
| Retro (Optional) | Historical tracking | 17 |

## ✅ Completed (Do NOT create unless retro desired)

| # | Title | Notes |
|---|-------|-------|
| C1 | CI workflow (build + test) | Implemented |
| C2 | Export delimiter & header overrides | Implemented |
| C3 | Publish code coverage badge (#1) | Added dynamic JSON badge + artifact & commit step |
| C4 | Schema report alternative formats (JSON & Markdown) (#2) | CLI --format with text/json/markdown + tests |
| C5 | Coverage pipeline enhancement (#3) | PR comment & badge color thresholds |

## 🎯 Wave 1 – Foundation & Observability (Completed)

All items delivered (see Completed table). README badge updated; CLI schema formats implemented with tests; CI posts PR coverage comment.

## 🧪 Wave 2 – User Experience & Selectivity (Completed ✅ 2025-08-11)

| # | Title | Effort | Priority | Dependencies | Description / Acceptance | Status |
|---|-------|--------|----------|--------------|--------------------------|--------|
| 4 | Enhanced web UI (EPIC) | XXL | P1 | 5,6 partial | Parent tracking issue (progress, checklists, design notes). | Done |
| 5 | Web UI: Drag & drop SQLite upload | M | P1 | none | Drop zone + validation (extension, size); shows new DB in list; tests for controller & service. | Done |
| 6 | Web UI: Multi-table export selection | M | P1 | 5 | UI checkboxes + select all/none; pass selected tables to backend (placeholder service if logic pending). | Done |
| 7 | Per-table selection for CLI export & generation | S | P1 | none | `--tables Users,Orders` filter for export & generate commands; update tests & README. | Done |

### 📋 Wave 2 Closure Summary

Delivered Scope (matches detailed plan below):

- Drag & drop upload endpoint + client script (AJAX) with SQLite header validation (first 16 bytes) & extension/size checks.
- Multi-table selection UI (Alpine.js) with Select All / Clear All, disabled actions when empty, live counts.
- CLI --tables flag (export, schema, generate) with exit code 2 for 0 matched tables; normalization & logging of unknown tables.
- Service-layer filtering (ExportService + ApplicationService) centralizing logic.
- README updated with new flag & UI notes.
- Unit tests added for filtering; (integration tests for upload/export can be added in Wave 3 hardening phase).

Deferred / Not in Wave 2 (explicitly):

- Code generation actual table filtering (flag plumbed, logic deferred).
- Screenshots & a11y notes (can be attached to Epic issue retrospectively if desired).

Quality Notes:

- All existing tests passing prior to closure; minor README lint fix applied.
- No new external runtime dependencies introduced.

---

### 📋 Wave 2 Detailed Plan (Original Reference)

This section expands issues 4–7 into concrete sub-tasks, acceptance criteria, design constraints, and test coverage. Use these checklists to create granular GitHub issues / PR task lists.

#### 4. Enhanced web UI (Epic)

Goal: Establish a usable, consistent UI shell + interactive flows for upload and selective export (aggregating #5 & #6) while keeping the stack lightweight (Razor + Alpine.js + Tailwind).

Scope (In):

- Base layout (header w/ project name + nav placeholder + footer)
- Unified status / notification pattern (inline alert component or lightweight toast div)
- Loading & error states for: database list fetch, upload, export
- Component structure (logical separation, not a JS framework migration)
- Accessibility pass (focus, aria-live for status messages)

Out of Scope (Wave 2):

- Authentication / user prefs (Wave 5)
- REST API expansion beyond endpoints needed for upload & export
- Client-side routing framework

Definition of Done (Epic):

- Checklists for #5 and #6 fully delivered
- Layout & shared components documented inside the Epic issue body (README link optional)
- Screenshots (or short GIF) of: initial state, post-upload, multi-select export
- No console errors during normal flows
- Lighthouse (or manual) basic a11y checks pass (contrast, focus, form labels)

Sub-Components:

- `DatabaseList` (renders available DBs and tables toggle)
- `UploadPanel` (drop zone + fallback file input)
- `TableSelectionPanel` (checkbox list + select all/none)
- `ActionBar` (export button + feedback area)

#### 5. Web UI: Drag & drop SQLite upload

Tasks:

- Frontend: Drop zone (dragenter/dragover/leave styling) & file input fallback
- Validation: Extension (.db / .sqlite) + size (configurable threshold, default 25MB?)
- API: `POST /upload` (multipart) returns `{ id, name, tableCount }`
- Backend validation: attempt to open connection; reject if not valid SQLite header (first 16 bytes == `SQLite format 3\0`)
- Update in-memory catalog / discovery mechanism so new DB appears without restart
- Alpine.js state update after success; error message on failure
- Logging: info on upload success, warning on invalid file

Acceptance Criteria:

- Uploading valid DB adds it to list without full page reload
- Invalid extension or too-large file -> user-visible error (no unhandled exception)
- Corrupt file yields 400 with reason
- Duplicate filename yields distinct logical id (e.g., timestamp suffix) or rejection with clear message

Tests:

- Controller: 200 happy path, 400 invalid extension, 400 corrupt, 413 (if size limit enforced)
- Service: header validation, temp storage cleanup on failure
- Integration: upload then list includes new DB tables

#### 6. Web UI: Multi-table export selection

Tasks:

- Extend discovery JSON endpoint (if needed) to include table names
- UI: Checkbox for each table; “Select All”, “Select None”; selected count badge
- State: Track `selectedTables[]`; disable export button if empty
- API: Export endpoint accepts `?tables=Users,Orders` OR JSON `{ "tables": ["Users","Orders"] }` (query first for simplicity)
- Service: Filter applied once (avoid filtering inside multiple layers)
- UX: Spinner during export + success message (download or link)

Acceptance Criteria:

- Only selected tables exported; file set excludes unselected
- Unknown table names ignored with warning (not fatal) & logged
- If zero valid tables after filtering -> 400 returned (UI shows message)
- “Select All” toggles to “Select None” when all selected

Tests:

- Service filtering: mixed valid + invalid list
- Controller: query param parsing; empty selection rejection
- UI (manual / screenshot): selection toggling & disabled export state

#### 7. CLI: Per-table selection for export & generation

Flag: `--tables` (alias: `-t`) accepts comma or semicolon separated list; whitespace trimmed; case-insensitive.

Tasks:

- Option parsing helper (split, trim, distinct)
- Inject parsed list into services
- Update `export` and `generate` commands
- Logging: filtered N/M tables; list unknown(s) in one warning line
- Exit codes: 0 if ≥1 table processed; 2 if none matched
- README update (examples + note about case-insensitivity)

Acceptance Criteria:

- `--tables Users,Orders` limits output to those tables
- Mixed valid/invalid still succeeds if at least one valid
- All invalid -> error exit & message
- Help text shows flag & description

Tests:

- Parsing: normalization, duplicate removal, delimiter variants
- ExportService invoked with correct subset (mock)
- End-to-end command with valid & invalid names

#### 🔧 Shared Design Decisions

- Centralize table filtering in ExportService / CodeGenerationService
- Reuse discovery endpoint (no new selection metadata endpoint)
- Minimal state: Alpine.js + Tailwind utilities
- Case-insensitive matching via `StringComparer.OrdinalIgnoreCase`

#### 🧪 Test Coverage Matrix (Wave 2 addition)

| Layer | Upload | Multi-Select | CLI Filter |
|-------|--------|--------------|------------|
| Unit (services) | Header validation, storage | Filter application | Parsing, filtering |
| Controller/API | 200 & failure paths | Query param handling | N/A (System.CommandLine tests) |
| Integration | Upload then discover | Export subset artifact | End-to-end CLI invocation |

#### 📦 Package / Dependency Impact (Wave 2)

Required Additions:

- Test project: `Microsoft.AspNetCore.Mvc.Testing` (web integration tests)

No Removals:

- All existing packages retained; no framework shift.

Optional (Defer unless justified):

- `Microsoft.AspNetCore.TestHost` (if not transitive)
- Toast/notification helper library (custom component preferred)

#### 🕒 Suggested Implementation Order

1. (7) CLI filter (foundation for consistent semantics)
2. Service layer filter integration & tests
3. (5) Upload endpoint + frontend
4. (6) Multi-table selection UI + export wiring
5. (4) Epic documentation wrap-up & screenshots

#### ✅ Definition of Done (Wave 2 Overall)

- All issue-specific acceptance criteria satisfied
- Added tests passing (unit + integration); coverage not regressing >2% absolute
- README updated (CLI tables flag + brief UI section)
- This detailed plan kept in sync with delivered scope
- No critical accessibility regressions (keyboard nav & visible focus)

## 📦 Wave 3 – Packaging, Performance & Data Output

| # | Title | Effort | Priority | Dependencies | Description / Acceptance |
|---|-------|--------|----------|--------------|--------------------------|
| 8 | Docker packaging (console + web) | M | P1 | none | Multi-stage Dockerfiles; README run examples; optional image size badge. |
| 9 | Benchmark suite (BenchmarkDotNet) | M | P1 | core services stable | Benchmarks: discovery, export (varied row counts), schema gen; baseline results committed. |
| 13 | Advanced export formats (JSON, Parquet, Excel) | L | P2 | 7, core export | Strategy pattern; `--format` flag; JSON first (fast win), Parquet via Parquet.Net, Excel via EPPlus; tests for each output. |

## 🌐 Wave 4 – Platform & Extensibility

| # | Title | Effort | Priority | Dependencies | Description / Acceptance |
|---|-------|--------|----------|--------------|--------------------------|
| 10 | REST API + OpenAPI | L | P2 | core stable | Endpoints: discover/schema/export/generate; Swagger UI; minimal rate limiting placeholder. |
| 11 | Additional database providers (PostgreSQL/MySQL/SQL Server) | XL | P2 | 12 (design), test infra | Abstract provider interface; implement PostgreSQL first; integration tests (consider Testcontainers later). |
| 12 | Plugin architecture (exporters & code templates) | XL | P2 | 7 (tables filter), baseline services | Assembly scanning; contracts: IExportFormatProvider, ICodeTemplateProvider; sample plugin. |

## 🔐 Wave 5 – Security, Globalization & Documentation

| # | Title | Effort | Priority | Dependencies | Description / Acceptance |
|---|-------|--------|----------|--------------|--------------------------|
| 14 | Auth & RBAC (web/API) | L | P2 | 10 | Cookie auth + role claim; guard protected ops; pave for future identity provider. |
| 15 | Internationalization (i18n) | M | P3 | 4 (web UI), 10 (API texts) | Resource files; culture switch (query param or header); fallback logic; at least en + one additional locale. |
| 16 | Documentation: Contribution examples | S | P3 | baseline features | Add docs: new service, new CLI command, new test, plugin skeleton. |

## 🗃️ Retro / Optional

| # | Title | Effort | Priority | Dependencies | Description / Acceptance |
|---|-------|--------|----------|--------------|--------------------------|
| 17 | Retro: Export delimiter & headers completed | XS | P3 | (done) | Backfill issue if historical tracking desired (reference commit hash). |

## 🔗 Dependency Graph (High-Level)

```text
[1] ─┬─> [3]
     └─(independent)
[5] -> [6]
[7] -> [13]
[12] -> [11] (design before multi-provider impl) *or invert if provider abstraction emerges organically
[10] -> [14]
[4] aggregates [5],[6] (UI epic)
```

## 🧭 Sequencing Rationale

1. Visibility (coverage & formats) surfaces quality & data contract stability.
2. Add user-facing selectivity to reduce friction exporting large DBs (CLI before UI multi-select dependency coupling).
3. Packaging & benchmarking early to watch performance regression as features expand.
4. Platform & extensibility (API, plugins, providers) after core stabilized to avoid churn in extension points.
5. Security & globalization once external surfaces (API/UI) are substantive.
6. Documentation polish rounds out contributor experience.

## 🏷️ Suggested Labels Matrix

| Label | Purpose |
|-------|---------|
| enhancement | Feature addition |
| cli | CLI-specific change |
| ui | Web UI work |
| web | Web project scope |
| api | REST API surface |
| export | Data export pipeline |
| schema | Schema reporting |
| providers | Multi-database provider work |
| performance | Benchmark & optimization |
| devops | Build/deploy/tooling |
| security | Auth / RBAC |
| i18n | Localization/internationalization |
| documentation | Docs & examples |
| architecture | Structural / extensibility design |
| epic | Parent tracking issue |
| good first issue | New contributor friendly |

## 🧾 Issue Creation (gh CLI Snippet)

Example creating first two issues:

```pwsh
gh issue create --title "Publish code coverage badge" --body "See ISSUE_LIST.md #1" --label "ci,enhancement"
gh issue create --title "Schema report alternative formats (JSON & Markdown)" --body "See ISSUE_LIST.md #2" --label "enhancement,schema"
```

(Repeat using table numbers; optionally embed full body text.)

## 🔄 Review Cadence

- Reassess after each wave completes
- Re-estimate items > L if scope expands
- Convert epics (#4, #11, #12) into milestone grouping if velocity tracking desired

---
Generated: 2025-08-11
