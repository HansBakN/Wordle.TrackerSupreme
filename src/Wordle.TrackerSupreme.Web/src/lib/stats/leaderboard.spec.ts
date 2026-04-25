import { describe, expect, it } from 'vitest';
import { defaultLeaderboardMinimumGames, getLeaderboardMinimumGames } from './leaderboard';

describe('leaderboard helpers', () => {
	it('defaults to the ten-game minimum', () => {
		expect(defaultLeaderboardMinimumGames).toBe(10);
		expect(getLeaderboardMinimumGames(false)).toBe(10);
	});

	it('drops the threshold when low-volume players are included', () => {
		expect(getLeaderboardMinimumGames(true)).toBe(0);
	});
});
