/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable @typescript-eslint/no-explicit-any */
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class StatusService {
	/**
	 * @returns any OK
	 * @throws ApiError
	 */
	public static getApiStatus(): CancelablePromise<any> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/Status'
		});
	}
}
