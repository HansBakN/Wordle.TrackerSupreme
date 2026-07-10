import { collectNytStates, isOfficialWordleUrl } from './extraction.js';

export async function runImport({ code, apiBase, tab, executeScript, fetchFn = fetch, onProgress = () => {} }) {
  if (!tab?.id || !isOfficialWordleUrl(tab.url)) throw new Error('Open the official NYT Wordle page before importing.');
  onProgress('catalogue');
  const catalogueResponse = await fetchFn(`${apiBase}/api/import/nyt/catalogue?importCode=${encodeURIComponent(code)}`);
  if (!catalogueResponse.ok) throw new Error('The Tracker Supreme import code is invalid or expired.');
  const catalogue = await catalogueResponse.json();
  onProgress('extracting');
  const [{ result }] = await executeScript({ target: { tabId: tab.id }, func: collectNytStates, args: [catalogue] });
  onProgress('uploading', result.states.length);
  const upload = await fetchFn(`${apiBase}/api/import/nyt`, {
    method: 'POST', headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ schemaVersion: 1, importCode: code, ...result })
  });
  const payload = await upload.json().catch(() => ({}));
  if (!upload.ok) throw new Error(payload.detail ?? 'Tracker Supreme could not complete the import.');
  return payload;
}
