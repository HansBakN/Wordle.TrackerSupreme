# AGENTS – How to work in this repo

## Priorities
- Favor Docker Compose workflows; avoid introducing host-only steps when compose equivalents exist.
- Keep API contracts stable; if you change DTOs/controllers, update corresponding Svelte client models (`src/Wordle.TrackerSupreme.Web/src/lib/api-client` and `openapi.json`) and tests.
- Don’t hand-edit existing migrations unless explicitly requested; generate new ones when schema changes are intentional.
- Preserve Wordle rules: default 5-letter words, 6 guesses, solutions reveal at 12:00 (server local), guesses are normalized to uppercase.

## Quick project map
- Backend: `src/Wordle.TrackerSupreme.BackEnd`
  - API controllers: `Wordle.TrackerSupreme.Api/Controllers`
  - Application services: `Wordle.TrackerSupreme.Application/Services`
  - Domain models/interfaces: `Wordle.TrackerSupreme.Domain`
  - EF + repos: `Wordle.TrackerSupreme.Infrastructure`
  - Tests: `Wordle.TrackerSupreme.Tests`
- Frontend (SvelteKit): `src/Wordle.TrackerSupreme.Web`
  - API client + types: `src/lib/api-client`, `src/lib/auth`, `src/lib/game`
  - UI tests: `tests/ui` (Playwright), unit tests via Vitest.

## Run commands (copy/paste ready)
- Start stack (API :8080, web :3000): `docker compose --env-file .env.local --profile app up -d --build`
- Stop stack: `docker compose down` (add `-v` to drop Postgres volume `wordle_db_data`).
- Apply migrations only when needed: `docker compose --env-file .env.local --profile migrate up --build migrator`
- Tests (preferred):
  - Backend: `docker compose --profile tests run --rm tests-backend`
  - Frontend + UI: `docker compose --profile tests run --rm tests-frontend`
  - Both suites together: `docker compose --profile tests-all up --abort-on-container-exit --remove-orphans`
- Format/lint frontend (should be clean): `npm run format` then `npm run lint` in `src/Wordle.TrackerSupreme.Web`.

## Domain quick-reference
- One daily puzzle keyed by date; `DailyPuzzleService` creates or fills missing solutions.
- Gameplay defaults: word length 5, max guesses 6 (`GameOptions`).
- Reveal/cutoff: solutions unlock and late plays stop counting after `GameClock.GetRevealInstant` (12:00 local by default).
- Guess validation: non-empty, letters only, exact length, uppercased; attempts fail when out of guesses; solutions reveal when solved/failed or after cutoff.

## Working patterns
- Adding/changing API endpoints: adjust Domain interfaces/services, update Application implementations, wire through API controllers/DTOs, then refresh frontend client typings/openapi and Svelte usage.
- Data access: go through repositories (`GameRepository`, `PlayerRepository`); keep `SaveChanges` calls consistent with existing patterns.
- Testing: add/adjust unit tests in `Wordle.TrackerSupreme.Tests` for backend changes; mirror API changes with frontend unit/UI tests where applicable.
- Env/config: use `.env.local` for local defaults (APP_HOST, POSTGRES_*, JWT secret, ASPNETCORE_ENVIRONMENT). Avoid committing secrets.
- CI discipline: before handing off changes, always run the full test suite (backend + frontend/UI), run frontend formatter + linter (must be clean), and manually exercise the project to verify any newly implemented feature end-to-end.

## Style nudges
- C#: stick to current nullability/records/classes conventions; keep mapping helpers in controllers small and deterministic.
- Frontend: follow existing Svelte/TypeScript patterns (stores in `src/lib/auth`, API helpers in `src/lib/game/api.ts`), prefer typed API client usage over ad-hoc fetches.
- Control flow braces: always use explicit block scopes for `if/else/for/while` (no single-line implicit bodies). Example: use `if (condition) { return; }`, never `if (condition) return;`.
