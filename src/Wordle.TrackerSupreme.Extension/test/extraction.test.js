import test from 'node:test';
import assert from 'node:assert/strict';
import { collectNytStates, isOfficialWordleUrl } from '../extraction.js';

test('validates the active tab strictly', () => {
  assert.equal(isOfficialWordleUrl('https://www.nytimes.com/games/wordle/index.html'), true);
  assert.equal(isOfficialWordleUrl('https://example.com/games/wordle'), false);
});

test('extracts local state, batches requests and keeps newest state', async () => {
  const storage = new Map([['games-state-wordleV2/player', JSON.stringify({ gamesPlayed: 3, states: [{ puzzle_id: 1, print_date: '2021-06-19', status: 'WIN', timestamp: 2, board_state: ['cigar'] }] })]]);
  global.localStorage = { length: 1, key: () => [...storage.keys()][0], getItem: (key) => storage.get(key) };
  const calls = [];
  global.fetch = async (url) => {
    calls.push(url);
    return { ok: true, status: 200, json: async () => ({ states: [{ puzzle_id: 1, print_date: '2021-06-19', status: 'WIN', timestamp: 3, board_state: ['rebut', 'cigar'], hard_mode: true }] }) };
  };
  const catalogue = Array.from({ length: 26 }, (_, index) => ({ nytPuzzleId: index + 1 }));
  const result = await collectNytStates(catalogue);
  assert.equal(calls.length, 2);
  assert.deepEqual(result.states[0].boardState, ['REBUT', 'CIGAR']);
  assert.equal(result.states[0].hardMode, true);
});

test('reports an unsigned-in NYT response', async () => {
  global.localStorage = { length: 0, key: () => null, getItem: () => null };
  global.fetch = async () => ({ ok: false, status: 403 });
  await assert.rejects(() => collectNytStates([{ nytPuzzleId: 1 }]), /signed-in account/);
});

test('keeps local results after a partial endpoint failure', async () => {
  const value = JSON.stringify({ states: [{ puzzleId: 1, printDate: '2021-06-19', status: 'FAIL', boardState: ['rebut', 'sissy', 'humph', 'awake', 'blush', 'focal'] }] });
  global.localStorage = { length: 1, key: () => 'games-state-wordleV2/a', getItem: () => value };
  global.fetch = async () => ({ ok: false, status: 500 });
  const result = await collectNytStates([{ nytPuzzleId: 1 }]);
  assert.equal(result.states.length, 1);
  assert.equal(result.states[0].status, 'FAIL');
});
