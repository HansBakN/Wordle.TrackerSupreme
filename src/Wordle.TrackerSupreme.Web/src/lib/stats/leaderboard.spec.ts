import { describe, expect, it } from 'vitest';
import { formatTodayLeaderboardMeta, leaderboardSortOptions } from './leaderboard';

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

	it('exposes supported all-time leaderboard sort options', () => {
		expect(leaderboardSortOptions).toEqual([
			{ value: 'winRate', label: 'Win rate' },
			{ value: 'averageGuessCount', label: 'Avg guesses' },
			{ value: 'wins', label: 'Wins' },
			{ value: 'totalAttempts', label: 'Attempts' },
			{ value: 'currentStreak', label: 'Current streak' },
			{ value: 'longestStreak', label: 'Longest streak' }
		]);
	});
});
