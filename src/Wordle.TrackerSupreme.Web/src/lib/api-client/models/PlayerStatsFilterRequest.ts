/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
export type PlayerStatsFilterRequest = {
	includeHardMode?: boolean;
	includeEasyMode?: boolean;
	includeBeforeReveal?: boolean;
	includeAfterReveal?: boolean;
	includeSolved?: boolean;
	includeFailed?: boolean;
	includeInProgress?: boolean;
	countPracticeAttempts?: boolean;
	fromDate?: string | null;
	toDate?: string | null;
	minGuessCount?: number | null;
	maxGuessCount?: number | null;
};
