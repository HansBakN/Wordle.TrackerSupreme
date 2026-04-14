import { beforeEach, describe, expect, it, vi } from 'vitest';

const notifyUnauthorizedResponse = vi.fn();

vi.mock('$lib/api', () => ({
	notifyUnauthorizedResponse
}));

const { request } = await import('./request');

describe('generated api request', () => {
	beforeEach(() => {
		notifyUnauthorizedResponse.mockReset();
		vi.restoreAllMocks();
	});

	it('notifies the auth layer when an authenticated request returns 401', async () => {
		const mockResponse = {
			ok: false,
			status: 401,
			statusText: 'Unauthorized',
			headers: new Headers({ 'Content-Type': 'application/json' }),
			json: () => Promise.resolve({ message: 'Unauthorized' })
		} as Response;

		vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await expect(
			request(
				{
					BASE: 'http://api.test',
					VERSION: '1',
					WITH_CREDENTIALS: false,
					CREDENTIALS: 'include',
					TOKEN: 'abc123'
				},
				{
					method: 'GET',
					url: '/secure'
				}
			)
		).rejects.toThrowError('Unauthorized');

		expect(notifyUnauthorizedResponse).toHaveBeenCalledWith(true);
	});

	it('does not notify the auth layer when auth bootstrap returns 401', async () => {
		const mockResponse = {
			ok: false,
			status: 401,
			statusText: 'Unauthorized',
			headers: new Headers({ 'Content-Type': 'application/json' }),
			json: () => Promise.resolve({ message: 'Unauthorized' })
		} as Response;

		vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await expect(
			request(
				{
					BASE: 'http://api.test',
					VERSION: '1',
					WITH_CREDENTIALS: true,
					CREDENTIALS: 'include',
					TOKEN: undefined
				},
				{
					method: 'GET',
					url: '/api/Auth/me'
				}
			)
		).rejects.toThrowError('Unauthorized');

		expect(notifyUnauthorizedResponse).toHaveBeenCalledWith(false);
	});
});
