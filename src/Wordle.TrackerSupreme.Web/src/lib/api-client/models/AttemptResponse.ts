/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { GuessResponse } from './GuessResponse';
export type AttemptResponse = {
  attemptId?: string;
  status?: string;
  isAfterReveal?: boolean;
  createdOn?: string;
  completedOn?: string | null;
  guesses?: Array<GuessResponse>;
};

