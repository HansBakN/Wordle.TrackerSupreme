/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AdminPlayerAttemptResponse } from '../models/AdminPlayerAttemptResponse';
import type { AdminPlayerDetailResponse } from '../models/AdminPlayerDetailResponse';
import type { AdminPlayerSummaryResponse } from '../models/AdminPlayerSummaryResponse';
import type { AdminResetPasswordRequest } from '../models/AdminResetPasswordRequest';
import type { AdminSetAdminStatusRequest } from '../models/AdminSetAdminStatusRequest';
import type { AdminUpdateAttemptRequest } from '../models/AdminUpdateAttemptRequest';
import type { AdminUpdatePlayerRequest } from '../models/AdminUpdatePlayerRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AdminService {
	/**
	 * @returns AdminPlayerSummaryResponse OK
	 * @throws ApiError
	 */
	public static getApiAdminPlayers(): CancelablePromise<Array<AdminPlayerSummaryResponse>> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/Admin/players'
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
			mediaType: 'application/json'
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
			mediaType: 'application/json'
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
			mediaType: 'application/json'
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
			mediaType: 'application/json'
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
