import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';

const artifactRoot =
	process.env.E2E_ARTIFACT_DIR ?? path.resolve(__dirname, '../../artifacts/e2e/playwright');

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
		baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
		headless: true,
		trace: 'on-first-retry',
		screenshot: 'only-on-failure',
		video: 'retain-on-failure'
	},
	projects: [
		{
			name: 'chromium',
			use: { ...devices['Desktop Chrome'] }
		}
	]
});
