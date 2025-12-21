import { describe, expect, it } from 'vitest';
import { getRevealDurationMs, shouldTriggerSolveCelebration } from './celebration';

describe('getRevealDurationMs', () => {
	it('calculates reveal time based on word length', () => {
		expect(getRevealDurationMs(5)).toBe(1830);
	});

	it('handles invalid or missing lengths', () => {
		expect(getRevealDurationMs(0)).toBe(950);
		expect(getRevealDurationMs(-4)).toBe(950);
		expect(getRevealDurationMs(Number.NaN)).toBe(950);
	});
});

describe('shouldTriggerSolveCelebration', () => {
	it('triggers only when status transitions to solved', () => {
		expect(shouldTriggerSolveCelebration('InProgress', 'Solved')).toBe(true);
		expect(shouldTriggerSolveCelebration('Failed', 'Solved')).toBe(true);
	});

	it('does not trigger when already solved or not solved', () => {
		expect(shouldTriggerSolveCelebration('Solved', 'Solved')).toBe(false);
		expect(shouldTriggerSolveCelebration('Solved', 'Failed')).toBe(false);
		expect(shouldTriggerSolveCelebration(null, 'InProgress')).toBe(false);
	});
});
