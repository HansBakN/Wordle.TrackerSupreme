import type { PlayerStatsFilterRequest } from '$lib/api-client/models/PlayerStatsFilterRequest';

export type StatsFilterState = {
	includeHardMode: boolean;
	includeEasyMode: boolean;
	includeBeforeReveal: boolean;
	includeAfterReveal: boolean;
	includeSolved: boolean;
	includeFailed: boolean;
	includeInProgress: boolean;
	countPracticeAttempts: boolean;
	fromDate: string;
	toDate: string;
	minGuessCount: string | number;
	maxGuessCount: string | number;
};

export const defaultStatsFilterState: StatsFilterState = {
	includeHardMode: true,
	includeEasyMode: false,
	includeBeforeReveal: true,
	includeAfterReveal: false,
	includeSolved: true,
	includeFailed: true,
	includeInProgress: false,
	countPracticeAttempts: false,
	fromDate: '',
	toDate: '',
	minGuessCount: '',
	maxGuessCount: ''
};

function toOptionalNumber(value: string | number | null | undefined): number | undefined {
	if (value === null || value === undefined) {
		return undefined;
	}
	if (typeof value === 'number') {
		if (Number.isNaN(value)) {
			return undefined;
		}
		return value;
	}
	if (!value.trim()) {
		return undefined;
	}
	const parsed = Number.parseInt(value, 10);
	if (Number.isNaN(parsed)) {
		return undefined;
	}
	return parsed;
}

export function buildStatsFilterRequest(state: StatsFilterState): PlayerStatsFilterRequest {
	return {
		includeHardMode: state.includeHardMode,
		includeEasyMode: state.includeEasyMode,
		includeBeforeReveal: state.includeBeforeReveal,
		includeAfterReveal: state.includeAfterReveal,
		includeSolved: state.includeSolved,
		includeFailed: state.includeFailed,
		includeInProgress: state.includeInProgress,
		countPracticeAttempts: state.countPracticeAttempts,
		fromDate: state.fromDate || undefined,
		toDate: state.toDate || undefined,
		minGuessCount: toOptionalNumber(state.minGuessCount),
		maxGuessCount: toOptionalNumber(state.maxGuessCount)
	};
}
