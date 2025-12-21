import { getApiBase } from '$lib/api';
import { OpenAPI } from '$lib/api-client';
import type { GameStateResponse, PlayerStatsResponse, SolutionsResponse } from './types';

async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
	const base = getApiBase();
	const headers = new Headers(init.headers ?? {});
	headers.set('Content-Type', 'application/json');
	const token = OpenAPI.TOKEN;
	if (token) {
		headers.set('Authorization', `Bearer ${token}`);
	}

	const response = await fetch(`${base}${path}`, {
		...init,
		headers
	});

	if (!response.ok) {
		let message = 'Request failed';
		const bodyText = await response.text();
		if (bodyText) {
			try {
				const payload = JSON.parse(bodyText);
				message = payload?.message ?? message;
			} catch {
				message = bodyText;
			}
		}

		throw new Error(message || `${response.status} ${response.statusText}`);
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
	return apiFetch<PlayerStatsResponse>('/api/stats/me');
}
