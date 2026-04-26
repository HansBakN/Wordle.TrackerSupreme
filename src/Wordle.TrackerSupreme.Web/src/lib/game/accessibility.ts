import type { LetterResult } from './types';

export function describeTileForScreenReader(
	letter: string | null | undefined,
	result: LetterResult | null
): string {
	const normalizedLetter = letter?.trim().toUpperCase() ?? '';
	if (!normalizedLetter) {
		return 'Empty tile';
	}

	if (result === null) {
		return normalizedLetter;
	}

	return `${normalizedLetter}, ${result.toLowerCase()}`;
}
