/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { LeaderboardPageResponse } from '../models/LeaderboardPageResponse';
import type { PlayerStatsEntryResponse } from '../models/PlayerStatsEntryResponse';
import type { PlayerStatsFilterRequest } from '../models/PlayerStatsFilterRequest';
import type { PlayerStatsResponse } from '../models/PlayerStatsResponse';
import type { TodayLeaderboardEntryResponse } from '../models/TodayLeaderboardEntryResponse';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class StatsService {
	/**
	 * @returns PlayerStatsResponse OK
	 * @throws ApiError
	 */
	public static getApiStatsMe(): CancelablePromise<PlayerStatsResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/stats/me'
		});
	}
	/**
	 * @returns PlayerStatsEntryResponse OK
	 * @throws ApiError
	 */
	public static postApiStatsPlayers({
		requestBody
	}: {
		requestBody?: PlayerStatsFilterRequest;
	}): CancelablePromise<Array<PlayerStatsEntryResponse>> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/stats/players',
			body: requestBody,
			mediaType: 'application/json'
		});
	}
	/**
	 * @returns LeaderboardPageResponse OK
	 * @throws ApiError
	 */
	public static getApiStatsLeaderboard({
		page = 1,
		pageSize = 10,
		sort = 'winRate',
		direction = 'desc'
	}: {
		page?: number;
		pageSize?: number;
		sort?: string;
		direction?: string;
	}): CancelablePromise<LeaderboardPageResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/stats/leaderboard',
			query: {
				page: page,
				pageSize: pageSize,
				sort: sort,
				direction: direction
			}
		});
	}
	/**
	 * @returns TodayLeaderboardEntryResponse OK
	 * @throws ApiError
	 */
	public static getApiStatsLeaderboardToday(): CancelablePromise<
		Array<TodayLeaderboardEntryResponse>
	> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/stats/leaderboard/today'
		});
	}
}
