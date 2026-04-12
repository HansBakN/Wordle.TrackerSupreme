---
name: regen-client
description: Regenerate the TypeScript API client from openapi.json after any API contract change (new endpoints, changed DTOs, renamed fields). Keeps the frontend in sync with the backend.
---

Regenerate the frontend TypeScript API client from the OpenAPI spec:

1. Run `npm run gen:api` inside `src/Wordle.TrackerSupreme.Web/` (uses `openapi-typescript-codegen` with the local `openapi.json`).
2. Show a diff summary of what changed in `src/lib/api-client/` — new services, removed models, renamed fields.
3. Search for any Svelte components or helpers that import from `$lib/api-client` or `$lib/game/api.ts` and check whether they still compile cleanly:
   - Run `npm run check` to catch TypeScript errors.
   - Fix any broken imports or type mismatches introduced by the regenerated client.
4. If $ARGUMENTS mentions a specific endpoint or model name, focus the impact analysis on usages of that name.
5. Report what changed and whether any manual follow-up is needed.

Note: `openapi.json` in `src/Wordle.TrackerSupreme.Web/` is the source of truth for the client. If you have just changed API controllers or DTOs, update `openapi.json` first (e.g. from Swagger UI at `http://localhost:8080/swagger`), then run this skill.
