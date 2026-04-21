/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { LeaderboardEntryResponse } from './LeaderboardEntryResponse';
export type LeaderboardPageResponse = {
	streams?: Array<'TrackerSupreme' | 'NewYorkTimes'> | null;
	items?: Array<LeaderboardEntryResponse> | null;
	total?: number;
	page?: number;
	pageSize?: number;
	totalPages?: number;
};
