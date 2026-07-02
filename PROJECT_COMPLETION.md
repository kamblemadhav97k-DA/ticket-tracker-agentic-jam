# Project Completion Report — Ticket Tracker

A Kanban-style ticket tracker built as a three-tier single-page application, delivered
against the Hackathon Ticketing System requirements specification and reference wireframes.

_Status: **complete and ready for submission.**_

---

## Features implemented

### Authentication & email verification
- Sign up with email + password; emails trimmed and unique case-insensitively.
- Passwords require ≥ 8 characters and are hashed with **Argon2id** (never stored in plain text).
- Login / logout with local credentials via **JWT bearer** tokens (no tokens in URLs).
- Email-verification message sent through a configurable **SMTP** service (supports `relay1.dataart.com`).
- Verification tokens **expire after 24 hours and are single-use**; issuing a new one invalidates earlier unused tokens.
- Unverified accounts cannot use the app (login returns **403**); successful verification leads to the login screen.
- Resend verification available from both the login and verification-result screens.
- All business endpoints require authentication; only sign-up, login, verify, resend, and `/health` are public.

### Teams
- List, create, rename, delete. Name non-empty (trimmed) and unique case-insensitively.
- **Cannot delete** a team that still contains tickets or epics → **409 Conflict** (no cascade); UI disables the button and shows a message.

### Epics
- Per-team CRUD; a team is chosen at creation and cannot be changed afterwards.
- Title required; description optional.
- **Cannot delete** an epic referenced by tickets → **409 Conflict**; UI disables the control and explains why.

### Tickets
- Fields: id, team, type (`bug|feature|fix`), state (5-stage workflow), optional epic, title, body, created-by, created-at, modified-at.
- Backend validates all enum values and references; an epic must belong to the **same team** as its ticket.
- `ModifiedAt` advances only on a real field/state change (unchanged saves do not bump it).
- Changing a ticket's team clears the selected epic (UI) and is enforced server-side.
- Delete requires explicit confirmation and **cascades to comments**.

### Comments
- Add and list comments (chronological, **oldest first**); body required; immutable after creation.
- Adding a comment does **not** change the ticket's `ModifiedAt` or board ordering.

### Kanban board
- Primary screen for one selected team; **exactly five columns** in workflow order with per-column counts.
- Cards show type, title, and epic. Create a ticket and open an existing ticket from the board.
- **Drag-and-drop** between any columns persists the new state immediately via the API; on failure the card reverts and an error is shown.
- Within a column, cards are ordered most-recently-modified first.
- Filtering by type and epic plus case-insensitive title search, combined with **AND** logic; live ticket count.

### Cross-cutting
- Meaningful HTTP status codes and RFC 7807 ProblemDetails (400 validation, 401 auth, 404 not found, 409 conflict).
- Timestamps are ISO-8601 UTC; identifiers are UUIDs.
- Loading / empty / success / error states across the UI.
- Schema created via EF Core migrations on startup; a fresh database holds **no seed data**.

---

## Architecture

**Clean Architecture** with a strict inward dependency flow:

```
API  →  Infrastructure  →  Application  →  Domain
```

| Project | Responsibility |
| --- | --- |
| `TicketTracker.Domain` | Entities (`Team`, `Epic`, `Ticket`, `Comment`) and enums. No external dependencies. |
| `TicketTracker.Application` | DTOs, service interfaces, typed exceptions, enum-canonicalisation. No infrastructure/EF dependencies. |
| `TicketTracker.Infrastructure` | EF Core (PostgreSQL), ASP.NET Core Identity + Argon2id hasher, JWT issuance, SMTP email, email-verification service, business service implementations. |
| `TicketTracker.API` | Controllers, JWT auth pipeline, CORS, Swagger, global exception handler, startup migration. |
| `TicketTracker.Tests` | xUnit unit tests + a full in-process HTTP integration flow. |
| `frontend/` | React + TypeScript SPA (routing, auth context, typed API client, Kanban board). |

Three tiers stay clearly separated: React SPA (presentation) → ASP.NET Core Web API (application/API) → PostgreSQL (persistence). The frontend is served by nginx, which proxies `/api` to the backend.

---

## Technologies used

**Backend**
- .NET 10 / ASP.NET Core 10 Web API (controllers)
- Entity Framework Core 10 + Npgsql (PostgreSQL 16)
- ASP.NET Core Identity + JWT bearer authentication
- Argon2id password hashing (Isopoh.Cryptography.Argon2)
- MailKit (SMTP), Swashbuckle (Swagger/OpenAPI)
- xUnit + EF InMemory + `WebApplicationFactory` for tests

**Frontend**
- React 19 + TypeScript + Vite
- react-router-dom (routing/guards), @dnd-kit/core (drag-and-drop)
- Vitest (unit tests)

**Infrastructure**
- Docker + Docker Compose (db, backend, frontend, Mailpit mail sink)
- nginx (serves SPA, proxies `/api`)

---

## Test results

| Suite | Result |
| --- | --- |
| Backend (`dotnet test`) | **27 passed**, 0 failed, 0 skipped |
| Frontend (`npm run test`) | **4 passed**, 0 failed |

Backend coverage includes Argon2id hashing, JWT issuance/validation, the email-verification token
lifecycle (single-use / reissue-invalidates / 24h expiry), team/epic/ticket/comment business rules,
and an end-to-end HTTP flow (register → verify → login → team/epic/ticket/comment CRUD → 409 conflicts → cascade delete).

---

## Build status

| Build | Result |
| --- | --- |
| Backend — `dotnet build -c Release` | ✅ Build succeeded, **0 warnings / 0 errors** |
| Frontend — `npm run build` (`tsc -b && vite build`) | ✅ Type-checks and bundles cleanly |
| Formatting — `dotnet format --verify-no-changes` | ✅ Clean |
| TODO / FIXME / HACK in source | ✅ None |

---

## Docker status

`docker compose build` — ✅ **succeeds**; both `ticketingsystem-backend` and `ticketingsystem-frontend` images build.

From a clean checkout the full stack starts with:

```bash
docker compose up --build
```

- SPA: http://localhost:8080
- API health: http://localhost:8080/health
- Mail inbox (Mailpit): http://localhost:8025

No host-installed frontend, backend, or database runtime is required beyond Docker Compose. The
database schema is applied automatically via EF Core migrations on startup (no seed data).

---

## Known optional improvements

These are **not required** by the specification (§14 stretch features and minor polish); the project is complete without them.

- **Stretch features:** password reset, edit/delete own comments, ticket activity history, virtualized rendering for very large boards.
- **Board re-sort after drag:** a moved card re-sorts to the top of its new column only after the next board load (server ordering is correct; a client-side re-sort would make it instant).
- **Ticket ID display:** UUIDs are shown as a short `#xxxxxxxx` prefix rather than a friendly `TCK-####` sequence.
- **UX polish:** custom confirmation dialogs instead of the browser `confirm()`, and improved board responsiveness on narrow viewports (desktop is the target per spec).
- **Frontend test depth:** unit tests cover helpers; component/drag-drop tests could be added.
