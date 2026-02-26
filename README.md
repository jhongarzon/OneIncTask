# OneIncTask - Long-Running Job Processing Application

A production-ready SPA that simulates heavy long-running background processing using .NET 10, React, SignalR, and PostgreSQL.

## Architecture

- **Backend**: .NET 10 Clean Architecture + custom CQRS (no MediatR)
- **Frontend**: React 18 + TypeScript + Vite + shadcn/ui + TanStack Query
- **Real-time**: SignalR (strongly-typed hub)
- **Background Jobs**: BackgroundService + Channel<T>
- **Auth**: Nginx basic auth + JWT (for SignalR WebSocket)
- **Database**: PostgreSQL 16 with EF Core 10

## Quick Start

### Docker Compose (recommended)

```bash
docker compose up --build
```

Open http://localhost and login with `admin` / `password123`.

### Local Development

1. Start PostgreSQL:
```bash
docker compose up db -d
```

2. Run backend:
```bash
cd src/backend
dotnet run --project src/OneIncTask.Api
```

3. Run frontend:
```bash
cd src/frontend
npm install
npm run dev
```

## How It Works

1. Enter text in the input field and click "Process"
2. The backend computes unique character counts (sorted by ASCII) + Base64
3. Characters stream back one-by-one with random 1-5s delays via SignalR
4. Cancel anytime - the job stops immediately
5. View history of all past jobs

**Example**: Input `Hello, World!` produces ` 1!1,1H1W1d1e1l3o2r1/SGVsbG8sIFdvcmxkIQ==`

## Documentation

For detailed architecture, API reference, data flow diagrams, infrastructure setup, and deployment guides, see the [Technical Reference](docs/TECHNICAL_REFERENCE.md).

## Running Tests

### Backend
```bash
cd src/backend
dotnet test
```

### Frontend
```bash
cd src/frontend
npm test
```
