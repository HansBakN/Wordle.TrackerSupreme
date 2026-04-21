/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AdminCreatePuzzleRequest } from '../models/AdminCreatePuzzleRequest';
import type { AdminPlayerAttemptResponse } from '../models/AdminPlayerAttemptResponse';
import type { AdminPlayerDetailResponse } from '../models/AdminPlayerDetailResponse';
import type { AdminPlayerSummaryResponse } from '../models/AdminPlayerSummaryResponse';
import type { AdminPuzzleResponse } from '../models/AdminPuzzleResponse';
import type { AdminResetPasswordRequest } from '../models/AdminResetPasswordRequest';
import type { AdminSetAdminStatusRequest } from '../models/AdminSetAdminStatusRequest';
import type { AdminUpdateAttemptRequest } from '../models/AdminUpdateAttemptRequest';
import type { AdminUpdatePlayerRequest } from '../models/AdminUpdatePlayerRequest';
import type { AdminUpdatePuzzleRequest } from '../models/AdminUpdatePuzzleRequest';
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
	/**
	 * @returns AdminPuzzleResponse OK
	 * @throws ApiError
	 */
	public static getApiAdminPuzzles(): CancelablePromise<Array<AdminPuzzleResponse>> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/Admin/puzzles'
		});
	}
	/**
	 * @returns AdminPuzzleResponse Created
	 * @throws ApiError
	 */
	public static postApiAdminPuzzles({
		requestBody
	}: {
		requestBody?: AdminCreatePuzzleRequest;
	}): CancelablePromise<AdminPuzzleResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/Admin/puzzles',
			body: requestBody,
			mediaType: 'application/json',
			errors: {
				400: `Bad Request`,
				409: `Conflict`
			}
		});
	}
	/**
	 * @returns AdminPuzzleResponse OK
	 * @throws ApiError
	 */
	public static getApiAdminPuzzles1({
		puzzleId
	}: {
		puzzleId: string;
	}): CancelablePromise<AdminPuzzleResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/Admin/puzzles/{puzzleId}',
			path: {
				puzzleId: puzzleId
			}
		});
	}
	/**
	 * @returns AdminPuzzleResponse OK
	 * @throws ApiError
	 */
	public static putApiAdminPuzzles({
		puzzleId,
		requestBody
	}: {
		puzzleId: string;
		requestBody?: AdminUpdatePuzzleRequest;
	}): CancelablePromise<AdminPuzzleResponse> {
		return __request(OpenAPI, {
			method: 'PUT',
			url: '/api/Admin/puzzles/{puzzleId}',
			path: {
				puzzleId: puzzleId
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
	public static deleteApiAdminPuzzles({ puzzleId }: { puzzleId: string }): CancelablePromise<void> {
		return __request(OpenAPI, {
			method: 'DELETE',
			url: '/api/Admin/puzzles/{puzzleId}',
			path: {
				puzzleId: puzzleId
			},
			errors: {
				409: `Conflict`
			}
		});
	}
}
