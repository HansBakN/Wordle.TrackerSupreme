import { getApiBase, notifyUnauthorizedResponse } from '$lib/api';
import { StatsService } from '$lib/api-client/services/StatsService';
import type {
	GameStateResponse,
	PlayerStatsResponse,
	PracticeStateResponse,
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
				message = payload?.message ?? message;
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

export function fetchGameState(): Promise<GameStateResponse> {
	return apiFetch<GameStateResponse>('/api/game/state');
}

export function submitGuess(guess: string): Promise<GameStateResponse> {
	return apiFetch<GameStateResponse>('/api/game/guess', {
		method: 'POST',
		body: JSON.stringify({ guess })
	});
}

export function enableEasyMode(): Promise<GameStateResponse> {
	return apiFetch<GameStateResponse>('/api/game/easy-mode', {
		method: 'POST'
	});
}

export function fetchSolutions(): Promise<SolutionsResponse> {
	return apiFetch<SolutionsResponse>('/api/game/solutions');
}

export function fetchMyStats(): Promise<PlayerStatsResponse> {
	return StatsService.getApiStatsMe();
}

export function startPracticeGame(): Promise<PracticeStateResponse> {
	return apiFetch<PracticeStateResponse>('/api/practice/start', { method: 'POST' });
}

export function fetchPracticeState(): Promise<PracticeStateResponse> {
	return apiFetch<PracticeStateResponse>('/api/practice/state');
}

export function submitPracticeGuess(guess: string): Promise<PracticeStateResponse> {
	return apiFetch<PracticeStateResponse>('/api/practice/guess', {
		method: 'POST',
		body: JSON.stringify({ guess })
	});
}
