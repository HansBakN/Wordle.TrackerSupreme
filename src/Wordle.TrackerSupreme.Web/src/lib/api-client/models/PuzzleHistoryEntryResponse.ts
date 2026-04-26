/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { GuessResponse } from './GuessResponse';
export type PuzzleHistoryEntryResponse = {
	puzzleDate: string;
	solution?: string | null;
	status: string;
	playedInHardMode: boolean;
	isAfterReveal: boolean;
	guessCount: number;
	guesses: Array<GuessResponse>;
};
