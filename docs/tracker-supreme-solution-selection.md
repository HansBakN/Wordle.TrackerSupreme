# Tracker Supreme Solution Selection

Issue #100 introduces a new default Tracker Supreme puzzle stream that must not
reuse the New York Times daily solution. This document defines the intended
answer-selection behavior for issue #106. It does not implement the selector.

## Goals

- Choose one Tracker Supreme solution per calendar date.
- Keep the answer list close to the existing NYT-style five-letter word list.
- Allow previous Tracker Supreme solutions to be selected again.
- Make recent repeats very unlikely.
- Restore a previously used word to normal probability one year after it was
  last selected.
- Keep selection deterministic so all API instances agree on the same solution.
- Leave the NYT stream untouched.

## Word Source

Use the existing embedded `wordlist-nyt.txt` as the source list unless a
maintainer later provides a separate curated answer list. The list is already
loaded and normalized by `WordListValidator`, and it keeps guesses and possible
answers aligned with the current Wordle-style rules.

The selector should reject words that are not exactly the configured word length
or contain non-letter characters. The existing default is five uppercase ASCII
letters.

## Persisted Selection

The selected Tracker Supreme solution should be stored in the stream-specific
`DailyPuzzles` row for that date. Selection should run only when that row does
not already have a solution.

Persisting the selected solution is required because the recurrence penalty
depends on past selections. It also prevents later word-list changes from
changing a previously generated daily puzzle.

## Deterministic Randomness

For a target date, build a deterministic pseudo-random seed from:

- the puzzle stream value, `TrackerSupreme`;
- the target `PuzzleDate`;
- an application-level non-secret selector salt stored in configuration.

The salt should not be treated as a security secret. Its purpose is to prevent
the solution order from being trivially identical across cloned environments
that share the same date and word list.

Given the same date, stream, salt, word list, and solution history, the selector
must choose the same solution.

## Recency Weight

Each candidate word receives a base weight of `1.0` when it has never been used
as a Tracker Supreme solution.

For a previously used word, calculate `daysSinceLastUse` from the target date
and the most recent earlier Tracker Supreme puzzle that used that word.

Use this weight:

```text
if daysSinceLastUse >= 365:
    weight = 1.0
else:
    ageRatio = max(daysSinceLastUse, 0) / 365
    weight = max(0.02, ageRatio ^ 3)
```

This makes a next-day repeat possible but extremely unlikely, then gradually
accelerates the recovery curve until the word is fully restored after one year.
Examples:

| Days since last use | Weight |
| --- | ---: |
| 1 | 0.02 |
| 30 | 0.02 |
| 90 | 0.02 |
| 180 | 0.12 |
| 270 | 0.40 |
| 365 | 1.00 |

The `0.02` floor prevents a word from becoming impossible before the one-year
reset. The cubic curve makes the recovery slow at first and faster closer to the
reset date.

## Selection Algorithm

For the target date:

1. Load all valid candidate words from the configured answer source.
2. Load Tracker Supreme solution history before the target date.
3. Calculate each candidate word weight from the recency rule.
4. Use the deterministic seed to draw one weighted candidate.
5. Persist that word as the target date's Tracker Supreme solution.

If every candidate is filtered out or receives an invalid weight, fail the daily
puzzle creation with a clear service error instead of falling back to NYT.

## Boundaries

This selector applies only to the Tracker Supreme stream. The NYT stream should
continue to use the official provider or any future NYT-specific provider.

Stats and leaderboard code should not duplicate selection rules. They should
read persisted puzzle rows and their stream labels.

Seeder code may use the same selector for realistic history, but tests may also
seed explicit solutions when deterministic assertions are easier to maintain.

## Automated Coverage Required

Backend coverage should include:

- deterministic selection for a fixed date, salt, word list, and history;
- no dependence on the NYT official provider for Tracker Supreme selection;
- strong suppression for a recently used word;
- full weight recovery at `365` days;
- unchanged persisted solution when a puzzle row already has a solution;
- validation that invalid candidate words are excluded;
- failure behavior when no valid candidates are available.

Seeder coverage should include:

- generated Tracker Supreme histories with repeated words possible over long
  ranges;
- idempotent reseeding when solutions already exist.

E2E coverage should not assert the exact selected word unless the test controls
the selector inputs. Prefer asserting stream isolation and persisted state.

## Operational Notes

- Changing `wordlist-nyt.txt` affects only future ungenerated Tracker Supreme
  puzzles. Existing persisted solutions remain unchanged.
- Changing the selector salt changes future selections for dates without a
  persisted solution. It should be treated as an operational decision and noted
  in release notes.
- The implementation should expose enough diagnostics in logs to identify the
  target date, stream, candidate count, and whether the solution was created or
  reused from storage. It should not log full weight tables during normal
  requests.

## Open Follow-Ups

- Issue #101 should add the stream-aware schema needed to persist the selected
  Tracker Supreme solution separately from NYT.
- Issue #102 should expose the selected Tracker Supreme puzzle as the default
  gameplay stream.
- Issue #103 should keep stats and leaderboards based on persisted stream labels
  rather than recalculating solution-source assumptions.
