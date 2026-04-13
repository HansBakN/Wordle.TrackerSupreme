import { AuthService } from '$lib/api-client/services/AuthService';
import { configureApiClient, getApiBase, registerUnauthorizedHandler } from '$lib/api';
import { writable } from 'svelte/store';
import type { AuthResponse as ApiAuthResponse } from '$lib/api-client/models/AuthResponse';
import type { PlayerResponse as ApiPlayerResponse } from '$lib/api-client/models/PlayerResponse';
import type { AuthState, Player } from './types';

const PENDING_AUTH_ERROR_KEY = 'wts_auth_error';

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

function loadPendingAuthError(): string | null {
	if (typeof sessionStorage === 'undefined') {
		return null;
	}

	return sessionStorage.getItem(PENDING_AUTH_ERROR_KEY);
}

export const auth = writable<AuthState>({
	user: null,
	token: null,
	ready: false,
	error: null
});

configureApiClient();

function setAuthState(user: Player | null, error: string | null, clearPendingAuthError = true) {
	configureApiClient();
	if (clearPendingAuthError) {
		persistPendingAuthError(null);
	}

	auth.set({
		user,
		token: null,
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
	setAuthState(null, message, false);
	redirectToSignIn();
}

registerUnauthorizedHandler(() => {
	expireSession();
});

function setAuthenticated(result: ApiAuthResponse) {
	setAuthState(mapPlayer(result.player), null);
}

function mapPlayer(player: ApiPlayerResponse | undefined): Player | null {
	if (!player) {
		return null;
	}

	return {
		id: player.id ?? '',
		displayName: player.displayName ?? '',
		email: player.email ?? '',
		createdOn: player.createdOn ?? '',
		isAdmin: player.isAdmin ?? false
	};
}

export async function bootstrapAuth() {
	try {
		const player = await AuthService.getApiAuthMe();
		setAuthState(mapPlayer(player), null);
	} catch (error) {
		const pendingAuthError = loadPendingAuthError();
		if (error instanceof Error && error.message === 'Unauthorized') {
			setAuthState(null, pendingAuthError, pendingAuthError === null);
			return;
		}

		console.error('Auth bootstrap failed', error);
		setAuthState(
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

export async function signOut() {
	try {
		await fetch(`${getApiBase()}/api/auth/signout`, {
			method: 'POST',
			credentials: 'include'
		});
	} catch (error) {
		console.error('Sign out request failed', error);
	}

	setAuthState(null, null);
}
