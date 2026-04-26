/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AdminPlayerSummaryResponse } from './AdminPlayerSummaryResponse';
export type AdminPlayersPageResponse = {
	players?: Array<AdminPlayerSummaryResponse> | null;
	totalCount: number;
	page: number;
	pageSize: number;
};
