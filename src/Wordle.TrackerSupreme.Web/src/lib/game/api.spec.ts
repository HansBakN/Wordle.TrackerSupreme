import { beforeEach, describe, expect, it, vi } from 'vitest';

const notifyUnauthorizedResponse = vi.fn();

vi.mock('$lib/api', () => ({
	getApiBase: () => 'http://api.test',
	notifyUnauthorizedResponse
}));

// Lazy import after mocks so the module picks them up.
const { ApiResponseError, enableEasyMode, fetchGameState, submitGuess } = await import('./api');

describe('api helpers', () => {
	beforeEach(() => {
		notifyUnauthorizedResponse.mockReset();
		vi.restoreAllMocks();
	});

	it('sends credentialed requests without a bearer header', async () => {
		const mockResponse = {
			ok: true,
			status: 200,
			json: () => Promise.resolve({ puzzleDate: '2025-01-01' })
		} as Response;
		const fetchSpy = vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await fetchGameState();

		const call = fetchSpy.mock.calls[0];
		const init = call[1] as RequestInit;
		const headers = init?.headers as Headers;
		expect(init.credentials).toBe('include');
		expect(headers.get('Authorization')).toBeNull();
	});

	it('throws error message from payload when request fails', async () => {
		const mockResponse = {
			ok: false,
			status: 400,
			statusText: 'Bad Request',
			text: () => Promise.resolve(JSON.stringify({ message: 'Nope' }))
		} as Response;

		vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await expect(submitGuess('crane')).rejects.toThrowError('Nope');
	});

	it('notifies the auth layer when a credentialed request returns 401', async () => {
		const mockResponse = {
			ok: false,
			status: 401,
			statusText: 'Unauthorized',
			text: () => Promise.resolve(JSON.stringify({ message: 'Unauthorized' }))
		} as Response;

		vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await expect(fetchGameState()).rejects.toThrowError('Unauthorized');
		expect(notifyUnauthorizedResponse).toHaveBeenCalledWith(true);
	});

	it('posts to easy mode endpoint', async () => {
		const mockResponse = {
			ok: true,
			status: 200,
			json: () => Promise.resolve({ puzzleDate: '2025-01-01', isHardMode: false })
		} as Response;
		const fetchSpy = vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await enableEasyMode();

		const call = fetchSpy.mock.calls[0];
		expect(call[0]).toBe('http://api.test/api/game/easy-mode');
		expect((call[1] as RequestInit)?.method).toBe('POST');
	});

	it('throws ApiResponseError with code when response has a code field', async () => {
		const mockResponse = {
			ok: false,
			status: 503,
			statusText: 'Service Unavailable',
			text: () =>
				Promise.resolve(JSON.stringify({ code: 'puzzle_unavailable', message: 'No puzzle today.' }))
		} as Response;

		vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		const err = await fetchGameState().catch((e) => e);
		expect(err).toBeInstanceOf(ApiResponseError);
		expect((err as ApiResponseError).status).toBe(503);
		expect((err as ApiResponseError).code).toBe('puzzle_unavailable');
		expect((err as ApiResponseError).message).toBe('No puzzle today.');
	});

	it('reads detail from ProblemDetails 503 response when detail and code are both present', async () => {
		const mockResponse = {
			ok: false,
			status: 503,
			statusText: 'Service Unavailable',
			text: () =>
				Promise.resolve(
					JSON.stringify({
						type: 'https://tools.ietf.org/html/rfc9110#section-15.6.4',
						title: 'Service Unavailable',
						status: 503,
						detail: 'No puzzle available today.',
						code: 'puzzle_unavailable'
					})
				)
		} as Response;

		vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		const err = await fetchGameState().catch((e) => e);
		expect(err).toBeInstanceOf(ApiResponseError);
		expect((err as ApiResponseError).status).toBe(503);
		expect((err as ApiResponseError).code).toBe('puzzle_unavailable');
		expect((err as ApiResponseError).message).toBe('No puzzle available today.');
	});

	it('throws error detail from ProblemDetails payload when request fails', async () => {
		const mockResponse = {
			ok: false,
			status: 409,
			statusText: 'Conflict',
			text: () =>
				Promise.resolve(
					JSON.stringify({
						type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
						title: 'Conflict',
						status: 409,
						detail: 'You already have an attempt for today.'
					})
				)
		} as Response;

		vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await expect(submitGuess('crane')).rejects.toThrowError('You already have an attempt for today.');
	});

	it('submits guesses with a JSON payload', async () => {
		const mockResponse = {
			ok: true,
			status: 200,
			json: () => Promise.resolve({ puzzleDate: '2025-01-01' })
		} as Response;
		const fetchSpy = vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await submitGuess('CRANE');

		const [, init] = fetchSpy.mock.calls[0];
		expect((init as RequestInit)?.method).toBe('POST');
		expect((init as RequestInit)?.body).toBe(JSON.stringify({ guess: 'CRANE' }));
	});
});
