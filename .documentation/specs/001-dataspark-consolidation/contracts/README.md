# API Contracts: DataSpark Platform

**Branch**: `001-dataspark-consolidation` | **Date**: 2026-03-30

This directory defines public interface contracts for the DataSpark platform.

## Contract Types

| File | Interface Type | Consumers |
|------|---------------|-----------|
| [web-api.md](web-api.md) | RESTful HTTP API | External clients, scripts, dashboards |
| [cli.md](cli.md) | Command-line interface | Developers, CI/CD pipelines |

## Authentication

All REST API endpoints require API key authentication via the `X-Api-Key` header.
Web UI routes use standard cookie-based session + CSRF tokens.
CLI operates locally with no authentication.
