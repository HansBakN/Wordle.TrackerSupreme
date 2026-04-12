---
name: e2e
description: Run the full Wordle TrackerSupreme E2E suite — starts the Docker stack, resets deterministic seed data, and runs Playwright headlessly. Use after any user-visible change before opening a PR.
---

Run the full end-to-end test suite for this project:

1. Run `./scripts/e2e.sh` from the repo root. This script handles everything: starting the Docker stack, applying migrations, resetting seed data, installing Playwright browsers, and executing all specs.
2. Monitor the output for pass/fail results per spec.
3. If the suite fails, read the relevant artifact files to diagnose:
   - `artifacts/e2e/backend.log` — API logs
   - `artifacts/e2e/frontend.log` — SvelteKit logs
   - `artifacts/e2e/playwright/` — Playwright HTML report and traces
4. Report a concise summary: total specs, passed, failed, and any error messages from failing tests.
5. If failures look like flakiness (timing, race condition) rather than a real regression, note that and suggest re-running.

The full suite takes 3–5 minutes. Do not interrupt it early.
