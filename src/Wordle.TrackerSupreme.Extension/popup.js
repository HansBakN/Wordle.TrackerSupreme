import { API_BASE_URL } from './config.js';
import { runImport } from './import-flow.js';

const button = document.querySelector('#import');
const input = document.querySelector('#code');
const status = document.querySelector('#status');

button.addEventListener('click', async () => {
  button.disabled = true;
  try {
    const code = input.value.trim().toUpperCase();
    if (!/^[A-Z2-9]{4}-?[A-Z2-9]{4}$/.test(code)) throw new Error('Enter the eight-character import code.');
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    const payload = await runImport({ code, apiBase: API_BASE_URL, tab,
      executeScript: (options) => chrome.scripting.executeScript(options),
      onProgress: (step, count) => { status.textContent = step === 'catalogue' ? 'Downloading the published puzzle catalogue…'
        : step === 'extracting' ? 'Reading available Wordle history from NYT…' : `Uploading ${count} completed games…`; }
    });
    status.textContent = `Done: ${payload.imported} imported, ${payload.duplicates} duplicate, ${payload.conflicts} conflict, ${payload.rejected} rejected.`;
  } catch (error) {
    status.textContent = error instanceof Error ? error.message : 'The import could not be completed.';
  } finally {
    button.disabled = false;
  }
});
