/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AdminPlayerAttemptResponse } from './AdminPlayerAttemptResponse';
export type AdminPlayerDetailResponse = {
	id?: string;
	displayName?: string | null;
	email?: string | null;
	createdOn?: string;
	isAdmin?: boolean;
	attempts?: Array<AdminPlayerAttemptResponse>;
};
