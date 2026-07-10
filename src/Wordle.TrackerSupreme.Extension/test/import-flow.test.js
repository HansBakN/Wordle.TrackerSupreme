import test from 'node:test';
import assert from 'node:assert/strict';
import { runImport } from '../import-flow.js';

const tab = { id: 7, url: 'https://www.nytimes.com/games/wordle/index.html' };

test('uploads normalized extraction payload and returns a successful summary', async () => {
  const calls = [];
  const fetchFn = async (url, options) => {
    calls.push({ url, options });
    if (url.includes('catalogue')) return { ok: true, json: async () => [{ nytPuzzleId: 1 }] };
    return { ok: true, json: async () => ({ imported: 1, duplicates: 0, conflicts: 0, rejected: 0 }) };
  };
  const executeScript = async () => [{ result: { aggregateGamesPlayed: 1, states: [{ nytPuzzleId: 1, status: 'WIN', boardState: ['CIGAR'] }] } }];
  const result = await runImport({ code: 'ABCD-EFGH', apiBase: 'https://wordle.trackersupreme.dk', tab, executeScript, fetchFn });
  assert.equal(result.imported, 1);
  assert.equal(JSON.parse(calls[1].options.body).schemaVersion, 1);
  assert.equal(JSON.parse(calls[1].options.body).importCode, 'ABCD-EFGH');
});

test('surfaces upload failure details', async () => {
  const fetchFn = async (url) => url.includes('catalogue')
    ? { ok: true, json: async () => [] }
    : { ok: false, json: async () => ({ detail: 'Import code has already been used.' }) };
  const executeScript = async () => [{ result: { aggregateGamesPlayed: 0, states: [] } }];
  await assert.rejects(() => runImport({ code: 'ABCD-EFGH', apiBase: 'https://wordle.trackersupreme.dk', tab, executeScript, fetchFn }), /already been used/);
});
