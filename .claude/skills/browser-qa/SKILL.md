---
name: browser-qa
description: Open a live browser and perform interactive QA on the running app — sign in, play the game, verify UI behaviour. Use this after fixing UI bugs or before opening a PR when you need human-level click/type testing rather than automated scripts.
---

## Overview

This skill uses the **Playwright MCP** tools to drive a real Chromium browser session against the running app. It is the equivalent of a human QA developer manually testing the app.

**Prerequisite:** The Docker stack must be running.
Start it if needed: `docker-compose --env-file .env.local --profile app up -d --build`
The web app is then reachable at **http://localhost:3000**.

---

## Available Playwright MCP tools

| Tool                                        | Purpose                                                    |
| ------------------------------------------- | ---------------------------------------------------------- |
| `mcp__playwright__browser_navigate`         | Open a URL                                                 |
| `mcp__playwright__browser_snapshot`         | Get the current accessibility tree (use to find selectors) |
| `mcp__playwright__browser_take_screenshot`  | Visual screenshot for inspection                           |
| `mcp__playwright__browser_click`            | Click an element (use `ref` from snapshot)                 |
| `mcp__playwright__browser_type`             | Type text into a focused field                             |
| `mcp__playwright__browser_press_key`        | Press a keyboard key (e.g. `Enter`, `Backspace`)           |
| `mcp__playwright__browser_fill_form`        | Fill a form by label                                       |
| `mcp__playwright__browser_wait_for`         | Wait for text/selector to appear                           |
| `mcp__playwright__browser_console_messages` | Read JS console for errors                                 |
| `mcp__playwright__browser_network_requests` | Inspect XHR/fetch calls and their responses                |
| `mcp__playwright__browser_evaluate`         | Run arbitrary JS in the page                               |
| `mcp__playwright__browser_close`            | Close the browser when done                                |

---

## Standard QA workflow

### 1. Sign in

```
navigate → http://localhost:3000/signin
snapshot → find username/password inputs
type credentials (use seed data: player1 / Player1! or admin / Admin1!)
click Sign in
wait_for → "Daily Wordle" heading
```

### 2. Play a round (golden path)

```
snapshot → confirm game board is visible
press_key → letters one by one (or use browser_evaluate to call pushLetter)
verify → current-row tiles update with each letter
press_key → Enter
wait_for → row reveals with colour feedback
network_requests → confirm POST /api/game/guess returned 200
```

### 3. Test double-submit prevention (Issue #7)

```
Type a 5-letter word using keyboard letters
snapshot → verify on-screen Enter button is enabled (not disabled)
press_key → Enter   ← fires keyboard handler
snapshot → verify on-screen Enter button is NOW disabled (submitting=true guard)
network_requests → confirm only ONE POST /api/game/guess was made
```

### 4. Check for JS errors

```
console_messages → look for any [error] lines
```

### 5. Close

```
browser_close
```

---

## Seed accounts (deterministic dev data)

After running `docker-compose --profile seed run --rm seeder`, these accounts exist:

| Username  | Password   | Notes                            |
| --------- | ---------- | -------------------------------- |
| `player1` | `Player1!` | Regular player with game history |
| `admin`   | `Admin1!`  | Admin account                    |

---

## Tips

- Always call `browser_snapshot` before clicking — it returns `ref` IDs needed by `browser_click`.
- Use `data-testid` attributes where available (e.g. `board-row-0`, `confetti`, `win-stats`) for stable selectors.
- `browser_network_requests` is the most reliable way to verify that exactly one API call was made (not two) for double-submit tests.
- If the browser session dies, just call `browser_navigate` again — it restarts automatically.
