import { OpenAPI } from '$lib/api-client';

let baseConfigured = false;

const fallbackApiBase = (() => {
	if (typeof window === 'undefined') {
		return 'http://api:8080';
	}

	const origin = window.location.origin;
	if (origin.includes('localhost')) {
		return 'http://localhost:8080';
	}

	return origin;
})();

function ensureBaseConfigured() {
	if (baseConfigured) return;
	OpenAPI.BASE = (import.meta.env.VITE_API_BASE as string | undefined) ?? fallbackApiBase;
	baseConfigured = true;
}

export function configureApiClient(token: string | null) {
	ensureBaseConfigured();
	OpenAPI.TOKEN = token ?? undefined;
}

export function getApiBase() {
	ensureBaseConfigured();
	return OpenAPI.BASE;
}
