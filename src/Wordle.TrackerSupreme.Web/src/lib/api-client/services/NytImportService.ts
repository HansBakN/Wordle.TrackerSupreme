/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { NytImportResponse } from '../models/NytImportResponse';
import type { NytImportSessionResponse } from '../models/NytImportSessionResponse';
import type { NytImportSessionStatusResponse } from '../models/NytImportSessionStatusResponse';
import type { NytImportSubmissionRequest } from '../models/NytImportSubmissionRequest';
import type { NytPuzzleCatalogueEntryResponse } from '../models/NytPuzzleCatalogueEntryResponse';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class NytImportService {
	/**
	 * @returns NytImportSessionResponse OK
	 * @throws ApiError
	 */
	public static postApiImportNytSession(): CancelablePromise<NytImportSessionResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/import/nyt/session'
		});
	}
	/**
	 * @returns NytImportSessionStatusResponse OK
	 * @throws ApiError
	 */
	public static getApiImportNytSession({
		sessionId
	}: {
		sessionId: string;
	}): CancelablePromise<NytImportSessionStatusResponse> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/import/nyt/session/{sessionId}',
			path: {
				sessionId: sessionId
			}
		});
	}
	/**
	 * @returns NytPuzzleCatalogueEntryResponse OK
	 * @throws ApiError
	 */
	public static getApiImportNytCatalogue({
		importCode
	}: {
		importCode?: string;
	}): CancelablePromise<Array<NytPuzzleCatalogueEntryResponse>> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/import/nyt/catalogue',
			query: {
				importCode: importCode
			}
		});
	}
	/**
	 * @returns NytImportResponse OK
	 * @throws ApiError
	 */
	public static postApiImportNyt({
		requestBody
	}: {
		requestBody?: NytImportSubmissionRequest;
	}): CancelablePromise<NytImportResponse> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/import/nyt',
			body: requestBody,
			mediaType: 'application/json'
		});
	}
}
