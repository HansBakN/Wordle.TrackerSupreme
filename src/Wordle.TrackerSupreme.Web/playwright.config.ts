import type { PlaywrightTestConfig } from '@playwright/test';
import { devices } from '@playwright/test';

const config: PlaywrightTestConfig = {
	testDir: './tests/ui',
	timeout: 60_000,
	expect: {
		timeout: 10_000
	},
	use: {
		baseURL: 'http://localhost:4173',
		trace: 'on-first-retry'
	},
	projects: [
		{
			name: 'chromium',
			use: { ...devices['Desktop Chrome'] }
		}
	],
	webServer: {
		command: 'npm run dev -- --host --port 4173 --mode test',
		url: 'http://localhost:4173',
		reuseExistingServer: true,
		timeout: 120_000,
		env: {
			VITE_API_BASE: 'http://127.0.0.1:9'
		}
	}
};

export default config;
