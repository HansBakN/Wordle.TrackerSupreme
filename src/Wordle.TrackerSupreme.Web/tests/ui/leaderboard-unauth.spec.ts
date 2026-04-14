import { test, expect } from '@playwright/test';

test('leaderboard shows the unauthenticated prompt without loading data', async ({ page }) => {
	let leaderboardRequestCount = 0;

	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 401,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Unauthorized' })
		});
	});

	await page.route('**/api/stats/leaderboard', async (route) => {
		leaderboardRequestCount += 1;
		await route.fulfill({
			status: 500,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'This request should not be made.' })
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByText('Sign in to view the leaderboard.')).toBeVisible();
	await expect(page.getByRole('main').getByRole('link', { name: 'Sign in' })).toBeVisible();
	expect(leaderboardRequestCount).toBe(0);
});
