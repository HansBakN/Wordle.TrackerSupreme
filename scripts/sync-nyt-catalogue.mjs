import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = path.join(root, 'src/Wordle.TrackerSupreme.BackEnd/Wordle.TrackerSupreme.Application/Services/Import/nyt-puzzle-catalogue.json');
const start = new Date('2021-06-19T00:00:00Z');
const today = new Date();
today.setUTCHours(0, 0, 0, 0);

let existing = [];
try { existing = JSON.parse(await readFile(output, 'utf8')); } catch { }
const byDate = new Map(existing.map((entry) => [entry.PrintDate, entry]));
const missing = [];
for (let date = new Date(start); date <= today; date.setUTCDate(date.getUTCDate() + 1)) {
  const value = date.toISOString().slice(0, 10);
  if (!byDate.has(value)) missing.push(value);
}

async function worker() {
  while (missing.length > 0) {
    const printDate = missing.shift();
    const response = await fetch(`https://www.nytimes.com/svc/wordle/v2/${printDate}.json`);
    if (!response.ok) throw new Error(`NYT returned ${response.status} for ${printDate}`);
    const payload = await response.json();
    byDate.set(printDate, {
      NytPuzzleId: payload.id,
      PrintDate: payload.print_date,
      PublicPuzzleNumber: payload.days_since_launch ?? null
    });
  }
}

await Promise.all(Array.from({ length: 4 }, worker));
const catalogue = [...byDate.values()].sort((left, right) => left.PrintDate.localeCompare(right.PrintDate));
await writeFile(output, `${JSON.stringify(catalogue, null, 2)}\n`);
console.log(`Wrote ${catalogue.length} published puzzles through ${catalogue.at(-1)?.PrintDate}.`);
