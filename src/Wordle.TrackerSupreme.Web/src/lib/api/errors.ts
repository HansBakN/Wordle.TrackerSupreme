import { ApiError } from '$lib/api-client';

export function getApiErrorMessage(error: unknown, fallback: string): string {
	if (error instanceof ApiError) {
		const body = error.body as
			| { message?: string; detail?: string; title?: string; errors?: Record<string, string[]> }
			| undefined;

		if (body?.message) {
			return body.message;
		}

		if (body?.detail) {
			return body.detail;
		}

		const validationMessages = body?.errors
			? Object.values(body.errors).flat().filter(Boolean)
			: [];
		if (validationMessages.length > 0) {
			return validationMessages.join(' ');
		}

		if (body?.title) {
			return body.title;
		}
	}

	return error instanceof Error ? error.message : fallback;
}
