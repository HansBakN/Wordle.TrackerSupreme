/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AuthResponse } from '../models/AuthResponse';
import type { PlayerResponse } from '../models/PlayerResponse';
import type { SignInRequest } from '../models/SignInRequest';
import type { SignUpRequest } from '../models/SignUpRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AuthService {
	/**
	 * @returns AuthResponse OK
	 * @throws ApiError
	 */
	public static postApiAuthSignup({
		requestBody
	}: {
		requestBody?: SignUpRequest;
	}): CancelablePromise<AuthResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/Auth/signup',
			body: requestBody,
			mediaType: 'application/json'
		});
	}
	/**
	 * @returns AuthResponse OK
	 * @throws ApiError
	 */
	public static postApiAuthSignin({
		requestBody
	}: {
		requestBody?: SignInRequest;
	}): CancelablePromise<AuthResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/Auth/signin',
			body: requestBody,
			mediaType: 'application/json'
		});
	}
	/**
	 * @returns PlayerResponse OK
	 * @throws ApiError
	 */
	public static getApiAuthMe(): CancelablePromise<PlayerResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/Auth/me'
		});
	}
}
