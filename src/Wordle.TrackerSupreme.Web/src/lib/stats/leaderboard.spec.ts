import { describe, expect, it } from 'vitest';
import { formatTodayLeaderboardMeta } from './leaderboard';

describe('leaderboard helpers', () => {
	it('formats today rows for finished and in-progress attempts', () => {
		expect(
			formatTodayLeaderboardMeta({
				result: 'Solved',
				guessCount: 3,
				playedInHardMode: true
			})
		).toBe('3 guesses • Hard mode');

		expect(
			formatTodayLeaderboardMeta({
				result: 'In progress',
				guessCount: 4,
				playedInHardMode: false
			})
		).toBe('4 guesses so far • Easy mode');
	});
});
