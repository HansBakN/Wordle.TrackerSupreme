import { describe, expect, it } from 'vitest';
import {
	defaultLeaderboardMinimumGames,
	formatTodayLeaderboardMeta,
	getLeaderboardMinimumGames
} from './leaderboard';

describe('leaderboard helpers', () => {
	it('defaults to the ten-game minimum', () => {
		expect(defaultLeaderboardMinimumGames).toBe(10);
		expect(getLeaderboardMinimumGames(false)).toBe(10);
	});

	it('drops the threshold when low-volume players are included', () => {
		expect(getLeaderboardMinimumGames(true)).toBe(0);
	});

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
