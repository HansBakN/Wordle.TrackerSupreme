/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { NytImportRecordResponse } from './NytImportRecordResponse';
export type NytImportResponse = {
	requested?: number;
	imported?: number;
	duplicates?: number;
	conflicts?: number;
	rejected?: number;
	missingFromNyt?: number;
	results?: Array<NytImportRecordResponse> | null;
};
