---
name: migrate
description: Apply pending EF Core database migrations via Docker Compose. Use after pulling new migration files or when the DB schema is behind the current code.
---

Apply database migrations to the local PostgreSQL instance:

1. Ensure the database container is running. If not, start it:
   ```
   docker compose --env-file .env.local --profile app up -d db
   ```
2. Run the migrator:
   ```
   docker compose --env-file .env.local --profile migrate up --build migrator
   ```
3. Check the output for errors. A successful run ends with "Done." from the EF tooling.
4. Report success or, on failure, include the relevant error lines.

**Creating a new migration** (when the domain model has changed):

If $ARGUMENTS contains a migration name (e.g. `/migrate AddGuessIndexes`), also generate the migration file before applying:

1. Confirm the stack is running and the DB is reachable.
2. Run the EF `migrations add` command from the backend source tree — the startup project is `Wordle.TrackerSupreme.Api` and the migrations project is `Wordle.TrackerSupreme.Migrations`:
   ```
   dotnet ef migrations add $ARGUMENTS \
     --project src/Wordle.TrackerSupreme.BackEnd/Wordle.TrackerSupreme.Migrations \
     --startup-project src/Wordle.TrackerSupreme.BackEnd/Wordle.TrackerSupreme.Api \
     --context WordleTrackerSupremeDbContext
   ```
   This requires `dotnet-ef` tool and a reachable database (set `ConnectionStrings__WordleTrackerSupremeDbContext` in your environment or `.env.local`).
3. Review the generated Up/Down SQL in the new migration file before applying.
4. Then apply via the migrator container (step 2 above).

Never hand-edit existing migration files.
