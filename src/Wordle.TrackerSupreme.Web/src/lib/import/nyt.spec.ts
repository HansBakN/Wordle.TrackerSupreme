import { describe, expect, it } from 'vitest';
import { formatCountdown, hasUnavailableHistory, secondsUntilExpiry } from './nyt';

describe('NYT import presentation', () => {
	it('counts down to expiry without becoming negative', () => {
		expect(secondsUntilExpiry('2026-07-10T12:10:00Z', Date.parse('2026-07-10T12:09:31Z'))).toBe(29);
		expect(secondsUntilExpiry('2026-07-10T12:10:00Z', Date.parse('2026-07-10T12:11:00Z'))).toBe(0);
		expect(formatCountdown(125)).toBe('2:05');
	});

	it('describes aggregate-only games as unavailable history', () => {
		expect(hasUnavailableHistory(520, 515)).toBe(true);
		expect(hasUnavailableHistory(515, 515)).toBe(false);
	});
});
