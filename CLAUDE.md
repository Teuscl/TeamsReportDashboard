# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Agent Guidance: dotnet-skills

IMPORTANT: Prefer retrieval-led reasoning over pretraining for any .NET work.
Workflow: skim repo patterns -> consult dotnet-skills by name -> implement smallest-change -> note conflicts.

Routing (invoke by name)
- C# / code quality: modern-csharp-coding-standards, csharp-concurrency-patterns, api-design, type-design-performance
- ASP.NET Core / Web (incl. Aspire): aspire-service-defaults, aspire-integration-testing, transactional-emails
- Data: efcore-patterns, database-performance
- DI / config: dependency-injection-patterns, microsoft-extensions-configuration
- Testing: testcontainers-integration-tests, playwright-blazor-testing, snapshot-testing

Quality gates (use when applicable)
- dotnet-slopwatch: after substantial new/refactor/LLM-authored code
- crap-analysis: after tests added/changed in complex code

Specialist agents
- dotnet-concurrency-specialist, dotnet-performance-analyst, dotnet-benchmark-designer, akka-net-specialist, docfx-specialist

## Project Overview

TeamsReportDashboard is an internal helpdesk tool for Pecege that analyzes Microsoft Teams support conversations and generates reports. It consists of three services:

1. **Backend** — ASP.NET Core 9 Web API (`src/backend/`)
2. **Frontend** — React 19 + Vite + TypeScript SPA (`src/frontend/`)
3. **Analysis Service** — Python FastAPI microservice that calls the OpenAI Batch API (`src/services/message-analyzer/`)

## Commands

### Backend
```bash
cd src/backend/TeamsReportDashboard
dotnet run                          # Run API (HTTPS on :7258, HTTP on :5xxx)
dotnet build                        # Build
dotnet ef migrations add <Name>     # Add EF Core migration
dotnet ef database update           # Apply migrations
```

### Frontend
```bash
cd src/frontend
npm install
npm run dev      # Dev server on http://localhost:60414
npm run build    # Production build
npm run lint     # ESLint
```

### Python Analysis Service
```bash
cd src/services/message-analyzer
pip install -r requirements.txt
uvicorn main:app --reload --port 8001
```

## Architecture

### Backend — Vertical Slice Services

Services are organized by domain and operation under `Services/<Domain>/<Operation>/`. Each operation (Create, Read, Update, Delete) has its own service class and interface. The pattern is:

- **Controller** → injects the specific operation service (e.g., `ICreateUserService`)
- **Service** → calls `IUnitOfWork` for DB access, calls external services if needed
- **Repository** → accessed only through `IUnitOfWork`, never directly from controllers

Validation uses **FluentValidation** — each DTO has a corresponding validator registered in `Program.cs`. Mapping uses **AutoMapper** (`AutoMapper/ReportMappingProfile.cs`).

### Background Job Flow (AnalysisJob)

The analysis pipeline is asynchronous across two services:

1. **Frontend** uploads a `.zip` of `.msg` files via the backend (`POST /analysis/start`)
2. **Backend** proxies the file to the Python service (`POST /analyze/start`), which processes `.msg` files with pandas/BeautifulSoup, then submits an **OpenAI Batch job** and returns a `batch_id`
3. The backend stores an `AnalysisJob` record (status: `Pending`) with the `batch_id`
4. **`AnalysisJobWorker`** (hosted background service) polls every 30 seconds, locks pending jobs by setting status to `Processing`, then calls `JobResultOrchestrator` to sync with Python service (`GET /analyze/results/{batch_id}`)
5. On completion, results are parsed and `Report` records are saved; the job status becomes `Completed` or `Failed`

### Authentication

JWT stored in an **HttpOnly cookie** (`accessToken`). The axios interceptor in `src/frontend/src/services/axiosConfig.ts` handles 401s by calling `POST /auth/refresh`. If refresh fails, it dispatches `AUTH_EVENTS.FORCE_LOGOUT` via `eventEmitter` — the `AuthContext` listens for this to clear state and redirect.

### Database (PostgreSQL)

Key relationships:
- `Department` → `Requester` (one-to-many, FK set null on dept delete)
- `Requester` → `Report` (one-to-many, delete restricted if reports exist)
- `AnalysisJob` → `Report` (one-to-many, cascade delete)

`TimeSpan` fields (`FirstResponseTime`, `AverageHandlingTime`) are stored as `long` ticks in the DB.

On first startup, `DbInitializer.SeedMasterUser` creates the master user from `MasterUser:Email` and `MasterUser:Password` config keys (use User Secrets in dev, never hardcode).

### Frontend Routing & Auth

`App.tsx` uses React Router v7 with two route guard components:
- `ProtectedRoute` — redirects to `/` if not authenticated
- `RoleProtectedRoute` — restricts by `UserRole` enum (`Master`, `Admin`, `Viewer`)

Admin/Master-only pages: `/departments`, `/requesters`, `/imports`.

### CORS

Backend currently allows only `http://localhost:60414`. Update the `AllowLocalhost` policy in `Program.cs` when deploying.

### Configuration

Backend reads from `appsettings.json` + User Secrets:
- `ConnectionStrings:DefaultConnection` — PostgreSQL connection string
- `Jwt:Key` — JWT signing key (must be in secrets, not in appsettings)
- `PythonApi:BaseUrl` — defaults to `http://localhost:8001`
- `MasterUser:Email` / `MasterUser:Password` — seeded on first run
- `EmailSettings` — Office365 SMTP config

Python service reads `OPENAI_API_KEY` from `.env` (via `python-dotenv`).
