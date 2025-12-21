import { test, expect } from '@playwright/test';

test('leaderboard shows empty state when no entries', async ({ page }) => {
	page.on('pageerror', (error) => {
		console.error('pageerror', error);
	});

	await page.addInitScript(() => {
		window.localStorage.setItem('wts_auth_token', 'test-token');
	});

	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '11111111-1111-1111-1111-111111111111',
				displayName: 'Tester',
				email: 'tester@example.com',
				createdOn: '2025-01-01T00:00:00Z'
			})
		});
	});

	await page.route('**/api/stats/leaderboard', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([])
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByText('No leaderboard entries yet.')).toBeVisible();
});
