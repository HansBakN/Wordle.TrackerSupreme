import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const artifactRoot =
	process.env.E2E_ARTIFACT_DIR ?? path.resolve(__dirname, '../../artifacts/e2e/playwright');
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:3000';

export default defineConfig({
	testDir: './tests/e2e',
	timeout: 60_000,
	expect: {
		timeout: 10_000
	},
	retries: process.env.CI ? 1 : 0,
	workers: process.env.CI ? 2 : undefined,
	outputDir: path.join(artifactRoot, 'test-results'),
	reporter: [
		['list'],
		['html', { outputFolder: path.join(artifactRoot, 'report'), open: 'never' }]
	],
	use: {
		baseURL,
		headless: true,
		trace: 'on-first-retry',
		screenshot: 'only-on-failure',
		video: 'retain-on-failure',
		storageState: {
			cookies: [],
			origins: [
				{
					origin: baseURL,
					localStorage: [{ name: 'wts_hasSeenHowToPlay', value: '1' }]
				}
			]
		}
	},
	projects: [
		{
			name: 'chromium',
			use: { ...devices['Desktop Chrome'] }
		}
	]
});
