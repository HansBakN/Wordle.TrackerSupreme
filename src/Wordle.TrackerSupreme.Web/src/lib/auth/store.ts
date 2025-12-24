import { AuthService } from '$lib/api-client/services/AuthService';
import { configureApiClient } from '$lib/api';
import { writable } from 'svelte/store';
import type { AuthResponse as ApiAuthResponse } from '$lib/api-client/models/AuthResponse';
import type { PlayerResponse as ApiPlayerResponse } from '$lib/api-client/models/PlayerResponse';
import type { AuthState, Player } from './types';

const TOKEN_STORAGE_KEY = 'wts_auth_token';

function loadStoredToken(): string | null {
	if (typeof localStorage === 'undefined') {
		return null;
	}
	return localStorage.getItem(TOKEN_STORAGE_KEY);
}

function persistToken(token: string | null) {
	if (typeof localStorage === 'undefined') {
		return;
	}
	if (token) {
		localStorage.setItem(TOKEN_STORAGE_KEY, token);
	} else {
		localStorage.removeItem(TOKEN_STORAGE_KEY);
	}
}

const initialToken = loadStoredToken();

export const auth = writable<AuthState>({
	user: null,
	token: initialToken,
	ready: false
});

configureApiClient(initialToken);

function setAuthenticated(result: ApiAuthResponse) {
	persistToken(result.token);
	configureApiClient(result.token);
	auth.set({
		user: mapPlayer(result.player),
		token: result.token,
		ready: true,
		error: null
	});
}

function mapPlayer(player: ApiPlayerResponse): Player {
	return {
		id: player.id ?? '',
		displayName: player.displayName ?? '',
		email: player.email ?? '',
		createdOn: player.createdOn ?? '',
		isAdmin: player.isAdmin ?? false
	};
}

export async function bootstrapAuth() {
	const token = loadStoredToken();
	if (!token) {
		auth.set({ user: null, token: null, ready: true });
		return;
	}

	configureApiClient(token);

	try {
		const player = await AuthService.getApiAuthMe();
		auth.set({ user: mapPlayer(player), token, ready: true, error: null });
	} catch (error) {
		console.error('Auth bootstrap failed', error);
		persistToken(null);
		auth.set({ user: null, token: null, ready: true, error: 'Session expired. Please sign in.' });
	}
}

export async function signIn(email: string, password: string) {
	const result = await AuthService.postApiAuthSignin({
		requestBody: { email, password }
	});
	setAuthenticated(result);
}

export async function signUp(displayName: string, email: string, password: string) {
	const result = await AuthService.postApiAuthSignup({
		requestBody: { displayName, email, password }
	});
	setAuthenticated(result);
}

export function signOut() {
	persistToken(null);
	configureApiClient(null);
	auth.set({ user: null, token: null, ready: true, error: null });
}
