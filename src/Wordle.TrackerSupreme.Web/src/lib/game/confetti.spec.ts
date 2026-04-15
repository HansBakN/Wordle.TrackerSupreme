import { describe, expect, it } from 'vitest';
import { buildConfettiPieces, defaultConfettiPieceCount } from './confetti';

describe('confetti helpers', () => {
	it('uses a larger default burst for win celebrations', () => {
		const pieces = buildConfettiPieces(defaultConfettiPieceCount, () => 0.5);

		expect(defaultConfettiPieceCount).toBe(60);
		expect(pieces).toHaveLength(60);
	});
});
