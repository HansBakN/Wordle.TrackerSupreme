# Wordle Tracker Supreme — Claude Code Project Guide

## Purpose

A full-stack Wordle game tracker: players solve the daily word puzzle, scores are persisted, and a leaderboard ranks everyone by win-rate / average guesses. The repo contains a .NET 10 REST API, a SvelteKit frontend, Docker Compose orchestration, and a Playwright E2E suite.

## Stack

| Layer      | Technology                                                                            |
| ---------- | ------------------------------------------------------------------------------------- |
| Backend    | .NET 10, ASP.NET Core, Entity Framework Core, PostgreSQL 16                           |
| Frontend   | SvelteKit 2.49, TypeScript 5.9, Tailwind CSS 4, Vite 7                                |
| Auth       | JWT HS256 (≥32-byte secret), ASP.NET Identity `PasswordHasher<Player>`                |
| Tests      | xUnit (backend), Vitest (frontend unit), Playwright (UI + E2E)                        |
| CI         | GitHub Actions — `.github/workflows/e2e.yml` runs the full E2E suite on every push/PR |
| Containers | Docker Compose v2 — profiles: `app`, `migrate`, `seed`, `tests`, `tests-all`, `proxy` |

## Quick commands

```bash
# Start full stack (API :8080, web :3000)
docker compose --env-file .env.local --profile app up -d --build

# Apply DB migrations
docker compose --env-file .env.local --profile migrate up --build migrator

# Seed deterministic dev data
docker compose --env-file .env.local --profile seed run --rm seeder

# Backend tests (no local SDK needed)
docker compose --profile tests run --rm tests-backend

# Frontend unit + UI tests
docker compose --profile tests run --rm tests-frontend

# Full E2E suite (requires Node 20+, Docker)
./scripts/e2e.sh

# Frontend formatter / linter (must be clean before handing off)
cd src/Wordle.TrackerSupreme.Web && npm run format && npm run lint
```

## Architecture

```
src/
  Wordle.TrackerSupreme.BackEnd/
    Wordle.TrackerSupreme.Api/          Controllers, DTOs, Program.cs, JWT middleware
    Wordle.TrackerSupreme.Application/  Business logic services (GameplayService, PlayerStatisticsService, AdminService)
    Wordle.TrackerSupreme.Domain/       Models, interfaces, repository contracts, domain services
    Wordle.TrackerSupreme.Infrastructure/ EF Core DbContext, concrete repositories, migrations glue
    Wordle.TrackerSupreme.Migrations/   EF migrations project (never hand-edit existing migrations)
    Wordle.TrackerSupreme.Seeder/       Deterministic test-data generator
    Wordle.TrackerSupreme.Tests/        xUnit tests — unit + integration
  Wordle.TrackerSupreme.Web/
    src/lib/api-client/                 Generated OpenAPI client — never hand-edit (regenerate from openapi.json)
    src/lib/auth/                       Auth store (Svelte stores)
    src/lib/game/                       Game API helpers (api.ts), types
    src/lib/stats/                      Filter helpers
    src/routes/                         SvelteKit pages: home (game), signin, signup, leaderboard, stats, admin
    tests/e2e/                          Playwright E2E specs
    tests/ui/                           Playwright UI-only specs
scripts/
  e2e.sh                                E2E orchestration script
```

## Domain rules (do not break)

- One daily puzzle per calendar date, keyed by `PuzzleDate`.
- Default: 5-letter words, 6 max guesses, solutions reveal at **12:00 local server time** (`GameClock.GetRevealInstant`).
- Attempts after 12:00 are "practice" — tracked separately, do not affect streaks or leaderboard.
- Hard mode: guesses must keep all confirmed letters in their exact positions and include all revealed letters.
- Guesses are normalised to uppercase before evaluation.

## Known open issues

