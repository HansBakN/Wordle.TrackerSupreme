# Tracker Supreme Chrome extension

This Manifest V3 extension imports completed Wordle states from the signed-in user's official NYT Wordle page. It has no background access to browsing history, never reads cookies, and receives only temporary access to the active NYT tab after the user clicks the extension.

## Local development and tests

```bash
cd src/Wordle.TrackerSupreme.Extension
npm test
WTS_API_BASE_URL=http://localhost:8080 npm run build
```

The build writes `dist/` and `wordle-tracker-supreme-extension.zip`. `WTS_API_BASE_URL` defaults to `https://wordle.trackersupreme.dk`; the build updates the distributable manifest's single host permission to the configured origin. The checked-in manifest retains only the production Tracker Supreme host permission.

To load it, open `chrome://extensions`, enable Developer mode, choose **Load unpacked**, and select `src/Wordle.TrackerSupreme.Extension/dist`. Open `https://www.nytimes.com/games/wordle/` while signed in to NYT, then activate the extension and enter a code created on Tracker Supreme's `/import/nyt` page.

## Contracts and limitations

The extension uses the code with `GET /api/import/nyt/catalogue`, reads `games-state-wordleV2/*` local-storage entries in the active page, requests NYT states in sequential batches of 25, and sends the explicit schema-v1 payload to `POST /api/import/nyt`. Only completed `WIN` and `FAIL` records are uploaded. NYT cookies, credentials, user IDs, solutions, and feedback are never included.

NYT does not guarantee that an individual state remains available for every game represented by aggregate statistics. The import reports that difference as unavailable/unknown history and does not reconstruct guesses. Endpoint or storage-shape changes on NYT may require an extension update; requests stop cleanly on rejection or an incompatible response.

The local fixture at `test/fixture/nyt-wordle.html` supports automated extraction tests without contacting NYT.
