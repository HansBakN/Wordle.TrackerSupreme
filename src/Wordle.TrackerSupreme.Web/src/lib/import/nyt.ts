export function secondsUntilExpiry(expiresAt: string, now = Date.now()) {
	return Math.max(0, Math.ceil((new Date(expiresAt).getTime() - now) / 1000));
}

export function formatCountdown(seconds: number) {
	const minutes = Math.floor(seconds / 60);
	return `${minutes}:${String(seconds % 60).padStart(2, '0')}`;
}

export function hasUnavailableHistory(aggregateGamesPlayed?: number | null, requested = 0) {
	return aggregateGamesPlayed != null && aggregateGamesPlayed > requested;
}
