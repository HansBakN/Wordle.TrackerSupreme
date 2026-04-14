import { beforeEach, describe, expect, it, vi } from 'vitest';

import { OpenAPI } from '$lib/api-client';

async function loadApiModuleAt(origin: string) {
	vi.resetModules();
	OpenAPI.BASE = '';
	vi.stubGlobal('window', {
		location: {
			origin,
			hostname: new URL(origin).hostname
		}
	});
	return import('./index');
}

describe('api config', () => {
	beforeEach(() => {
		vi.unstubAllEnvs();
		vi.unstubAllGlobals();
	});

	it('uses localhost for the local API fallback when served from localhost', async () => {
		const { getApiBase } = await loadApiModuleAt('http://localhost:3000');

		expect(getApiBase()).toBe('http://localhost:8080');
	});

	it('preserves 127.0.0.1 for the local API fallback when served from 127.0.0.1', async () => {
		const { getApiBase } = await loadApiModuleAt('http://127.0.0.1:3000');

		expect(getApiBase()).toBe('http://127.0.0.1:8080');
	});

	it('prefers the configured API base when VITE_API_BASE is set', async () => {
		vi.stubEnv('VITE_API_BASE', 'https://api.example.test');
		const { getApiBase } = await loadApiModuleAt('http://127.0.0.1:3000');

		expect(getApiBase()).toBe('https://api.example.test');
	});
});
