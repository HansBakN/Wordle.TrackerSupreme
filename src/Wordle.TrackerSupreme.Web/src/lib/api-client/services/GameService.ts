/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { GameStateResponse } from '../models/GameStateResponse';
import type { PuzzleStream } from '../models/PuzzleStream';
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
	public static getApiGameState({
		stream
	}: {
		stream?: PuzzleStream;
	} = {}): CancelablePromise<GameStateResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/game/state',
			query: {
				stream: stream
			},
			errors: {
				503: `Service Unavailable`
			}
		});
	}
	/**
	 * @returns GameStateResponse OK
	 * @throws ApiError
	 */
	public static postApiGameGuess({
		stream,
		requestBody
	}: {
		stream?: PuzzleStream;
		requestBody?: SubmitGuessRequest;
	}): CancelablePromise<GameStateResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/game/guess',
			query: {
				stream: stream
			},
			body: requestBody,
			mediaType: 'application/json',
			errors: {
				400: `Bad Request`,
				409: `Conflict`,
				503: `Service Unavailable`
			}
		});
	}
	/**
	 * @returns GameStateResponse OK
	 * @throws ApiError
	 */
	public static postApiGameEasyMode({
		stream
	}: {
		stream?: PuzzleStream;
	} = {}): CancelablePromise<GameStateResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/game/easy-mode',
			query: {
				stream: stream
			},
			errors: {
				409: `Conflict`,
				503: `Service Unavailable`
			}
		});
	}
	/**
	 * @returns SolutionsResponse OK
	 * @throws ApiError
	 */
	public static getApiGameSolutions({
		stream
	}: {
		stream?: PuzzleStream;
	} = {}): CancelablePromise<SolutionsResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/game/solutions',
			query: {
				stream: stream
			},
			errors: {
				403: `Forbidden`,
				503: `Service Unavailable`
			}
		});
	}
}
