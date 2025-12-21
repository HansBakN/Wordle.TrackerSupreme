import { describe, expect, it } from 'vitest';
import { buildStatsFilterRequest, defaultStatsFilterState } from './filters';

describe('stats filter helpers', () => {
	it('builds default request for hard mode before reveal', () => {
		const request = buildStatsFilterRequest(defaultStatsFilterState);

		expect(request.includeHardMode).toBe(true);
		expect(request.includeEasyMode).toBe(false);
		expect(request.includeBeforeReveal).toBe(true);
		expect(request.includeAfterReveal).toBe(false);
		expect(request.includeSolved).toBe(true);
		expect(request.includeFailed).toBe(true);
		expect(request.includeInProgress).toBe(false);
	});

	it('converts numeric filters and date strings when provided', () => {
		const request = buildStatsFilterRequest({
			...defaultStatsFilterState,
			fromDate: '2025-01-01',
			toDate: '2025-01-31',
			minGuessCount: '2',
			maxGuessCount: '5'
		});

		expect(request.fromDate).toBe('2025-01-01');
		expect(request.toDate).toBe('2025-01-31');
		expect(request.minGuessCount).toBe(2);
		expect(request.maxGuessCount).toBe(5);
	});

	it('drops invalid numeric filters', () => {
		const request = buildStatsFilterRequest({
			...defaultStatsFilterState,
			minGuessCount: 'nope',
			maxGuessCount: ''
		});

		expect(request.minGuessCount).toBeUndefined();
		expect(request.maxGuessCount).toBeUndefined();
	});

	it('accepts numeric inputs for guess counts', () => {
		const request = buildStatsFilterRequest({
			...defaultStatsFilterState,
			minGuessCount: 3,
			maxGuessCount: Number.NaN
		});

		expect(request.minGuessCount).toBe(3);
		expect(request.maxGuessCount).toBeUndefined();
	});
});
