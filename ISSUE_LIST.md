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

## 🧪 Wave 2 – User Experience & Selectivity

| # | Title | Effort | Priority | Dependencies | Description / Acceptance |
|---|-------|--------|----------|--------------|--------------------------|
| 4 | Enhanced web UI (EPIC) | XXL | P1 | 5,6 partial | Parent tracking issue (progress, checklists, design notes). |
| 5 | Web UI: Drag & drop SQLite upload | M | P1 | none | Drop zone + validation (extension, size); shows new DB in list; tests for controller & service. |
| 6 | Web UI: Multi-table export selection | M | P1 | 5 | UI checkboxes + select all/none; pass selected tables to backend (placeholder service if logic pending). |
| 7 | Per-table selection for CLI export & generation | S | P1 | none | `--tables Users,Orders` filter for export & generate commands; update tests & README. |

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
