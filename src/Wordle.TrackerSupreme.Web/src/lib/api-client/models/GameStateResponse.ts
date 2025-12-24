/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AttemptResponse } from './AttemptResponse';
export type GameStateResponse = {
	puzzleDate?: string;
	cutoffPassed?: boolean;
	solutionRevealed?: boolean;
	allowLatePlay?: boolean;
	wordLength?: number;
	maxGuesses?: number;
	isHardMode?: boolean;
	canGuess?: boolean;
	attempt?: AttemptResponse | null;
	solution?: string | null;
};
