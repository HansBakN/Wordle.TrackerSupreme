#!/usr/bin/env bash

resolve_compose_command() {
  if docker compose version >/dev/null 2>&1; then
    COMPOSE_CMD=(docker compose)
    return 0
  fi

  if command -v docker-compose >/dev/null 2>&1; then
    COMPOSE_CMD=(docker-compose)
    return 0
  fi

  echo "Neither 'docker compose' nor 'docker-compose' is available." >&2
  return 1
}

compose_supports_run_build() {
  if [[ "${COMPOSE_CMD[0]}" == "docker-compose" ]]; then
    return 1
  fi

  local help_text
  help_text="$("${COMPOSE_CMD[@]}" run --help 2>/dev/null || true)"

  if [[ "$help_text" == *"--build"* ]]; then
    return 0
  fi

  return 1
}
