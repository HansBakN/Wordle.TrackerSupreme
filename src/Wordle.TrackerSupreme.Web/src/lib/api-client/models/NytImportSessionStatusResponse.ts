/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
export type NytImportSessionStatusResponse = {
	sessionId: string;
	expiresAt: string;
	completedAt?: string | null;
	aggregateGamesPlayed?: number | null;
	requested: number;
	imported: number;
	duplicates: number;
	conflicts: number;
	rejected: number;
	missingFromNyt: number;
};
