/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AdminPlayerAttemptResponse } from '../models/AdminPlayerAttemptResponse';
import type { AdminPlayerDetailResponse } from '../models/AdminPlayerDetailResponse';
import type { AdminPlayerSummaryResponse } from '../models/AdminPlayerSummaryResponse';
import type { AdminPlayersPageResponse } from '../models/AdminPlayersPageResponse';
import type { AdminResetPasswordRequest } from '../models/AdminResetPasswordRequest';
import type { AdminSetAdminStatusRequest } from '../models/AdminSetAdminStatusRequest';
import type { AdminUpdateAttemptRequest } from '../models/AdminUpdateAttemptRequest';
import type { AdminUpdatePlayerRequest } from '../models/AdminUpdatePlayerRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AdminService {
	/**
	 * @returns AdminPlayersPageResponse OK
	 * @throws ApiError
	 */
	public static getApiAdminPlayers({
		search,
		page,
		pageSize
	}: {
		search?: string;
		page?: number;
		pageSize?: number;
	} = {}): CancelablePromise<AdminPlayersPageResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/Admin/players',
			query: {
				search,
				page,
				pageSize
			}
		});
	}
	/**
	 * @returns AdminPlayerDetailResponse OK
	 * @throws ApiError
	 */
	public static getApiAdminPlayers1({
		playerId
	}: {
		playerId: string;
	}): CancelablePromise<AdminPlayerDetailResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/Admin/players/{playerId}',
			path: {
				playerId: playerId
			}
		});
	}
	/**
	 * @returns AdminPlayerDetailResponse OK
	 * @throws ApiError
	 */
	public static putApiAdminPlayers({
		playerId,
		requestBody
	}: {
		playerId: string;
		requestBody?: AdminUpdatePlayerRequest;
	}): CancelablePromise<AdminPlayerDetailResponse> {
		return __request(OpenAPI, {
			method: 'PUT',
			url: '/api/Admin/players/{playerId}',
			path: {
				playerId: playerId
			},
			body: requestBody,
			mediaType: 'application/json',
			errors: {
				400: `Bad Request`,
				409: `Conflict`
			}
		});
	}
	/**
	 * @returns void
	 * @throws ApiError
	 */
	public static putApiAdminPlayersPassword({
		playerId,
		requestBody
	}: {
		playerId: string;
		requestBody?: AdminResetPasswordRequest;
	}): CancelablePromise<void> {
		return __request(OpenAPI, {
			method: 'PUT',
			url: '/api/Admin/players/{playerId}/password',
			path: {
				playerId: playerId
			},
			body: requestBody,
			mediaType: 'application/json',
			errors: {
				400: `Bad Request`,
				409: `Conflict`
			}
		});
	}
	/**
	 * @returns AdminPlayerDetailResponse OK
	 * @throws ApiError
	 */
	public static putApiAdminPlayersAdmin({
		playerId,
		requestBody
	}: {
		playerId: string;
		requestBody?: AdminSetAdminStatusRequest;
	}): CancelablePromise<AdminPlayerDetailResponse> {
		return __request(OpenAPI, {
			method: 'PUT',
			url: '/api/Admin/players/{playerId}/admin',
			path: {
				playerId: playerId
			},
			body: requestBody,
			mediaType: 'application/json',
			errors: {
				400: `Bad Request`,
				409: `Conflict`
			}
		});
	}
	/**
	 * @returns AdminPlayerAttemptResponse OK
	 * @throws ApiError
	 */
	public static putApiAdminAttempts({
		attemptId,
		requestBody
	}: {
		attemptId: string;
		requestBody?: AdminUpdateAttemptRequest;
	}): CancelablePromise<AdminPlayerAttemptResponse> {
		return __request(OpenAPI, {
			method: 'PUT',
			url: '/api/Admin/attempts/{attemptId}',
			path: {
				attemptId: attemptId
			},
			body: requestBody,
			mediaType: 'application/json',
			errors: {
				400: `Bad Request`,
				409: `Conflict`
			}
		});
	}
	/**
	 * @returns void
	 * @throws ApiError
	 */
	public static deleteApiAdminAttempts({
		attemptId
	}: {
		attemptId: string;
	}): CancelablePromise<void> {
		return __request(OpenAPI, {
			method: 'DELETE',
			url: '/api/Admin/attempts/{attemptId}',
			path: {
				attemptId: attemptId
			}
		});
	}
}
