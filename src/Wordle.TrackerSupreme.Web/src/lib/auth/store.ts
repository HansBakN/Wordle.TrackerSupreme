import { AuthService } from '$lib/api-client/services/AuthService';
import { configureApiClient, registerUnauthorizedHandler } from '$lib/api';
import { writable } from 'svelte/store';
import type { AuthResponse as ApiAuthResponse } from '$lib/api-client/models/AuthResponse';
import type { PlayerResponse as ApiPlayerResponse } from '$lib/api-client/models/PlayerResponse';
import type { AuthState, Player } from './types';

const TOKEN_STORAGE_KEY = 'wts_auth_token';
const PENDING_AUTH_ERROR_KEY = 'wts_auth_error';

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

function persistPendingAuthError(error: string | null) {
	if (typeof sessionStorage === 'undefined') {
		return;
	}

	if (error) {
		sessionStorage.setItem(PENDING_AUTH_ERROR_KEY, error);
	} else {
		sessionStorage.removeItem(PENDING_AUTH_ERROR_KEY);
	}
}

function consumePendingAuthError(): string | null {
	if (typeof sessionStorage === 'undefined') {
		return null;
	}

	const error = sessionStorage.getItem(PENDING_AUTH_ERROR_KEY);
	if (error) {
		sessionStorage.removeItem(PENDING_AUTH_ERROR_KEY);
	}

	return error;
}

function loadPendingAuthError(): string | null {
	if (typeof sessionStorage === 'undefined') {
		return null;
	}

	return sessionStorage.getItem(PENDING_AUTH_ERROR_KEY);
}

const initialToken = loadStoredToken();

export const auth = writable<AuthState>({
	user: null,
	token: initialToken,
	ready: false,
	error: null
});

configureApiClient(initialToken);

function setAuthState(
	token: string | null,
	user: Player | null,
	error: string | null,
	clearPendingAuthError = true
) {
	persistToken(token);
	configureApiClient(token);
	if (clearPendingAuthError) {
		persistPendingAuthError(null);
	}
	auth.set({
		user,
		token,
		ready: true,
		error
	});
}

function redirectToSignIn() {
	if (typeof window === 'undefined') {
		return;
	}

	const signInPath = '/signin';
	if (window.location.pathname === signInPath) {
		return;
	}

	window.location.assign(signInPath);
}

export function expireSession(message = 'Session expired. Please sign in again.') {
	persistPendingAuthError(message);
	setAuthState(null, null, message, false);
	redirectToSignIn();
}

registerUnauthorizedHandler(() => {
	expireSession();
});

function setAuthenticated(result: ApiAuthResponse) {
	setAuthState(result.token ?? null, mapPlayer(result.player), null);
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
		auth.set({ user: null, token: null, ready: true, error: consumePendingAuthError() });
		return;
	}

	configureApiClient(token);

	try {
		const player = await AuthService.getApiAuthMe();
		auth.set({ user: mapPlayer(player), token, ready: true, error: null });
	} catch (error) {
		console.error('Auth bootstrap failed', error);
		const pendingAuthError = loadPendingAuthError();
		setAuthState(
			null,
			null,
			pendingAuthError ?? 'Session expired. Please sign in.',
			pendingAuthError === null
		);
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
	setAuthState(null, null, null);
}
