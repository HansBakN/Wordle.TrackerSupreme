import { describe, expect, it } from 'vitest';
import type { GameStateResponse } from './types';
import { buildShareText } from './share';

const baseState: GameStateResponse = {
	puzzleDate: '2026-04-12',
	cutoffPassed: false,
	solutionRevealed: false,
	allowLatePlay: true,
	wordLength: 5,
	maxGuesses: 6,
	isHardMode: true,
	canGuess: false,
	attempt: null,
	solution: null
};

describe('buildShareText', () => {
	it('formats a solved game as an emoji grid', () => {
		const state: GameStateResponse = {
			...baseState,
			attempt: {
				attemptId: 'a1',
				status: 'Solved',
				isAfterReveal: false,
				createdOn: '2026-04-12T08:00:00Z',
				completedOn: '2026-04-12T08:05:00Z',
				guesses: [
					{
						guessId: 'g1',
						guessNumber: 1,
						guessWord: 'CRANE',
						feedback: [
							{ position: 0, letter: 'C', result: 'Absent' },
							{ position: 1, letter: 'R', result: 'Present' },
							{ position: 2, letter: 'A', result: 'Absent' },
							{ position: 3, letter: 'N', result: 'Absent' },
							{ position: 4, letter: 'E', result: 'Absent' }
						]
					},
					{
						guessId: 'g2',
						guessNumber: 2,
						guessWord: 'PLANT',
						feedback: [
							{ position: 0, letter: 'P', result: 'Correct' },
							{ position: 1, letter: 'L', result: 'Correct' },
							{ position: 2, letter: 'A', result: 'Correct' },
							{ position: 3, letter: 'N', result: 'Correct' },
							{ position: 4, letter: 'T', result: 'Correct' }
						]
					}
				]
			}
		};

		const text = buildShareText(state);
		expect(text).toBe('Wordle Tracker Supreme 12/04-2026 2/6\n\n⬜🟨⬜⬜⬜\n🟩🟩🟩🟩🟩');
	});

	it('uses X/maxGuesses for a failed game', () => {
		const state: GameStateResponse = {
			...baseState,
			attempt: {
				attemptId: 'a2',
				status: 'Failed',
				isAfterReveal: false,
				createdOn: '2026-04-12T08:00:00Z',
				completedOn: '2026-04-12T08:10:00Z',
				guesses: [
					{
						guessId: 'g1',
						guessNumber: 1,
						guessWord: 'CRANE',
						feedback: [
							{ position: 0, letter: 'C', result: 'Absent' },
							{ position: 1, letter: 'R', result: 'Absent' },
							{ position: 2, letter: 'A', result: 'Absent' },
							{ position: 3, letter: 'N', result: 'Absent' },
							{ position: 4, letter: 'E', result: 'Absent' }
						]
					}
				]
			}
		};

		const text = buildShareText(state);
		expect(text).toContain('X/6');
		expect(text).toContain('⬜⬜⬜⬜⬜');
	});
});
