import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('redirects to sign-in when an authenticated game request returns 401', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Session ${nonce}`,
		email: `e2e.session.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 401,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Unauthorized' })
		});
	});

	await page.goto('/');

	await expect(page).toHaveURL(/\/signin$/);
});
