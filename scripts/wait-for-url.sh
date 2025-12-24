#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "Usage: $0 <url> <timeout-seconds>" >&2
  exit 1
fi

URL="$1"
TIMEOUT_SECONDS="$2"
STATUS_REGEX="${WAIT_FOR_URL_STATUS_REGEX:-^(2|3)}"
SLEEP_SECONDS="${WAIT_FOR_URL_INTERVAL_SECONDS:-0.5}"

start_time="$(date +%s)"

while true; do
  status_code="$(curl -s -o /dev/null -w "%{http_code}" "$URL" || true)"
  if [[ "$status_code" =~ $STATUS_REGEX ]]; then
    exit 0
  fi

  now="$(date +%s)"
  elapsed=$((now - start_time))
  if (( elapsed >= TIMEOUT_SECONDS )); then
    echo "Timed out waiting for $URL after ${TIMEOUT_SECONDS}s (last status: ${status_code})." >&2
    exit 1
  fi

  sleep "$SLEEP_SECONDS"
done
