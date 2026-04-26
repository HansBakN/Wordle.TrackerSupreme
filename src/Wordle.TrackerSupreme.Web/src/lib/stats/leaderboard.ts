export type TodayLeaderboardEntryLike = {
	result?: string | null;
	guessCount?: number | null;
	playedInHardMode?: boolean | null;
};

export type LeaderboardSortKey =
	| 'winRate'
	| 'averageGuessCount'
	| 'wins'
	| 'totalAttempts'
	| 'currentStreak'
	| 'longestStreak';

export const leaderboardSortOptions: Array<{ value: LeaderboardSortKey; label: string }> = [
	{ value: 'winRate', label: 'Win rate' },
	{ value: 'averageGuessCount', label: 'Avg guesses' },
	{ value: 'wins', label: 'Wins' },
	{ value: 'totalAttempts', label: 'Attempts' },
	{ value: 'currentStreak', label: 'Current streak' },
	{ value: 'longestStreak', label: 'Longest streak' }
];

export function formatTodayLeaderboardMeta(entry: TodayLeaderboardEntryLike): string {
	const guessCount = entry.guessCount ?? 0;
	const guessLabel =
		entry.result === 'In progress'
			? `${guessCount} ${guessCount === 1 ? 'guess' : 'guesses'} so far`
			: `${guessCount} ${guessCount === 1 ? 'guess' : 'guesses'}`;
	const modeLabel = entry.playedInHardMode ? 'Hard mode' : 'Easy mode';
	return `${guessLabel} • ${modeLabel}`;
}
