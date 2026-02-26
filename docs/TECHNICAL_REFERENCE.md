# OneIncTask - Technical Reference

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Backend](#backend)
  - [Tech Stack](#backend-tech-stack)
  - [Clean Architecture Layers](#clean-architecture-layers)
  - [API Endpoints](#api-endpoints)
  - [SignalR Hub](#signalr-hub)
  - [CQRS Implementation](#cqrs-implementation)
  - [Domain Model](#domain-model)
  - [Background Job Processing](#background-job-processing)
  - [String Processing Algorithm](#string-processing-algorithm)
  - [Middleware](#middleware)
  - [Configuration](#configuration)
- [Frontend](#frontend)
  - [Tech Stack](#frontend-tech-stack)
  - [Component Hierarchy](#component-hierarchy)
  - [Custom Hooks](#custom-hooks)
  - [API Client](#api-client)
  - [SignalR Client](#signalr-client)
  - [Authentication Flow](#authentication-flow)
- [Infrastructure](#infrastructure)
  - [Docker Setup](#docker-setup)
  - [Nginx Reverse Proxy](#nginx-reverse-proxy)
  - [Docker Compose](#docker-compose)
  - [Azure Deployment](#azure-deployment)
  - [Environment Variables](#environment-variables)
- [Testing](#testing)
  - [Backend Tests](#backend-tests)
  - [Frontend Tests](#frontend-tests)
  - [End-to-End Tests](#end-to-end-tests)
- [Data Flow](#data-flow)

---

## Overview

OneIncTask is a production-ready Single Page Application that demonstrates long-running background job processing. It processes text input by computing unique character counts and encoding them in Base64, streaming results back character-by-character through real-time WebSocket connections.

---

## Architecture

```
                    Internet
                       |
                    [Nginx]
                   /   |   \
                  /    |    \
          Basic Auth  JWT   WebSocket
              |        |       |
         [Frontend]  [API]  [SignalR Hub]
                       |       |
                  [Background Service]
                       |
                  [PostgreSQL]
```

### Key Patterns

| Pattern | Implementation |
|---------|---------------|
| Clean Architecture | Domain, Application, Infrastructure, API layers |
| CQRS | Custom dispatcher with FluentValidation (no MediatR) |
| Real-time | SignalR with strongly-typed hub and group-based messaging |
| Background Processing | `BackgroundService` + `Channel<T>` bounded queue |
| Authentication | Nginx basic auth (entry) + JWT Bearer (API/SignalR) |
| Repository Pattern | Separate read/write repositories |

---

## Backend

### Backend Tech Stack

| Package | Version | Purpose |
|---------|---------|---------|
| .NET | 10.0 | Runtime |
| ASP.NET Core | 10.0 | Web framework |
| Entity Framework Core | 10.0 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 | PostgreSQL provider |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.3 | JWT authentication |
| FluentValidation | 12.1.1 | Input validation |
| Serilog.AspNetCore | 10.0.0 | Structured logging |
| SignalR | Built-in | Real-time communication |

### Clean Architecture Layers

```
src/backend/src/
├── OneIncTask.Domain/           # Entities, enums, abstractions, repository interfaces
├── OneIncTask.Application/      # CQRS commands/queries, services, validators, DI
├── OneIncTask.Infrastructure/   # EF Core, repositories, background services, middleware
└── OneIncTask.Api/              # Controllers, SignalR hubs, JWT config, Program.cs
```

**Dependency flow:** API → Application → Domain ← Infrastructure

### API Endpoints

#### Authentication

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/auth/token` | Nginx basic auth | Issues JWT token. Reads `X-Auth-User` header set by nginx. |
| GET | `/health` | None | Health check endpoint |

**Token response:**
```json
{
  "token": "eyJhbG...",
  "expiresIn": 28800
}
```

#### Jobs (all require JWT Bearer)

| Method | Route | Description | Response |
|--------|-------|-------------|----------|
| POST | `/api/jobs` | Start a new job | `202` with `{ jobId }` |
| DELETE | `/api/jobs/{id}` | Cancel a running job | `200` with `true` |
| GET | `/api/jobs/{id}/status` | Get job status | `200` with status details |
| GET | `/api/jobs/history` | Get user's job history | `200` with job list |

**Start Job request:**
```json
{
  "inputText": "Hello, World!"
}
```
- Max length: 10,000 characters
- Returns `409 Conflict` if user already has an active job

**Job Status response:**
```json
{
  "jobId": "guid",
  "status": "Running",
  "currentResult": "partial result...",
  "processedCharacters": 5,
  "totalCharacters": 42,
  "createdAt": "2026-02-26T00:00:00Z",
  "completedAt": null,
  "errorMessage": null
}
```

### SignalR Hub

**Endpoint:** `/hub/job-progress`
**Auth:** JWT via query string `?access_token=<token>`

#### Client Events (server → client)

| Event | Parameters | Description |
|-------|-----------|-------------|
| `JobStarted` | `jobId`, `totalCharacters` | Job begins processing |
| `ReceiveCharacter` | `jobId`, `character`, `currentIndex`, `totalCount` | Single character processed |
| `JobCompleted` | `jobId`, `fullResult` | Job finished successfully |
| `JobCancelled` | `jobId` | Job was cancelled |
| `JobFailed` | `jobId`, `error` | Job failed with error |

**User Grouping:** Each user is placed in group `user-{userId}` on connection. All notifications are sent to the user's group.

### CQRS Implementation

Custom lightweight CQRS without MediatR, using reflection-based dispatchers.

**Commands:**

| Command | Input | Output |
|---------|-------|--------|
| `StartJob` | `UserId`, `InputText` | `ApiResponse<StartJobResponse>` |
| `CancelJob` | `JobId`, `UserId` | `ApiResponse<bool>` |

**Queries:**

| Query | Input | Output |
|-------|-------|--------|
| `GetJobStatus` | `JobId`, `UserId` | `JobStatusResponse` |
| `GetJobHistory` | `UserId` | `IEnumerable<JobHistoryItem>` |

**Flow:**
```
Controller → CommandDispatcher → FluentValidation → CommandHandler → Repository
Controller → QueryDispatcher → QueryHandler → Repository
```

### Domain Model

**Job Entity:**

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `Guid` | PK, auto-generated |
| UserId | `string` | Required, max 256 chars |
| InputText | `string` | Required, max 10,000 chars |
| ExpectedResult | `string?` | Max 50,000 chars |
| CurrentResult | `string` | Max 50,000 chars |
| Status | `JobStatus` | Stored as string, max 20 chars |
| TotalCharacters | `int` | |
| ProcessedCharacters | `int` | |
| CreatedAt | `DateTime` | UTC, auto-set |
| CompletedAt | `DateTime?` | |
| ErrorMessage | `string?` | Max 2,000 chars |

**JobStatus enum:** `Pending (0)` → `Running (1)` → `Completed (2)` | `Cancelled (3)` | `Failed (4)`

**Database indexes:**
- Composite: `(UserId, Status)` — for active job lookups
- Single: `CreatedAt` — for history ordering

### Background Job Processing

```
[StartJob Handler] → Channel<JobProcessingRequest> → [JobProcessorBackgroundService] → [JobProcessingService]
                          capacity: 100                     BackgroundService              per-character processing
                          backpressure: Wait                                               with SignalR notifications
```

**JobProcessorBackgroundService:**
- Continuously reads from `Channel<JobProcessingRequest>`
- Creates a `CancellationTokenSource` per job (registered in `JobCancellationRegistry`)
- Delegates processing to `IJobProcessingService`
- Handles cancellation and failure, updating job status accordingly

**JobProcessingService.ProcessJobAsync:**
1. Compute expected result via `StringProcessingService`
2. Update job status to `Running`
3. Send `JobStarted` notification
4. For each character in result:
   - Check cancellation token
   - Send `ReceiveCharacter` notification
   - Update DB every 10 characters
   - Random delay of 1–5 seconds (simulates heavy work)
5. Update job status to `Completed`
6. Send `JobCompleted` notification

**JobCancellationRegistry:**
- Singleton with `ConcurrentDictionary<Guid, CancellationTokenSource>`
- `Register(jobId)` — creates and stores CTS
- `TryCancel(jobId)` — triggers cancellation
- `Unregister(jobId)` — cleanup after job ends

### String Processing Algorithm

**`StringProcessingService.BuildProcessedString(input)`**

1. Count occurrences of each character (case-sensitive)
2. Sort characters by ASCII value
3. Format: `char1count1char2count2.../Base64(input)`

**Example:**
```
Input:  "Hello, World!"
Step 1: { ' ':1, '!':1, ',':1, 'H':1, 'W':1, 'd':1, 'e':1, 'l':3, 'o':2, 'r':1 }
Step 2: Sort by ASCII: space, !, comma, H, W, d, e, l, o, r
Step 3: " 1!1,1H1W1d1e1l3o2r1"
Base64: SGVsbG8sIFdvcmxkIQ==
Output: " 1!1,1H1W1d1e1l3o2r1/SGVsbG8sIFdvcmxkIQ=="
```

### Middleware

**GlobalExceptionHandlerMiddleware** — catches all unhandled exceptions:

| Exception Type | HTTP Status | Description |
|----------------|-------------|-------------|
| `OperationCanceledException` | 499 | Client closed request |
| `ValidationException` | 400 | Input validation failed (with details) |
| `UnauthorizedAccessException` | 403 | Forbidden |
| `InvalidOperationException` | 400 | Bad request |
| `KeyNotFoundException` | 404 | Resource not found |
| `ArgumentException` | 400 | Invalid argument |
| Other | 500 | Internal server error |

### Configuration

**JWT Settings (`Jwt` section):**

| Key | Default | Description |
|-----|---------|-------------|
| `Secret` | — | Symmetric signing key (32+ chars) |
| `Issuer` | `OneIncTask` | Token issuer claim |
| `Audience` | `OneIncTask` | Token audience claim |
| `ExpirationInMinutes` | `480` | Token lifetime (8 hours) |

**Token validation:** Issuer, audience, lifetime, and signing key are all validated. `ClockSkew` is set to zero.

**SignalR token handling:** JWT tokens for WebSocket connections are read from the `access_token` query string parameter for paths starting with `/hub`.

---

## Frontend

### Frontend Tech Stack

| Package | Version | Purpose |
|---------|---------|---------|
| React | 18.3.1 | UI framework |
| TypeScript | 5.7.2 | Type safety |
| Vite | 6.3.5 | Build tool & dev server |
| @microsoft/signalr | 8.0.0 | WebSocket client |
| @tanstack/react-query | 5.60.0 | Data fetching & caching |
| Tailwind CSS | 3.4.17 | Utility-first CSS |
| class-variance-authority | 0.7.1 | Component variants |
| lucide-react | 0.460.0 | Icons |
| Vitest | 2.1.0 | Test framework |
| @testing-library/react | 16.1.0 | Component testing |

### Component Hierarchy

```
QueryProvider (TanStack Query)
└── AuthProvider
    └── App
        ├── Header
        ├── Navigation (tab selector: "processing" | "history")
        └── Main Content
            ├── ProcessingPage
            │   ├── ProcessingForm (input field + submit/cancel buttons)
            │   ├── ProgressBar (visual progress + status)
            │   └── ResultDisplay (streamed result output)
            └── HistoryPage (table of past jobs)
```

**UI Components (shadcn/ui):**
- `Button` — with variants: default, destructive, outline, secondary, ghost, link
- `Badge` — with variants: default, secondary, destructive, outline
- `Card` — container with header, content, footer sections

### Custom Hooks

| Hook | Purpose | Key Returns |
|------|---------|-------------|
| `useAuth()` | Auth context | `token`, `isLoading`, `error`, `refreshToken()` |
| `useJobProgress(token, onAuthError?)` | Real-time job tracking via SignalR | `result`, `processedCharacters`, `totalCharacters`, `isProcessing`, `isCompleted`, `isCancelled`, `error`, `setJobId()`, `resetState()` |
| `useStartJob(onSuccess)` | Start job mutation | TanStack `useMutation` result |
| `useCancelJob(onSuccess?)` | Cancel job mutation | TanStack `useMutation` result |
| `useJobHistory()` | Fetch job history | `data`, `isLoading`, `error` (auto-refetches every 10s) |

### API Client

**File:** `src/lib/api-client.ts`

| Function | Method | Endpoint | Auth |
|----------|--------|----------|------|
| `getAuthToken()` | GET | `/api/auth/token` | Basic auth (browser-cached) |
| `startJob(inputText)` | POST | `/api/jobs` | Bearer JWT |
| `cancelJob(jobId)` | DELETE | `/api/jobs/{jobId}` | Bearer JWT |
| `getJobStatus(jobId)` | GET | `/api/jobs/{jobId}/status` | Bearer JWT |
| `getJobHistory()` | GET | `/api/jobs/history` | Bearer JWT |

**401 handling:** All API responses are checked for 401 status. On 401, the `onUnauthorized` callback triggers a token refresh in `AuthProvider`.

### SignalR Client

**File:** `src/lib/signalr-client.ts`

- **URL:** `/hub/job-progress`
- **Auth:** `accessTokenFactory` passes JWT as query string
- **Reconnect:** Automatic with backoff `[0, 2000, 5000, 10000, 30000]ms`
- **Token change detection:** If the token changes (after refresh), the old connection is stopped and a new one is created

### Authentication Flow

```
1. User navigates to app
2. Nginx returns 401 → browser prompts for basic auth (admin/password123)
3. Page loads → AuthProvider checks localStorage for 'jwt_token'
4. If missing: GET /api/auth/token (browser sends cached basic auth)
5. Nginx validates basic auth, sets X-Auth-User header
6. Backend creates JWT signed with secret → returns token
7. Frontend stores token in localStorage
8. All API calls include Authorization: Bearer <token>
9. SignalR passes token via ?access_token query string
10. On 401 from API or SignalR negotiate:
    - Clear stale token from localStorage
    - Fetch new token automatically
    - Reconnect SignalR with fresh token
```

---

## Infrastructure

### Docker Setup

**Backend image** (`src/backend/Dockerfile`):
- Build: `mcr.microsoft.com/dotnet/sdk:10.0` → publish Release
- Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0` (non-root user)
- Port: `8080`

**Frontend image** (`src/frontend/Dockerfile`):
- Build: `node:20-alpine` → `npm ci` + `npm run build` (Vite)
- Runtime: `nginx:alpine` serving static files
- Port: `80`

**Nginx image** (`nginx/Dockerfile`):
- Base: `nginx:alpine`
- Template: `envsubst` replaces `${VAR}` placeholders at startup
- Port: `80`

### Nginx Reverse Proxy

| Location | Auth | Proxies To | Notes |
|----------|------|-----------|-------|
| `/health` | None | `api:8080/health` | Health check |
| `/api/auth/token` | Basic auth | `api:8080` | Sets `X-Auth-User` header |
| `/api/` | None (JWT on API) | `api:8080` | Forwards `Authorization` header |
| `/hub/` | None (JWT on API) | `api:8080` | WebSocket upgrade, 86400s timeout |
| `/assets/` | None | `frontend:80` | Static assets with buffering |
| `/` | Basic auth | `frontend:80` | SPA entry point |

**Basic auth credentials:** `admin` / `password123` (stored in `.htpasswd`)

### Docker Compose

| Service | Image | Internal Port | External Port | Depends On |
|---------|-------|--------------|---------------|------------|
| `db` | `postgres:16-alpine` | 5432 | 5432 | — |
| `api` | Built from `src/backend/` | 8080 | — | db (healthy) |
| `frontend` | Built from `src/frontend/` | 80 | — | api |
| `nginx` | Built from `nginx/` | 80 | **8080** | api (healthy), frontend |

**Volume:** `pgdata` for PostgreSQL data persistence.

**Access:** `http://localhost:8080` → nginx → app

### Azure Deployment

**Script:** `deploy-azure.sh`

**Resources created:**

| # | Resource | SKU/Tier | Details |
|---|----------|----------|---------|
| 1 | Resource Group | — | `oneinctask-rg` |
| 2 | Container Registry (ACR) | Basic | Admin enabled, globally unique name |
| 3 | PostgreSQL Flexible Server | Standard_B1ms (Burstable) | v16, 32GB storage, SSL required |
| 4 | Container Apps Environment | — | Shared environment for all apps |
| 5 | API Container App | 0.5 CPU / 1Gi | Internal ingress, 1–3 replicas |
| 6 | Frontend Container App | 0.25 CPU / 0.5Gi | Internal ingress, 1 replica |
| 7 | Nginx Container App | 0.25 CPU / 0.5Gi | **External** ingress, 1 replica |

**Notes:**
- JWT secret is randomly generated per deployment (`openssl rand -base64 48`)
- DB password is randomly generated per deployment
- Only nginx has external (public) ingress — API and frontend are internal
- Images are built locally and pushed to ACR

### Environment Variables

#### Backend

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=db;Port=5432;Database=oneincdb;...` |
| `Jwt__Secret` | JWT signing key (32+ chars) | Auto-generated in Azure |
| `Jwt__Issuer` | JWT issuer claim | `OneIncTask` |
| `Jwt__Audience` | JWT audience claim | `OneIncTask` |
| `Jwt__ExpirationInMinutes` | Token lifetime | `480` |

#### Nginx

| Variable | Description | Docker Compose | Azure |
|----------|-------------|---------------|-------|
| `API_UPSTREAM` | API service address | `api:8080` | `oneinc-api:80` |
| `FRONTEND_UPSTREAM` | Frontend service address | `frontend:80` | `oneinc-frontend:80` |
| `API_HOST` | Host header for API proxy | `api` | `oneinc-api` |
| `FRONTEND_HOST` | Host header for frontend proxy | `frontend` | `oneinc-frontend` |

---

## Testing

### Backend Tests

**Framework:** xUnit 2.9.3 | **Mocking:** Moq 4.20.72 | **Assertions:** FluentAssertions 8.8.0

```bash
cd src/backend
dotnet test
```

| Test File | Covers | Tests |
|-----------|--------|-------|
| `StringProcessingServiceTests.cs` | Character counting, ASCII sorting, Base64 encoding | 8 tests |
| `StartJobHandlerTests.cs` | Job creation, conflict detection, input validation | 5 tests |
| `CancelJobHandlerTests.cs` | Job cancellation, ownership checks, status validation | 4 tests |
| `JobProcessingServiceTests.cs` | End-to-end processing, cancellation, error handling | 3 tests |
| `JobEntityTests.cs` | Entity creation, status transitions | Domain tests |

### Frontend Tests

**Framework:** Vitest 2.1.0 | **Component Testing:** Testing Library | **DOM:** jsdom

```bash
cd src/frontend
npm test            # run once
npm run test:watch  # watch mode
```

| Test File | Component | Tests |
|-----------|-----------|-------|
| `Badge.test.tsx` | Badge | Rendering, variants | 5 |
| `Button.test.tsx` | Button | Rendering, variants, disabled states | 6 |
| `ProcessingForm.test.tsx` | ProcessingForm | Submission, validation, button states | 8 |
| `ProgressBar.test.tsx` | ProgressBar | Progress display, status indicator | 9 |
| `ResultDisplay.test.tsx` | ResultDisplay | Result display, error display | 6 |
| `HistoryPage.test.tsx` | HistoryPage | History rendering, status badges | 5 |
| `useProcessJob.test.ts` | useStartJob, useCancelJob | API calls, error handling | 3 |

**Total: 42 frontend tests**

### End-to-End Tests

**Script:** `test_e2e.sh` (bash + curl)

```bash
chmod +x test_e2e.sh
./test_e2e.sh
```

Tests: health check → auth token → frontend assets → start job → poll until complete → verify result → check history → start and cancel job → verify cancellation → final history check.

---

## Data Flow

### Start Job

```
User clicks "Process"
  → ProcessingForm.onSubmit(inputText)
  → useStartJob.mutate(inputText)
  → POST /api/jobs { inputText }
  → [nginx /api/] forwards with Authorization header
  → JobsController.StartJob()
  → CommandDispatcher → StartJob.Validator → StartJob.Handler
  → Handler:
      1. Check no active job for user (409 if exists)
      2. Compute expected result via StringProcessingService
      3. Create Job entity (Pending) → save to DB
      4. Enqueue JobProcessingRequest to Channel<T>
  → Response: 202 { jobId }
  → useStartJob.onSuccess → progress.setJobId(jobId)

Background:
  JobProcessorBackgroundService dequeues request
  → JobProcessingService.ProcessJobAsync()
  → Update job: Pending → Running
  → SignalR: JobStarted(jobId, totalCharacters)
  → For each character:
      SignalR: ReceiveCharacter(jobId, char, index, total)
      DB update every 10 chars
      Random 1-5s delay
  → Update job: Running → Completed
  → SignalR: JobCompleted(jobId, fullResult)

Frontend:
  useJobProgress listens to SignalR events
  → Updates state on each ReceiveCharacter
  → ProgressBar and ResultDisplay re-render in real-time
```

### Cancel Job

```
User clicks "Cancel"
  → useCancelJob.mutate(jobId)
  → DELETE /api/jobs/{jobId}
  → CancelJob.Handler:
      1. Verify job exists and belongs to user
      2. Verify status is Pending or Running
      3. JobCancellationRegistry.TryCancel(jobId) → triggers CancellationToken
      4. Update job status → Cancelled
  → Background service catches OperationCanceledException
  → SignalR: JobCancelled(jobId)
  → Frontend: UI shows "Cancelled" status
```

### Token Refresh (on 401)

```
API call or SignalR negotiate returns 401
  → onUnauthorized callback (API) or catch in conn.start() (SignalR)
  → AuthProvider.refreshToken()
      1. Remove 'jwt_token' from localStorage
      2. Set token state to null
      3. Stop existing SignalR connection
  → AuthProvider detects null token → fetchToken()
      1. GET /api/auth/token (browser sends cached basic auth)
      2. Store new token in localStorage
      3. Set token state
  → useJobProgress detects new token
      1. Creates new SignalR connection with fresh token
      2. Connects successfully
```
