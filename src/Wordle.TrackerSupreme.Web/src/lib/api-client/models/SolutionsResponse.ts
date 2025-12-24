/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { SolutionEntryResponse } from './SolutionEntryResponse';
export type SolutionsResponse = {
	puzzleDate?: string;
	solution?: string;
	cutoffPassed?: boolean;
	entries?: Array<SolutionEntryResponse>;
};
