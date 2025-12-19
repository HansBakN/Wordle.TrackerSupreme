import { AuthService } from '$lib/api-client/services/AuthService';
import { configureApiClient } from '$lib/api';
import { get, writable } from 'svelte/store';
import type { AuthResponse, AuthState } from './types';

const TOKEN_STORAGE_KEY = 'wts_auth_token';

const initialToken = loadStoredToken();

function loadStoredToken(): string | null {
  if (typeof localStorage === 'undefined') return null;

  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  } catch (error) {
    console.error('Failed to load auth token from storage', error);
    return null;
  }
}

function persistToken(token: string | null) {
  if (typeof localStorage === 'undefined') return;

  try {
    if (token) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }
  } catch (error) {
    console.error('Failed to persist auth token', error);
  }
}

export const auth = writable<AuthState>({
  user: null,
  token: initialToken,
  ready: false,
  error: null
});

configureApiClient(initialToken);

function setAuthenticated(result: AuthResponse) {
  persistToken(result.token);
  configureApiClient(result.token);
  auth.set({
    user: result.player,
    token: result.token,
    ready: true,
    error: null
  });
}

function clearAuth(error: string | null = null) {
  persistToken(null);
  configureApiClient(null);
  auth.set({ user: null, token: null, ready: true, error });
}

export async function bootstrapAuth() {
  const token = get(auth).token;
  if (!token) {
    clearAuth();
    return;
  }

  configureApiClient(token);

  try {
    const player = await AuthService.getApiAuthMe();
    auth.set({ user: player, token, ready: true, error: null });
  } catch (error) {
    console.error('Auth bootstrap failed', error);
    clearAuth('Session expired. Please sign in.');
  }
}

export async function signIn(email: string, password: string) {
  const result = await AuthService.postApiAuthSignin({
    requestBody: { email, password }
  });
  setAuthenticated(result);
}

export async function signUp(displayName: string, email: string, password: string) {
  const result = await AuthService.postApiAuthSignup({ requestBody: { displayName, email, password } });
  setAuthenticated(result);
}

export function signOut() {
  clearAuth();
}
