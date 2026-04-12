import { describe, expect, it } from 'vitest';
import { ApiError } from '$lib/api-client';
import { getApiErrorMessage } from './errors';

describe('getApiErrorMessage', () => {
	it('prefers explicit API message bodies', () => {
		const error = new ApiError(
			{ method: 'POST', url: '/test' },
			{
				url: '/test',
				ok: false,
				status: 429,
				statusText: 'Too Many Requests',
				body: { message: 'Slow down.' }
			},
			'Too Many Requests'
		);

		expect(getApiErrorMessage(error, 'Fallback')).toBe('Slow down.');
	});

	it('falls back to the error message when no API body message exists', () => {
		expect(getApiErrorMessage(new Error('Boom'), 'Fallback')).toBe('Boom');
	});
});
