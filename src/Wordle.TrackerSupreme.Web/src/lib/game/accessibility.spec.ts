import { describe, expect, it } from 'vitest';
import { describeTileForScreenReader } from './accessibility';

describe('game accessibility helpers', () => {
	it('describes empty and evaluated tiles for screen readers', () => {
		expect(describeTileForScreenReader('', null)).toBe('Empty tile');
		expect(describeTileForScreenReader('c', 'Correct')).toBe('C, correct');
		expect(describeTileForScreenReader('r', 'Present')).toBe('R, present');
		expect(describeTileForScreenReader('a', 'Absent')).toBe('A, absent');
	});
});
