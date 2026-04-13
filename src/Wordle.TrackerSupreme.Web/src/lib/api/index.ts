import { OpenAPI } from '$lib/api-client';

let baseConfigured = false;
let unauthorizedHandler: (() => void) | null = null;

const fallbackApiBase = (() => {
	if (typeof window === 'undefined') {
		return 'http://api:8080';
	}

	const origin = window.location.origin;
	const hostname = window.location.hostname;
	if (hostname === 'localhost' || hostname === '127.0.0.1') {
		return `http://${hostname}:8080`;
	}

	return origin;
})();

function ensureBaseConfigured() {
	if (baseConfigured) {
		return;
	}
	OpenAPI.BASE = (import.meta.env.VITE_API_BASE as string | undefined) ?? fallbackApiBase;
	baseConfigured = true;
}

export function configureApiClient() {
	ensureBaseConfigured();
	OpenAPI.TOKEN = undefined;
	OpenAPI.WITH_CREDENTIALS = true;
	OpenAPI.CREDENTIALS = 'include';
}

export function getApiBase() {
	ensureBaseConfigured();
	return OpenAPI.BASE;
}

export function registerUnauthorizedHandler(handler: (() => void) | null) {
	unauthorizedHandler = handler;
}

export function notifyUnauthorizedResponse(shouldNotify: boolean) {
	if (!shouldNotify) {
		return;
	}

	unauthorizedHandler?.();
}
