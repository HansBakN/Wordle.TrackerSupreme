# Wordle Tracker Supreme – How to Run It

This project uses Docker Compose to run:
- Postgres database
- .NET API (port 8080)
- Svelte frontend (port 3000)
- Optional migrations runner
- Optional Traefik reverse proxy (only if you enable the `proxy` profile)

## Prerequisites
- Docker with Compose v2

## Quick start (local)
1) Review `.env.local` (already present) and adjust if needed:
   - `APP_HOST=localhost`
   - `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`
   - `ASPNETCORE_ENVIRONMENT=Development` (enables Swagger locally)
2) Start the stack (API on :8080, frontend on :3000):
   ```bash
   docker compose --env-file .env.local --profile app up -d --build
   ```
   - Frontend: `http://localhost:3000/`
   - API: `http://localhost:8080/api/status`
3) Apply database migrations (optional):
   ```bash
   docker compose --env-file .env.local --profile migrate up --build migrator
   ```
4) Seed local dev data (optional):
   ```bash
   docker compose --env-file .env.local --profile seed run --rm seeder
   ```
   The seeder is deterministic and configurable via environment variables or appsettings. Example overrides
   (can be added to `.env.local` for repeatable runs):
   - `Seeder__RandomSeed=4242`
   - `Seeder__PlayerCount=20`
   - `Seeder__MinSolvedPuzzles=30`
   - `Seeder__MaxSolvedPuzzles=200`
   - `Seeder__FailedPuzzlesMin=2`
   - `Seeder__FailedPuzzlesMax=12`
   - `Seeder__InProgressPuzzlesMin=1`
   - `Seeder__InProgressPuzzlesMax=4`
   - `Seeder__PuzzleDays=280`
   - `Seeder__DefaultPassword=dev-password`
   - `Seeder__AllowReseed=false`
   - `Seeder__AnchorDate=2025-02-01`
5) Stop everything:
   ```bash
   docker compose down
   ```
   Add `-v` to also remove the Postgres volume.

## Run all tests via Docker
Use the `tests` profile to run backend (.NET), frontend (Vitest), and UI (Playwright) suites without local SDKs:
```bash
docker compose --profile tests run --rm tests-backend
docker compose --profile tests run --rm tests-frontend
```
Each command mounts the repo into a container, restores dependencies, and executes the respective test suite.

To run both suites in one go, use the `tests-all` profile with compose up:
```bash
docker compose --profile tests-all up --abort-on-container-exit --remove-orphans
```
This starts both test containers; exit codes from either will stop the stack when `--abort-on-container-exit` is set.

## End-to-end (E2E) tests
The E2E runner starts the backend + frontend, resets deterministic seed data, and runs Playwright headlessly.

Prereqs:
- Docker (for API + DB + web containers)
- Node 20+ and npm
- Playwright browsers installed in the frontend workspace: `cd src/Wordle.TrackerSupreme.Web && npx playwright install chromium`

Run the full E2E suite from the repo root:
```bash
./scripts/e2e.sh
```

Environment overrides:
- `BACKEND_URL` (default `http://localhost:8080`)
- `FRONTEND_URL` (default `http://localhost:3000`)
- `E2E_BASE_URL` (defaults to `FRONTEND_URL`)
- `ARTIFACT_DIR` (default `artifacts/e2e`)
- `ENV_FILE` (default `.env.local` if present)

Artifacts:
- Backend logs: `artifacts/e2e/backend.log`
- Frontend logs: `artifacts/e2e/frontend.log`
- Playwright artifacts and report: `artifacts/e2e/playwright/`

Reset safety:
- The seeder reset is gated. The script sets `E2E_RESET_ENABLED=true` and `Seeder__ResetDatabase=true`.
- Resets are blocked in Production environments.

## Config files
- `docker-compose.yml` – all services; Traefik is optional via the `proxy` profile.
- `.env.local` – local defaults (ignored by git).
- `.env.example` – template for your own env file.

## Notes
- Postgres is only reachable on the internal Docker network; expose `5432` in an override if you need host access.
- Traefik is off by default to avoid Docker socket issues on Windows; enable it with `--profile proxy` after adjusting the socket mount for your platform. For HTTPS/domain use with Traefik, uncomment the ACME lines in `docker-compose.yml` and set `TRAEFIK_ACME_EMAIL` in your env file.
