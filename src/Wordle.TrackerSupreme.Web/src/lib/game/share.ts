import type { GameStateResponse } from './types';

function emojiForResult(result: string): string {
	if (result === 'Correct') {
		return '🟩';
	}
	if (result === 'Present') {
		return '🟨';
	}
	return '⬜';
}

export function buildShareText(state: GameStateResponse): string {
	const guesses = state.attempt?.guesses ?? [];
	const guessCount = state.attempt?.status === 'Solved' ? guesses.length : 'X';
	const maxGuesses = state.maxGuesses;

	const date = state.puzzleDate
		? (() => {
				const d = new Date(state.puzzleDate);
				const day = String(d.getUTCDate()).padStart(2, '0');
				const month = String(d.getUTCMonth() + 1).padStart(2, '0');
				const year = d.getUTCFullYear();
				return `${day}/${month}-${year}`;
			})()
		: '';

	const header = `Wordle Tracker Supreme ${date} ${guessCount}/${maxGuesses}`;

	const rows = [...guesses]
		.sort((a, b) => a.guessNumber - b.guessNumber)
		.map((guess) =>
			[...guess.feedback]
				.sort((a, b) => a.position - b.position)
				.map((fb) => emojiForResult(fb.result))
				.join('')
		)
		.join('\n');

	return rows ? `${header}\n\n${rows}` : header;
}
