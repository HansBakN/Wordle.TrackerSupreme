/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { GameStateResponse } from '../models/GameStateResponse';
import type { SolutionsResponse } from '../models/SolutionsResponse';
import type { SubmitGuessRequest } from '../models/SubmitGuessRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class GameService {
	/**
	 * @returns GameStateResponse OK
	 * @throws ApiError
	 */
	public static getApiGameState(): CancelablePromise<GameStateResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/game/state'
		});
	}
	/**
	 * @returns GameStateResponse OK
	 * @throws ApiError
	 */
	public static postApiGameGuess({
		requestBody
	}: {
		requestBody?: SubmitGuessRequest;
	}): CancelablePromise<GameStateResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/game/guess',
			body: requestBody,
			mediaType: 'application/json'
		});
	}
	/**
	 * @returns GameStateResponse OK
	 * @throws ApiError
	 */
	public static postApiGameEasyMode(): CancelablePromise<GameStateResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/game/easy-mode'
		});
	}
	/**
	 * @returns SolutionsResponse OK
	 * @throws ApiError
	 */
	public static getApiGameSolutions(): CancelablePromise<SolutionsResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/game/solutions',
			errors: {
				403: `Forbidden`
			}
		});
	}
}
