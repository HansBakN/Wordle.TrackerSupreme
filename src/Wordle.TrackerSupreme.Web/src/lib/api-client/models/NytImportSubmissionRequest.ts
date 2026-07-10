/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { NytImportStateRequest } from './NytImportStateRequest';
export type NytImportSubmissionRequest = {
	schemaVersion?: number;
	importCode: string;
	aggregateGamesPlayed?: number | null;
	states: Array<NytImportStateRequest | null>;
};
