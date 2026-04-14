import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { get } from 'svelte/store';

const mockAuthService = {
	postApiAuthSignin: vi.fn(),
	postApiAuthSignup: vi.fn(),
	getApiAuthMe: vi.fn()
};
let unauthorizedHandler: (() => void) | null = null;

const getApiBase = vi.fn(() => 'http://api.test');

vi.mock('$lib/api-client/services/AuthService', () => ({
	AuthService: mockAuthService
}));

vi.mock('$lib/api', () => ({
	configureApiClient: vi.fn(),
	getApiBase,
	registerUnauthorizedHandler: vi.fn((handler: (() => void) | null) => {
		unauthorizedHandler = handler;
	})
}));

const fetchMock = vi.fn();
vi.stubGlobal('fetch', fetchMock);

const { auth, signIn, signUp, signOut, bootstrapAuth } = await import('./store');

describe('auth store', () => {
	beforeEach(() => {
		sessionStorage.clear();
		vi.clearAllMocks();
		vi.spyOn(console, 'error').mockImplementation(() => {});
		auth.set({ user: null, token: null, ready: true, error: null });
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	it('signIn stores user without persisting a browser token', async () => {
		mockAuthService.postApiAuthSignin.mockResolvedValue({
			player: {
				id: '1',
				displayName: 'Tester',
				email: 't@example.com',
				isAdmin: false,
				createdOn: ''
			},
			token: null
		});

		await signIn('t@example.com', 'secret');

		const state = get(auth);
		expect(state.user?.displayName).toBe('Tester');
		expect(state.token).toBeNull();
		expect(localStorage.getItem('wts_auth_token')).toBeNull();
	});

	it('signUp stores user without persisting a browser token', async () => {
		mockAuthService.postApiAuthSignup.mockResolvedValue({
			player: {
				id: '2',
				displayName: 'Newbie',
				email: 'n@example.com',
				isAdmin: false,
				createdOn: ''
			},
			token: null
		});

		await signUp('Newbie', 'n@example.com', 'secret');

		expect(get(auth).user?.displayName).toBe('Newbie');
		expect(get(auth).token).toBeNull();
		expect(localStorage.getItem('wts_auth_token')).toBeNull();
	});

	it('signOut clears local auth state after calling the signout endpoint', async () => {
		fetchMock.mockResolvedValue({
			ok: true,
			status: 204
		});
		auth.set({
			user: {
				id: '1',
				displayName: 'Tester',
				email: 'tester@example.com',
				createdOn: '',
				isAdmin: false
			},
			token: null,
			ready: true,
			error: null
		});

		await signOut();

		expect(fetchMock).toHaveBeenCalledWith('http://api.test/api/auth/signout', {
			method: 'POST',
			credentials: 'include'
		});
		expect(get(auth).user).toBeNull();
		expect(get(auth).token).toBeNull();
	});

	it('bootstrapAuth preserves a pending expiry error for the redirected sign-in page', async () => {
		sessionStorage.setItem('wts_auth_error', 'Session expired. Please sign in again.');
		mockAuthService.getApiAuthMe.mockRejectedValue(new Error('Unauthorized'));

		await bootstrapAuth();

		expect(get(auth).error).toBe('Session expired. Please sign in again.');
		expect(sessionStorage.getItem('wts_auth_error')).toBe('Session expired. Please sign in again.');
	});

	it('bootstrapAuth treats unauthorized as a signed-out session', async () => {
		mockAuthService.getApiAuthMe.mockRejectedValue(new Error('Unauthorized'));

		await bootstrapAuth();

		expect(get(auth).user).toBeNull();
		expect(get(auth).token).toBeNull();
		expect(get(auth).error).toBeNull();
	});

	it('bootstrapAuth maps admin flag from the cookie-backed session', async () => {
		mockAuthService.getApiAuthMe.mockResolvedValue({
			id: '99',
			displayName: 'Admin',
			email: 'admin@example.com',
			createdOn: '2025-01-01T00:00:00Z',
			isAdmin: true
		});

		await bootstrapAuth();

		expect(get(auth).user?.isAdmin).toBe(true);
		expect(get(auth).token).toBeNull();
	});

	it('unauthorized handler clears auth state and redirects to sign-in', () => {
		auth.set({
			user: {
				id: '1',
				displayName: 'Tester',
				email: 'tester@example.com',
				createdOn: '',
				isAdmin: false
			},
			token: null,
			ready: true,
			error: null
		});

		unauthorizedHandler?.();

		expect(get(auth).user).toBeNull();
		expect(get(auth).error).toBe('Session expired. Please sign in again.');
	});

	it('unauthorized handler is a no-op when there is no active user session', () => {
		auth.set({ user: null, token: null, ready: true, error: null });

		unauthorizedHandler?.();

		expect(get(auth).user).toBeNull();
		expect(get(auth).error).toBeNull();
	});
});
