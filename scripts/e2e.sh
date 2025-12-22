#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACT_DIR="${ARTIFACT_DIR:-$ROOT_DIR/artifacts/e2e}"
BACKEND_URL="${BACKEND_URL:-http://localhost:8080}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost:3000}"
E2E_BASE_URL="${E2E_BASE_URL:-$FRONTEND_URL}"
BACKEND_HEALTH_URL="${BACKEND_HEALTH_URL:-$BACKEND_URL/health/ready}"
FRONTEND_HEALTH_URL="${FRONTEND_HEALTH_URL:-$FRONTEND_URL}"
ENV_FILE="${ENV_FILE:-$ROOT_DIR/.env.local}"

if [[ -z "$ARTIFACT_DIR" || "$ARTIFACT_DIR" == "/" ]]; then
  echo "Refusing to use unsafe ARTIFACT_DIR: $ARTIFACT_DIR" >&2
  exit 1
fi

rm -rf "$ARTIFACT_DIR"
mkdir -p "$ARTIFACT_DIR"

compose=(docker compose)
if [[ -f "$ENV_FILE" ]]; then
  compose+=(--env-file "$ENV_FILE")
fi

BACKEND_LOG_PID=""
FRONTEND_LOG_PID=""

cleanup() {
  set +e
  if [[ -n "$BACKEND_LOG_PID" ]]; then
    kill "$BACKEND_LOG_PID" >/dev/null 2>&1 || true
  fi
  if [[ -n "$FRONTEND_LOG_PID" ]]; then
    kill "$FRONTEND_LOG_PID" >/dev/null 2>&1 || true
  fi
  "${compose[@]}" --profile app down >/dev/null 2>&1 || true
}

trap cleanup EXIT

"${compose[@]}" --profile app up -d --build db api web

"${compose[@]}" logs -f --no-color api >"$ARTIFACT_DIR/backend.log" 2>&1 &
BACKEND_LOG_PID=$!
"${compose[@]}" logs -f --no-color web >"$ARTIFACT_DIR/frontend.log" 2>&1 &
FRONTEND_LOG_PID=$!

"$ROOT_DIR/scripts/wait-for-url.sh" "$BACKEND_HEALTH_URL" 180
"$ROOT_DIR/scripts/wait-for-url.sh" "$FRONTEND_HEALTH_URL" 180

E2E_RESET_ENABLED=true \
  Seeder__AllowReseed=true \
  Seeder__ResetDatabase=true \
  DOTNET_ENVIRONMENT=Development \
  "${compose[@]}" --profile seed run --rm --build \
  -e E2E_RESET_ENABLED=true \
  -e Seeder__AllowReseed=true \
  -e Seeder__ResetDatabase=true \
  -e DOTNET_ENVIRONMENT=Development \
  seeder

export E2E_BASE_URL
export E2E_ARTIFACT_DIR="$ARTIFACT_DIR/playwright"
export CI=1

if [[ ! -d "$ROOT_DIR/src/Wordle.TrackerSupreme.Web/node_modules" ]]; then
  (cd "$ROOT_DIR/src/Wordle.TrackerSupreme.Web" && npm ci)
fi

(cd "$ROOT_DIR/src/Wordle.TrackerSupreme.Web" && {
  if [[ "${CI:-}" == "1" && "$(uname -s)" == "Linux" ]]; then
    if command -v sudo >/dev/null 2>&1; then
      sudo npx playwright install --with-deps chromium
    else
      npx playwright install --with-deps chromium
    fi
  else
    npx playwright install chromium
  fi
})
(cd "$ROOT_DIR/src/Wordle.TrackerSupreme.Web" && npm run e2e)
