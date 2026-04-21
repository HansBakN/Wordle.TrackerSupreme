/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { GuessResponse } from './GuessResponse';
import type { PuzzleStream } from './PuzzleStream';
export type AdminPlayerAttemptResponse = {
	attemptId?: string;
	puzzleDate?: string;
	stream?: PuzzleStream;
	status?: string;
	playedInHardMode?: boolean;
	createdOn?: string;
	completedOn?: string | null;
	guesses?: Array<GuessResponse>;
};
