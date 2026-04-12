import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { get } from 'svelte/store';

const mockAuthService = {
	postApiAuthSignin: vi.fn(),
	postApiAuthSignup: vi.fn(),
	getApiAuthMe: vi.fn()
};
let unauthorizedHandler: (() => void) | null = null;

vi.mock('$lib/api-client/services/AuthService', () => ({
	AuthService: mockAuthService
}));

vi.mock('$lib/api', () => ({
	configureApiClient: vi.fn(),
	registerUnauthorizedHandler: vi.fn((handler: (() => void) | null) => {
		unauthorizedHandler = handler;
	})
}));

const { auth, signIn, signUp, signOut, bootstrapAuth } = await import('./store');

describe('auth store', () => {
	beforeEach(() => {
		localStorage.clear();
		vi.clearAllMocks();
		vi.spyOn(console, 'error').mockImplementation(() => {});
		auth.set({ user: null, token: null, ready: true });
	});

	afterEach(() => {
		localStorage.clear();
		vi.restoreAllMocks();
	});

	it('signIn stores token and user', async () => {
		mockAuthService.postApiAuthSignin.mockResolvedValue({
			token: 'abc123',
			player: {
				id: '1',
				displayName: 'Tester',
				email: 't@example.com',
				isAdmin: false,
				createdOn: ''
			}
		});

		await signIn('t@example.com', 'secret');

		expect(localStorage.getItem('wts_auth_token')).toBe('abc123');
		const state = get(auth);
		expect(state.user?.displayName).toBe('Tester');
		expect(state.token).toBe('abc123');
	});

	it('signUp stores token and user', async () => {
		mockAuthService.postApiAuthSignup.mockResolvedValue({
			token: 'xyz789',
			player: {
				id: '2',
				displayName: 'Newbie',
				email: 'n@example.com',
				isAdmin: false,
				createdOn: ''
			}
		});

		await signUp('Newbie', 'n@example.com', 'secret');

		expect(localStorage.getItem('wts_auth_token')).toBe('xyz789');
		expect(get(auth).user?.displayName).toBe('Newbie');
	});

	it('signOut clears token', () => {
		localStorage.setItem('wts_auth_token', 'keepme');
		signOut();
		expect(localStorage.getItem('wts_auth_token')).toBeNull();
		expect(get(auth).user).toBeNull();
	});

	it('bootstrapAuth clears expired token', async () => {
		localStorage.setItem('wts_auth_token', 'expired');
		mockAuthService.getApiAuthMe.mockRejectedValue(new Error('expired'));

		await bootstrapAuth();

		expect(localStorage.getItem('wts_auth_token')).toBeNull();
		expect(get(auth).user).toBeNull();
		expect(get(auth).token).toBeNull();
	});

	it('bootstrapAuth maps admin flag', async () => {
		localStorage.setItem('wts_auth_token', 'keep');
		mockAuthService.getApiAuthMe.mockResolvedValue({
			id: '99',
			displayName: 'Admin',
			email: 'admin@example.com',
			createdOn: '2025-01-01T00:00:00Z',
			isAdmin: true
		});

		await bootstrapAuth();

		expect(get(auth).user?.isAdmin).toBe(true);
	});

	it('unauthorized handler clears auth state and redirects to sign-in', () => {
		localStorage.setItem('wts_auth_token', 'keepme');
		auth.set({
			user: {
				id: '1',
				displayName: 'Tester',
				email: 'tester@example.com',
				createdOn: '',
				isAdmin: false
			},
			token: 'keepme',
			ready: true,
			error: null
		});

		unauthorizedHandler?.();

		expect(localStorage.getItem('wts_auth_token')).toBeNull();
		expect(get(auth).user).toBeNull();
		expect(get(auth).error).toBe('Session expired. Please sign in again.');
	});
});
