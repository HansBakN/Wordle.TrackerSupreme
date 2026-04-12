#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FRONTEND_DIR="$ROOT_DIR/src/Wordle.TrackerSupreme.Web"

usage() {
  cat <<'EOF'
Usage: ./scripts/verify.sh [backend|frontend|lint|e2e|all]

Targets:
  backend   Run backend tests in Docker.
  frontend  Run frontend unit + Playwright UI tests in Docker.
  lint      Run frontend formatting checks and ESLint locally.
  e2e       Run the full end-to-end workflow.
  all       Run backend, frontend, lint, and e2e checks in sequence.
EOF
}

run_backend() {
  docker compose --profile tests run --rm tests-backend
}

run_frontend() {
  docker compose --profile tests run --rm tests-frontend
}

run_lint() {
  (
    cd "$FRONTEND_DIR"
    npm run lint
  )
}

run_e2e() {
  "$ROOT_DIR/scripts/e2e.sh"
}

target="${1:-all}"

case "$target" in
  backend)
    run_backend
    ;;
  frontend)
    run_frontend
    ;;
  lint)
    run_lint
    ;;
  e2e)
    run_e2e
    ;;
  all)
    run_backend
    run_frontend
    run_lint
    run_e2e
    ;;
  -h|--help|help)
    usage
    ;;
  *)
    usage >&2
    exit 1
    ;;
esac