| #                                                                  | Summary                                        | Root cause                                                                                                                                                                                                                                                                                                                  |
| ------------------------------------------------------------------ | ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [#6](https://github.com/HansBakN/Wordle.TrackerSupreme/issues/6)   | Average guesses always 0                       | `GetPlayersWithAttempts` / `GetPlayerWithAttempts` don't include `Guesses` nav-prop; `GuessCount` returns 0 instead of the real count. Fix: add `.ThenInclude(a => a.Guesses)` to both repository methods.                                                                                                                  |
| [#7](https://github.com/HansBakN/Wordle.TrackerSupreme/issues/7)   | Double submits possible                        | On-screen Enter button `disabled` doesn't include `\|\| submitting`, so the button stays visually active during in-flight submission. A physical-Enter + on-screen-Enter combo can fire two events before the `submitting` guard is visible. Fix: add `\|\| submitting` to all interactive game button `disabled` bindings. |
| [#10](https://github.com/HansBakN/Wordle.TrackerSupreme/issues/10) | No rate limiting on auth endpoints             | `/api/auth/signin` and `/api/auth/signup` are unprotected; brute-force and bulk-registration possible. Fix: add ASP.NET Core `RateLimiter` middleware.                                                                                                                                                                      |
| [#11](https://github.com/HansBakN/Wordle.TrackerSupreme/issues/11) | No input length validation on signup DTOs      | `SignUpRequest` and admin update DTOs have no `[StringLength]` guards; an oversized `Password` causes intentionally-slow bcrypt to become a DoS vector.                                                                                                                                                                     |
| [#12](https://github.com/HansBakN/Wordle.TrackerSupreme/issues/12) | `AuthController` accesses `DbContext` directly | Bypasses `IPlayerRepository`, breaking testability and consistency.                                                                                                                                                                                                                                                         |

## Working patterns

### Adding or changing an API endpoint

1. Update Domain interfaces/models.
2. Update Application service implementation.
3. Update API controller + DTOs.
4. Regenerate the frontend API client from `openapi.json` (or update manually and keep `openapi.json` in sync).
5. Update or add backend xUnit tests and frontend unit/UI tests.

### Data access

- Always go through repository interfaces (`IGameRepository`, `IPlayerRepository`).
- Keep `SaveChanges` calls at the end of the service method, not scattered across helpers.
- Migrations: generate new ones with EF tooling; never hand-edit existing migration files.

### Frontend style

- Svelte 5 runes (`$state`, `$effect`, `onclick`) on new pages; legacy `on:click` syntax still present on some pages — **do not mix** within the same file.
- Use typed API client from `src/lib/api-client/` wherever possible; avoid ad-hoc `fetch` calls.
- All new interactive elements need `data-testid` attributes for Playwright selectors.

### Testing requirements (non-negotiable)

- Every user-visible feature needs coverage at all three levels: backend xUnit, frontend Vitest unit, and Playwright UI/E2E.
- Run the full E2E suite (`./scripts/e2e.sh`) after any user-visible change — CI also runs it on every push.
- After adding a new E2E test, verify it passes locally before opening a PR.

### Commit / PR discipline

- Frontend formatter + linter must be clean: `npm run format && npm run lint`.
- Never commit secrets, `.env.local`, or API keys.
- Pre-commit hook runs secret detection + shellcheck automatically.
- Use feature branches (`feat/<name>`) for non-trivial changes.

## Environment variables

Configured via `.env.local` (git-ignored). Use `.env.example` as a template.

| Variable                                            | Purpose                                                           |
| --------------------------------------------------- | ----------------------------------------------------------------- |
| `APP_HOST`                                          | Hostname used by Traefik / CORS                                   |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Database credentials                                              |
| `JWT__SECRET`                                       | Must be ≥32 bytes (ASCII); used for HS256 signing                 |
| `JWT__ISSUER`, `JWT__AUDIENCE`                      | JWT validation parameters                                         |
| `ASPNETCORE_ENVIRONMENT`                            | `Development` enables Swagger + dev word provider                 |
| `Cors__AllowedOrigins__0`                           | Comma-free origin allowlist entry                                 |
| `Seeder__*`                                         | Controls deterministic data generation (see README for full list) |

## Project-level tooling

### Skills (slash commands)

| Command           | When to use                                                                                                   |
| ----------------- | ------------------------------------------------------------------------------------------------------------- |
| `/e2e`            | After any user-visible change — runs the full Docker-backed E2E suite and summarises results                  |
| `/regen-client`   | After any API contract change — regenerates `src/lib/api-client/` from `openapi.json` and fixes broken usages |
| `/migrate [Name]` | Apply pending EF migrations; if a name is given, also generates a new migration first                         |
| `/browser-qa`     | Open a live Chromium browser and perform interactive QA — sign in, play the game, verify UI behaviour         |

### Subagents

| Agent                  | Model | Purpose                                                                               |
| ---------------------- | ----- | ------------------------------------------------------------------------------------- |
| `openapi-client-regen` | Haiku | Mechanical client regeneration: runs `gen:api`, fixes type errors, reports diffs      |
| `db-inspector`         | Haiku | Read-only live DB inspection via the postgres MCP (counts, schema, anomaly detection) |

### MCP servers

**`playwright`** — live Chromium browser automation via `@playwright/mcp`. Provides `mcp__playwright__*` tools for navigating, clicking, typing, taking screenshots, reading the console, and inspecting network requests. Use the `/browser-qa` skill for a guided workflow. No extra setup needed — the MCP server starts automatically.

**`postgres`** — direct SQL access to the local PostgreSQL instance via `@modelcontextprotocol/server-postgres`.

Setup (one-time):

1. The `docker-compose.override.yml` already exposes port 5432 to the host.
2. Set `POSTGRES_URL` in your shell (or add it to `.env.local` for reference — but don't commit it):
   ```bash
   export POSTGRES_URL="postgresql://wordle_user:<password>@localhost:5432/wordle_trackersupreme"
   ```
   Substitute the values from your `.env.local` (`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`).
3. The MCP server starts automatically when Claude Code loads this project.

Use the `db-inspector` subagent for read-only queries, or invoke `mcp__postgres__*` tools directly.

## Security notes

- JWT secret validation enforced at startup — the API will not start without a ≥32-byte secret.
- EF Core parameterised queries everywhere — no raw SQL injection surface.
- Admin endpoints require `isAdmin: true` JWT claim (`[Authorize(Policy = "AdminOnly")]`).
- CORS uses an explicit origin allowlist (not `*`), but `AllowAnyMethod()` is used — consider restricting to `GET, POST, PUT, DELETE` if attack surface reduction is required.
- No rate limiting is currently configured (see issue #10).
- No input length guards on auth DTOs (see issue #11).
