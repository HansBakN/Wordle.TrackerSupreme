export function isOfficialWordleUrl(url) {
  try {
    const parsed = new URL(url);
    return parsed.protocol === 'https:' && parsed.hostname === 'www.nytimes.com' && parsed.pathname.startsWith('/games/wordle');
  } catch {
    return false;
  }
}

export async function collectNytStates(catalogue) {
  const sleep = (milliseconds) => new Promise((resolve) => setTimeout(resolve, milliseconds));
  const number = (value) => value == null || value === '' ? null : Number(value);
  const list = (value) => {
    if (Array.isArray(value)) return value;
    if (typeof value !== 'string') return [];
    try { const parsed = JSON.parse(value); return Array.isArray(parsed) ? parsed : []; } catch { return []; }
  };
  const normalize = (raw) => {
    const state = raw?.game_data ?? raw?.gameData ?? raw?.state ?? raw;
    const puzzleId = number(state?.puzzle_id ?? state?.puzzleId ?? raw?.puzzle_id ?? raw?.puzzleId);
    const printDate = state?.print_date ?? state?.printDate ?? raw?.print_date ?? raw?.printDate;
    const boardState = list(state?.board_state ?? state?.boardState ?? state?.board ?? raw?.boardState)
      .filter((guess) => typeof guess === 'string' && guess.trim())
      .map((guess) => guess.trim().toUpperCase());
    const rawStatus = String(state?.status ?? state?.game_status ?? state?.gameStatus ?? raw?.status ?? '').toUpperCase();
    const status = ['WIN', 'WON', 'SUCCESS'].includes(rawStatus) ? 'WIN'
      : ['FAIL', 'FAILED', 'LOSS'].includes(rawStatus) ? 'FAIL' : null;
    if (!puzzleId || !printDate || !status || boardState.length === 0) return null;
    return {
      nytPuzzleId: puzzleId,
      printDate,
      timestamp: number(state?.timestamp ?? state?.last_updated ?? state?.lastUpdated ?? raw?.timestamp),
      status,
      hardMode: Boolean(state?.hard_mode ?? state?.hardMode ?? raw?.hardMode),
      isPlayingArchive: Boolean(state?.is_playing_archive ?? state?.isPlayingArchive ?? raw?.isPlayingArchive),
      boardState
    };
  };
  const flatten = (payload) => Array.isArray(payload) ? payload
    : Array.isArray(payload?.states) ? payload.states
    : Array.isArray(payload?.results) ? payload.results
    : payload && typeof payload === 'object' ? Object.values(payload) : [];
  const merged = new Map();
  let aggregateGamesPlayed = null;
  const add = (raw) => {
    const state = normalize(raw);
    if (!state) return;
    const previous = merged.get(state.nytPuzzleId);
    if (!previous || (state.timestamp ?? 0) >= (previous.timestamp ?? 0)) merged.set(state.nytPuzzleId, state);
  };

  for (let index = 0; index < localStorage.length; index++) {
    const key = localStorage.key(index);
    if (!key?.startsWith('games-state-wordleV2/')) continue;
    try {
      const payload = JSON.parse(localStorage.getItem(key));
      aggregateGamesPlayed = number(payload?.stats?.gamesPlayed ?? payload?.gamesPlayed) ?? aggregateGamesPlayed;
      flatten(payload).forEach(add);
    } catch { }
  }

  const ids = catalogue.map((entry) => entry.nytPuzzleId);
  for (let offset = 0; offset < ids.length; offset += 25) {
    const batch = ids.slice(offset, offset + 25);
    let response;
    try {
      response = await fetch(`/svc/games/state/wordleV2/latests?puzzle_ids=${batch.join(',')}`, { credentials: 'include' });
    } catch (error) {
      if (merged.size === 0) throw new Error(`NYT history could not be reached: ${error.message}`);
      break;
    }
    if (response.status === 403) throw new Error('NYT did not recognize a signed-in account. Sign in to NYT and try again.');
    if (!response.ok) {
      if (merged.size === 0) throw new Error(`NYT stopped the history request (${response.status}).`);
      break;
    }
    flatten(await response.json()).forEach(add);
    if (offset + 25 < ids.length) await sleep(250);
  }
  return { aggregateGamesPlayed, states: [...merged.values()].sort((a, b) => a.printDate.localeCompare(b.printDate)) };
}
