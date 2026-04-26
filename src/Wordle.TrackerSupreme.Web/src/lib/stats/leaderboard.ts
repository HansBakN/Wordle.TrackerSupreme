export const defaultLeaderboardMinimumGames = 10;

export function getLeaderboardMinimumGames(includeLowVolumePlayers: boolean): number {
	return includeLowVolumePlayers ? 0 : defaultLeaderboardMinimumGames;
}

export type TodayLeaderboardEntryLike = {
	result?: string | null;
	guessCount?: number | null;
	playedInHardMode?: boolean | null;
};

export function formatTodayLeaderboardMeta(entry: TodayLeaderboardEntryLike): string {
	const guessCount = entry.guessCount ?? 0;
	const guessLabel =
		entry.result === 'In progress'
			? `${guessCount} ${guessCount === 1 ? 'guess' : 'guesses'} so far`
			: `${guessCount} ${guessCount === 1 ? 'guess' : 'guesses'}`;
	const modeLabel = entry.playedInHardMode ? 'Hard mode' : 'Easy mode';
	return `${guessLabel} • ${modeLabel}`;
}
