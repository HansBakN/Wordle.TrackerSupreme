import { test, expect } from '@playwright/test';

test('stats page defaults to hard mode filters and excludes current player', async ({ page }) => {
	page.on('pageerror', (error) => {
		console.error('pageerror', error);
	});
	page.on('console', (message) => {
		if (message.type() === 'error') {
			console.error('console', message.text());
		}
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

	const bodies: Array<Record<string, unknown>> = [];
	await page.route('**/api/stats/players', async (route) => {
		const body = route.request().postDataJSON() as Record<string, unknown>;
		bodies.push(body);
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([
				{
					playerId: '11111111-1111-1111-1111-111111111111',
					displayName: 'Tester',
					stats: {
						totalAttempts: 3,
						wins: 2,
						failures: 1,
						practiceAttempts: 0,
						currentStreak: 2,
						longestStreak: 3,
						averageGuessCount: 3.5
					}
				},
				{
					playerId: '22222222-2222-2222-2222-222222222222',
					displayName: 'Rival',
					stats: {
						totalAttempts: 5,
						wins: 4,
						failures: 1,
						practiceAttempts: 1,
						currentStreak: 4,
						longestStreak: 5,
						averageGuessCount: 3
					}
				}
			])
		});
	});

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('heading', { name: 'Every player, every lens' })).toBeVisible();
	await expect(page.locator('main').getByText('Rival', { exact: true })).toBeVisible();
	await expect(page.locator('main').getByText('Tester', { exact: true })).toHaveCount(0);

	await expect(page.getByLabel('Easy mode attempts')).toBeVisible();
	await page.getByLabel('Easy mode attempts').check();
	await page.getByRole('button', { name: 'Apply filters' }).click();

	expect(bodies.length).toBeGreaterThanOrEqual(2);
	expect(bodies[0]).toMatchObject({
		includeHardMode: true,
		includeEasyMode: false,
		includeBeforeReveal: true,
		includeAfterReveal: false,
		includeSolved: true,
		includeFailed: true,
		includeInProgress: false
	});
	expect(bodies[1]).toMatchObject({
		includeEasyMode: true
	});
});
