export const defaultLeaderboardMinimumGames = 10;

export function getLeaderboardMinimumGames(includeLowVolumePlayers: boolean): number {
	return includeLowVolumePlayers ? 0 : defaultLeaderboardMinimumGames;
}
