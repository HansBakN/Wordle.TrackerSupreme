# Wordle Tracker Supreme Web

SvelteKit frontend for Wordle Tracker Supreme. This app talks to the backend through the generated client in `src/lib/api-client` and is covered by both Vitest and Playwright suites.

## Tooling

- Node.js 20 from the repo root `.nvmrc`
- npm with the checked-in `package-lock.json`

## Commands

```bash
npm ci
npm run dev
npm run build
npm run check
npm run format
npm run lint
npm test -- --run
npm run e2e
```

## API client workflow

The frontend consumes the checked-in OpenAPI document at `openapi.json`.

When backend contracts change:

```bash
npm run gen:api
```

Then update any affected code in:

- `src/lib/api-client`
- `src/lib/auth`
- `src/lib/game`
- route-level components under `src/routes`

## Testing

- Unit/component tests: `npm test -- --run`
- Local UI tests: `npx playwright test`
- Full app E2E flow from repo root: `../../scripts/e2e.sh`

## Auth and admin areas

- Auth state lives in `src/lib/auth/store.ts`
- Admin tooling lives under `src/routes/admin`
- Prefer typed client calls over ad-hoc `fetch`
