import type { GuessResponse, LetterResult } from './types';

export function getKeyboardLetterState(
	guesses: GuessResponse[] | null | undefined,
	letter: string
): LetterResult | null {
	if (!guesses?.length) {
		return null;
	}

	let best: LetterResult | null = null;

	for (const guess of guesses) {
		for (const feedback of guess.feedback) {
			if (feedback.letter !== letter) {
				continue;
			}

			if (feedback.result === 'Correct') {
				return 'Correct';
			}

			if (feedback.result === 'Present') {
				best = 'Present';
			} else if (best === null) {
				best = 'Absent';
			}
		}
	}

	return best;
}
