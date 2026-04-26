import { getApiBase, notifyUnauthorizedResponse } from '$lib/api';
import { StatsService } from '$lib/api-client/services/StatsService';
import type {
	GameStateResponse,
	PlayerStatsResponse,
	PuzzleStream,
	SolutionsResponse
} from './types';

export class ApiResponseError extends Error {
	readonly status: number;
	readonly code: string | undefined;

	constructor(message: string, status: number, code?: string) {
		super(message);
		this.status = status;
		this.code = code;
	}
}

async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
	const base = getApiBase();
	const headers = new Headers(init.headers ?? {});
	headers.set('Content-Type', 'application/json');

	const response = await fetch(`${base}${path}`, {
		...init,
		headers,
		credentials: 'include'
	});

	notifyUnauthorizedResponse(response.status === 401);

	if (!response.ok) {
		let message = 'Request failed';
		let code: string | undefined;
		const bodyText = await response.text();
		if (bodyText) {
			try {
				const payload = JSON.parse(bodyText);
				message = payload?.detail ?? payload?.message ?? message;
				code = payload?.code;
			} catch {
				message = bodyText;
			}
		}

		throw new ApiResponseError(
			message || `${response.status} ${response.statusText}`,
			response.status,
			code
		);
	}

	if (response.status === 204) {
		return undefined as T;
	}

	return (await response.json()) as T;
}

function withStream(path: string, stream: PuzzleStream): string {
	if (stream === 'TrackerSupreme') {
		return path;
	}

	return `${path}?stream=${encodeURIComponent(stream)}`;
}

export function fetchGameState(
	stream: PuzzleStream = 'TrackerSupreme'
): Promise<GameStateResponse> {
	return apiFetch<GameStateResponse>(withStream('/api/game/state', stream));
}

export function submitGuess(
	guess: string,
	stream: PuzzleStream = 'TrackerSupreme'
): Promise<GameStateResponse> {
	return apiFetch<GameStateResponse>(withStream('/api/game/guess', stream), {
		method: 'POST',
		body: JSON.stringify({ guess })
	});
}

export function enableEasyMode(
	stream: PuzzleStream = 'TrackerSupreme'
): Promise<GameStateResponse> {
	return apiFetch<GameStateResponse>(withStream('/api/game/easy-mode', stream), {
		method: 'POST'
	});
}

export function fetchSolutions(
	stream: PuzzleStream = 'TrackerSupreme'
): Promise<SolutionsResponse> {
	return apiFetch<SolutionsResponse>(withStream('/api/game/solutions', stream));
}

export function fetchMyStats(): Promise<PlayerStatsResponse> {
	return StatsService.getApiStatsMe();
}
