# Puzzle Stream Migration Plan

Issue #100 splits the app from one daily puzzle per date into separate Tracker
Supreme and New York Times puzzle streams. This document covers the historical
data migration and rollout expectations for issue #105. It intentionally does
not add the schema change; it defines the data rules that the schema work should
follow.

## Current State

- `DailyPuzzles` has one row per `PuzzleDate`, enforced by the unique
  `IX_DailyPuzzles_PuzzleDate` index.
- `Attempts` points at `DailyPuzzles` with `DailyPuzzleId`.
- One player can have only one attempt for a puzzle row, enforced by
  `IX_Attempts_PlayerId_DailyPuzzleId`.
- Existing daily solutions come from the New York Times source through
  `DailyPuzzleService` and `OfficialWordProvider`.
- Stats, leaderboards, admin attempt views, seeding, and game state all infer the
  puzzle identity from date alone.

## Migration Decision

Classify every existing `DailyPuzzles` row as the New York Times stream.

This is the safest default because pre-split solutions and attempts were based
on the NYT source. Backfilling existing rows as Tracker Supreme would create
false main-puzzle wins and would immediately pollute the new main leaderboard.

Tracker Supreme rows should be created only by the new Tracker Supreme puzzle
selection flow after the stream-aware schema exists. Historical Tracker Supreme
rows should not be fabricated unless a maintainer performs a separate, explicit
data import with known solutions.

## Expected Data Shape

The stream-aware schema should support at least two stream values:

- `TrackerSupreme`: the new default main puzzle stream.
- `NewYorkTimes`: the optional NYT puzzle stream.

Expected constraints after migration:

- `(PuzzleDate, Stream)` is unique for `DailyPuzzles`.
- `PuzzleDate` alone is no longer unique.
- `Attempts` can keep using `DailyPuzzleId` for uniqueness because separate
  streams have separate puzzle rows.
- A player can have one attempt per stream per date because that becomes one
  attempt per distinct puzzle row.

## Backfill Steps

The schema migration should follow this order:

1. Add a puzzle-stream column to `DailyPuzzles` with a temporary default of
   `NewYorkTimes`.
2. Backfill all existing `DailyPuzzles` rows to `NewYorkTimes`.
3. Make the new column required.
4. Drop the unique `IX_DailyPuzzles_PuzzleDate` index.
5. Add a unique index on `(PuzzleDate, Stream)`.
6. Keep `IX_Attempts_PlayerId_DailyPuzzleId` unchanged.
7. Update the EF model snapshot and create focused migration tests or repository
   tests that prove both streams can exist on the same date.

The migration should not update existing `Attempts` directly. Their stream is
derived through the already-linked `DailyPuzzleId`.

## Post-Migration Behavior

Immediately after migration:

- Existing attempts remain visible as NYT history.
- The main Tracker Supreme leaderboard contains no migrated historical wins.
- NYT-inclusive stats may include migrated historical attempts when the user
  explicitly selects that view.
- Personal history or breakdown views should label migrated attempts as NYT.
- Admin views should show the stream for existing puzzle and attempt records
  wherever date alone would be ambiguous.

After the first Tracker Supreme puzzle is generated:

- The home page should default to the Tracker Supreme stream.
- NYT gameplay should remain reachable through the stream-switching behavior
  defined in the gameplay issue.
- Attempts in each stream should remain isolated for duplicate-attempt checks,
  easy/hard mode, reveal state, solution display, and guess history.

## Automated Coverage Required

Backend coverage should include:

- A migration or repository test that starts with one existing date and confirms
  the row is classified as `NewYorkTimes`.
- A repository test that permits `TrackerSupreme` and `NewYorkTimes` puzzles on
  the same date.
- A repository or service test that prevents duplicate `(PuzzleDate, Stream)`
  puzzle rows.
- A gameplay test that the same player can have one attempt in each stream for
  the same date.
- A stats test that migrated NYT attempts do not count in Tracker Supreme-only
  totals.

Frontend and E2E coverage should include:

- A stats or leaderboard test where Tracker Supreme-only totals exclude
  migrated NYT history.
- A NYT-inclusive stats or leaderboard test where migrated NYT history is
  included.
- An admin UI test that shows stream labels for ambiguous same-date records.

Seeder coverage should include:

- Seeded data with both streams for at least one date.
- Seeded historical NYT attempts that do not count toward the main leaderboard.
- Idempotent seeding behavior when both stream rows already exist.

## Rollout Checklist

Before deploying the stream-aware schema:

- Confirm the production database does not already contain duplicate
  `DailyPuzzles.PuzzleDate` rows. The current unique index should guarantee this,
  but the deployment notes should still call it out.
- Confirm the migration classifies all existing rows as `NewYorkTimes`.
- Confirm the application version that understands streams is deployed with the
  schema migration. Older app versions assume `PuzzleDate` is globally unique.
- Confirm the main leaderboard copy explains that it starts from Tracker Supreme
  attempts only.

After deployment:

- Existing users should still be able to see their historical NYT attempts in
  NYT-inclusive views.
- Users should not see old NYT wins inflate the main Tracker Supreme leaderboard.
- Admins should expect existing historical records to be labeled as NYT.
- The first Tracker Supreme puzzle date should create a new puzzle row even when
  a NYT row exists for the same date.

## Open Follow-Ups

- Issue #101 should introduce the stream identity and schema migration using this
  plan.
- Issue #102 should define the gameplay selection and stream-switching behavior.
- Issue #103 should define the stats and leaderboard defaults and inclusive
  views.
- Issue #104 should update admin and seeding workflows after the core stream
  model is known.
- Issue #106 should define how Tracker Supreme daily solutions are selected.
