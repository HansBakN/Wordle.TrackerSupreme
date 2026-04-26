#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$ROOT_DIR/scripts/lib/compose.sh"
FRONTEND_DIR="$ROOT_DIR/src/Wordle.TrackerSupreme.Web"

ARTIFACT_DIR="${ARTIFACT_DIR:-$ROOT_DIR/artifacts/e2e}"
BACKEND_URL="${BACKEND_URL:-http://localhost:8080}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost:3000}"
E2E_BASE_URL="${E2E_BASE_URL:-$FRONTEND_URL}"
BACKEND_HEALTH_URL="${BACKEND_HEALTH_URL:-$BACKEND_URL/health/ready}"
FRONTEND_HEALTH_URL="${FRONTEND_HEALTH_URL:-$FRONTEND_URL}"
ENV_FILE="${ENV_FILE:-$ROOT_DIR/.env.local}"
is_ci=false
if [[ "${CI:-}" == "1" || "${CI:-}" == "true" || "${GITHUB_ACTIONS:-}" == "true" ]]; then
  is_ci=true
  export CI=1
fi

if [[ -z "$ARTIFACT_DIR" || "$ARTIFACT_DIR" == "/" ]]; then
  echo "Refusing to use unsafe ARTIFACT_DIR: $ARTIFACT_DIR" >&2
  exit 1
fi

rm -rf "$ARTIFACT_DIR"
mkdir -p "$ARTIFACT_DIR"

resolve_compose_command
compose=("${COMPOSE_CMD[@]}")
if [[ "$is_ci" == "true" ]]; then
  compose+=(-f "$ROOT_DIR/docker-compose.yml")
fi
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

ASPNETCORE_ENVIRONMENT=Development \
  "${compose[@]}" --profile app up -d --build db api web

"${compose[@]}" logs -f --no-color api >"$ARTIFACT_DIR/backend.log" 2>&1 &
BACKEND_LOG_PID=$!
"${compose[@]}" logs -f --no-color web >"$ARTIFACT_DIR/frontend.log" 2>&1 &
FRONTEND_LOG_PID=$!

"$ROOT_DIR/scripts/wait-for-url.sh" "$BACKEND_HEALTH_URL" 180
"$ROOT_DIR/scripts/wait-for-url.sh" "$FRONTEND_HEALTH_URL" 180

if compose_supports_run_build; then
  "${compose[@]}" --profile migrate run --rm --build migrator
else
  "${compose[@]}" --profile migrate build migrator
  "${compose[@]}" --profile migrate run --rm migrator
fi

if compose_supports_run_build; then
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
else
  "${compose[@]}" --profile seed build seeder
  E2E_RESET_ENABLED=true \
    Seeder__AllowReseed=true \
    Seeder__ResetDatabase=true \
    DOTNET_ENVIRONMENT=Development \
    "${compose[@]}" --profile seed run --rm \
    -e E2E_RESET_ENABLED=true \
    -e Seeder__AllowReseed=true \
    -e Seeder__ResetDatabase=true \
    -e DOTNET_ENVIRONMENT=Development \
    seeder
fi

export E2E_BASE_URL
export E2E_ARTIFACT_DIR="$ARTIFACT_DIR/playwright"

playwright_cli="$FRONTEND_DIR/node_modules/.bin/playwright"
needs_frontend_install=false

if [[ "$is_ci" == "true" || ! -x "$playwright_cli" ]]; then
  needs_frontend_install=true
fi

if [[ "$needs_frontend_install" == "true" ]]; then
  if [[ -d "$FRONTEND_DIR/node_modules" ]]; then
    if command -v sudo >/dev/null 2>&1; then
      sudo rm -rf "$FRONTEND_DIR/node_modules"
    else
      rm -rf "$FRONTEND_DIR/node_modules"
    fi
  fi
  (cd "$FRONTEND_DIR" && npm ci)
fi

(cd "$FRONTEND_DIR" && {
  if [[ "$is_ci" == "true" && "$(uname -s)" == "Linux" ]]; then
    if command -v sudo >/dev/null 2>&1; then
      sudo npx playwright install-deps chromium
    fi
  fi
  npx playwright install chromium
})
(cd "$ROOT_DIR/src/Wordle.TrackerSupreme.Web" && npm run e2e)
