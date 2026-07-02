# Ticket Tracker

A Kanban-style ticket tracker built as a three-tier SPA:

- **Backend** — ASP.NET Core 10 Web API, Clean Architecture, EF Core + PostgreSQL, ASP.NET Core Identity + JWT bearer auth.
- **Frontend** — React + TypeScript + Vite (served by nginx in Docker).
- **Database** — PostgreSQL 16.

> **Status: Complete — ready for submission.** Auth + email verification, Teams/Epics/Tickets/Comments CRUD, and the React SPA (auth screens, Kanban board with drag-and-drop, ticket editor with comments, team & epic management) are all implemented. Verified: **27 backend tests + 4 frontend tests pass**, backend Release build and frontend production build are clean (0 warnings), and **`docker compose build` succeeds**. See [PROJECT_COMPLETION.md](PROJECT_COMPLETION.md) for the full report.

## Screens (React SPA)

- **Login / Sign-up / Email verification** (with resend for unverified/expired links)
- **Kanban board** — team selector, type/epic filters + title search (AND), five state columns, drag-and-drop that persists via the API and reverts on failure, ticket count
- **Ticket create / edit / details** — all fields, team-change clears the epic, comments (oldest first), delete with confirmation
- **Teams management** — table with ticket/epic counts, create, rename, delete (disabled while referenced)
- **Epics management** — team selector, table with ticket counts, create/edit, delete (disabled while referenced)

## API endpoints

All endpoints require a JWT bearer token except `POST /api/auth/register|login|verify-email|resend-verification` and `GET /health`.

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/api/auth/register` | Sign up; sends verification email |
| POST | `/api/auth/verify-email` | Verify email with a single-use token |
| POST | `/api/auth/resend-verification` | Re-issue a verification email |
| POST | `/api/auth/login` | Log in → JWT (403 if unverified) |
| POST | `/api/auth/logout` | Log out (client discards token) |
| GET/POST | `/api/teams` | List / create teams |
| GET/PUT/DELETE | `/api/teams/{id}` | Get / rename / delete a team (409 if it has epics or tickets) |
| GET/POST | `/api/epics` | List (`?teamId=`) / create epics |
| GET/PUT/DELETE | `/api/epics/{id}` | Get / edit / delete an epic (409 if referenced by tickets) |
| GET | `/api/tickets?teamId=&type=&epicId=&search=` | Board query (AND filters, newest-modified first) |
| POST | `/api/tickets` | Create a ticket |
| GET/PUT/DELETE | `/api/tickets/{id}` | Get / edit / delete a ticket (deletes its comments) |
| PATCH | `/api/tickets/{id}/state` | Persist a state change (drag-and-drop) |
| GET/POST | `/api/tickets/{ticketId}/comments` | List (oldest first) / add a comment |

## Solution layout

```
TicketTracker.sln
├─ src/
│  ├─ TicketTracker.API             Web API: controllers, auth pipeline, Swagger, global error handler
│  ├─ TicketTracker.Application      DTOs, service interfaces, exceptions (no infrastructure deps)
│  ├─ TicketTracker.Domain           Entities (Team, Epic, Ticket, Comment) + enums
│  └─ TicketTracker.Infrastructure   EF Core (PostgreSQL), Identity/Argon2id, JWT, SMTP, services
├─ tests/TicketTracker.Tests         xUnit: unit rules + full HTTP CRUD integration flow
├─ frontend/                         React + TypeScript + Vite SPA (nginx in Docker)
├─ docker-compose.yml                db + backend + frontend + mail (Mailpit)
├─ .env.example                      Copy to .env and adjust
└─ README.md
```

Dependency direction (Clean Architecture): **API → Infrastructure → Application → Domain**.

## Prerequisites

- **Docker path (recommended):** Docker Desktop / Docker Engine with Compose. Nothing else required.
- **Local dev path:** .NET SDK 10, Node.js 20+, and a PostgreSQL 16 instance.

## Run with Docker Compose

From the repository root:

```bash
cp .env.example .env      # optional; sensible defaults are baked in
docker compose up --build
```

- SPA: <http://localhost:8080> (change with `WEB_PORT`)
- API health: <http://localhost:8080/health> (proxied to the backend)
- Mail inbox (Mailpit): <http://localhost:8025> — verification emails land here in local runs

The database schema is created automatically on startup (EF Core migrations); a fresh
database contains no seed data. For real email delivery, set `SMTP_HOST=relay1.dataart.com`
(and credentials) in `.env`.

## Run locally (without Docker)

Backend:

```bash
dotnet restore
dotnet build
dotnet run --project src/TicketTracker.API
```

- Swagger UI (Development): <http://localhost:5xxx/swagger>
- Health: <http://localhost:5xxx/health>

Configure `ConnectionStrings:DefaultConnection` and `Jwt:Secret` via
`src/TicketTracker.API/appsettings.Development.json` or environment variables.

Frontend:

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173 (proxies /api to the backend)
```

## Configuration

| Setting | appsettings key | Env var (compose) |
| --- | --- | --- |
| DB connection | `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| JWT issuer | `Jwt:Issuer` | `JWT_ISSUER` |
| JWT audience | `Jwt:Audience` | `JWT_AUDIENCE` |
| JWT secret | `Jwt:Secret` | `JWT_SECRET` |
| CORS origins | `Cors:AllowedOrigins` | — |
| SMTP host/port | `Smtp:Host` / `Smtp:Port` | `SMTP_HOST` / `SMTP_PORT` |
| SMTP credentials | `Smtp:Username` / `Smtp:Password` | `SMTP_USERNAME` / `SMTP_PASSWORD` |
| SPA base URL (verify links) | `App:ClientUrl` | `APP_CLIENT_URL` |

No secrets are committed. `appsettings.json` ships with empty secret values; real
values come from `appsettings.Development.json` (local dev only) or environment.

## Testing

```bash
dotnet test                 # 27 backend tests (unit + full HTTP CRUD flow)
cd frontend && npm run test # 4 frontend tests (format/board helpers)
```
