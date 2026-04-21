/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PracticeStateResponse } from '../models/PracticeStateResponse';
import type { SubmitGuessRequest } from '../models/SubmitGuessRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PracticeService {
	/**
	 * @returns PracticeStateResponse OK
	 * @throws ApiError
	 */
	public static postApiPracticeStart(): CancelablePromise<PracticeStateResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/practice/start'
		});
	}
	/**
	 * @returns PracticeStateResponse OK
	 * @throws ApiError
	 */
	public static getApiPracticeState(): CancelablePromise<PracticeStateResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/practice/state'
		});
	}
	/**
	 * @returns PracticeStateResponse OK
	 * @throws ApiError
	 */
	public static postApiPracticeGuess({
		requestBody
	}: {
		requestBody?: SubmitGuessRequest;
	}): CancelablePromise<PracticeStateResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/practice/guess',
			body: requestBody,
			mediaType: 'application/json'
		});
	}
}
