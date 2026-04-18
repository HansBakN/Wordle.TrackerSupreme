import { test, expect } from '@playwright/test';

test('stats page shows guess distribution bars when data is present', async ({ page }) => {
	page.on('pageerror', (error) => {
		console.error('pageerror', error);
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

	await page.route('**/api/stats/players', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([
				{
					playerId: '22222222-2222-2222-2222-222222222222',
					displayName: 'Rival',
					stats: {
						totalAttempts: 5,
						wins: 4,
						failures: 1,
						practiceAttempts: 0,
						currentStreak: 4,
						longestStreak: 5,
						averageGuessCount: 3,
						guessDistribution: { '2': 1, '3': 2, '4': 1 }
					}
				}
			])
		});
	});

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const distribution = page.locator('[data-testid="guess-distribution"]');
	await expect(distribution).toBeVisible();
	await expect(distribution.getByText('Guess distribution', { exact: false })).toBeVisible();

	// Rows 1–6 are always rendered; the counts for guesses 2,3,4 should be > 0
	const rows = distribution.locator('.flex.items-center');
	await expect(rows).toHaveCount(6);
});

test('stats page hides guess distribution when no wins', async ({ page }) => {
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

	await page.route('**/api/stats/players', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([
				{
					playerId: '22222222-2222-2222-2222-222222222222',
					displayName: 'Rival',
					stats: {
						totalAttempts: 2,
						wins: 0,
						failures: 2,
						practiceAttempts: 0,
						currentStreak: 0,
						longestStreak: 0,
						averageGuessCount: null,
						guessDistribution: {}
					}
				}
			])
		});
	});

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.locator('[data-testid="guess-distribution"]')).toHaveCount(0);
});
