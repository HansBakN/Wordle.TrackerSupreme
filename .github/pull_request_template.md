## Summary
- Describe the user-facing or developer-facing change.

## Verification
- [ ] `docker compose --profile tests run --rm tests-backend`
- [ ] `docker compose --profile tests run --rm tests-frontend`
- [ ] `./scripts/e2e.sh`
- [ ] `cd src/Wordle.TrackerSupreme.Web && npm run format`
- [ ] `cd src/Wordle.TrackerSupreme.Web && npm run lint`

## API / Data Contract Impact
- [ ] No API contract changes
- [ ] `openapi.json` updated
- [ ] frontend API client regenerated
- [ ] migration added

## Notes
- Seed data, manual test notes, screenshots, or follow-ups.
