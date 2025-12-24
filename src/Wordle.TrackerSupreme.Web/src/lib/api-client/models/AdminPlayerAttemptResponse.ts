/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { GuessResponse } from './GuessResponse';
export type AdminPlayerAttemptResponse = {
	attemptId?: string;
	puzzleDate?: string;
	status?: string;
	playedInHardMode?: boolean;
	createdOn?: string;
	completedOn?: string | null;
	guesses?: Array<GuessResponse>;
};
