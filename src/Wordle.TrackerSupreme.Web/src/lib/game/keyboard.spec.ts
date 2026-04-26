import { describe, expect, it } from 'vitest';
import type { GuessResponse } from './types';
import { getKeyboardLetterState } from './keyboard';

describe('getKeyboardLetterState', () => {
	it('returns the strongest known result for a letter', () => {
		const guesses: GuessResponse[] = [
			{
				guessId: 'guess-1',
				guessNumber: 1,
				guessWord: 'CRANE',
				feedback: [
					{ position: 0, letter: 'C', result: 'Absent' },
					{ position: 1, letter: 'R', result: 'Present' },
					{ position: 2, letter: 'A', result: 'Absent' },
					{ position: 3, letter: 'N', result: 'Correct' },
					{ position: 4, letter: 'E', result: 'Absent' }
				]
			},
			{
				guessId: 'guess-2',
				guessNumber: 2,
				guessWord: 'RINSE',
				feedback: [
					{ position: 0, letter: 'R', result: 'Correct' },
					{ position: 1, letter: 'I', result: 'Absent' },
					{ position: 2, letter: 'N', result: 'Correct' },
					{ position: 3, letter: 'S', result: 'Absent' },
					{ position: 4, letter: 'E', result: 'Present' }
				]
			}
		];

		expect(getKeyboardLetterState(guesses, 'R')).toBe('Correct');
		expect(getKeyboardLetterState(guesses, 'E')).toBe('Present');
		expect(getKeyboardLetterState(guesses, 'C')).toBe('Absent');
		expect(getKeyboardLetterState(guesses, 'Z')).toBeNull();
	});
});
