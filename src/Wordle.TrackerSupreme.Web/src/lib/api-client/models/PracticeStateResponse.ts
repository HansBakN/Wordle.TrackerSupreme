/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AttemptResponse } from './AttemptResponse';
export type PracticeStateResponse = {
	puzzleId: string;
	solutionRevealed: boolean;
	wordLength: number;
	maxGuesses: number;
	canGuess: boolean;
	attempt?: AttemptResponse | null;
	solution?: string | null;
};
