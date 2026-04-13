import { beforeEach, describe, expect, it, vi } from 'vitest';

const notifyUnauthorizedResponse = vi.fn();

vi.mock('$lib/api', () => ({
	getApiBase: () => 'http://api.test',
	notifyUnauthorizedResponse
}));

const openApiMock = { TOKEN: undefined as string | undefined };
vi.mock('$lib/api-client', () => ({
	OpenAPI: openApiMock
}));

// Lazy import after mocks so the module picks them up.
const { ApiResponseError, enableEasyMode, fetchGameState, submitGuess } = await import('./api');

describe('api helpers', () => {
	beforeEach(() => {
		openApiMock.TOKEN = undefined;
		notifyUnauthorizedResponse.mockReset();
		vi.restoreAllMocks();
	});

	it('includes bearer token when present', async () => {
		openApiMock.TOKEN = 'abc123';
		const mockResponse = {
			ok: true,
			status: 200,
			json: () => Promise.resolve({ puzzleDate: '2025-01-01' })
		} as Response;
		const fetchSpy = vi.spyOn(globalThis, 'fetch' as never).mockResolvedValue(mockResponse);

		await fetchGameState();

		const call = fetchSpy.mock.calls[0];
		const headers = (call[1] as RequestInit)?.headers as Headers;
		expect(headers.get('Authorization')).toBe('Bearer abc123');
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

	it('notifies the auth layer when an authenticated request returns 401', async () => {
		openApiMock.TOKEN = 'abc123';
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
