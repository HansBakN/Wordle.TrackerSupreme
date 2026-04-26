import { test, expect } from '@playwright/test';

async function dismissHowToPlayModal(page: import('@playwright/test').Page) {
	const closeButton = page.getByTestId('close-how-to-play');
	if (await closeButton.isVisible().catch(() => false)) {
		await closeButton.click();
	}
}

async function suppressHowToPlayModal(page: import('@playwright/test').Page) {
	await page.addInitScript(() => {
		localStorage.setItem('wts_hasSeenHowToPlay', '1');
	});
}

test('leaderboard shows ranked hard mode entries', async ({ page }) => {
	await suppressHowToPlayModal(page);
	let requestedUrl: string | null = null;

	page.on('pageerror', (error) => {
		console.error('pageerror', error);
	});
	page.on('console', (message) => {
		if (message.type() === 'error') {
			console.error('console', message.text());
		}
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

	await page.route('**/api/stats/leaderboard**', async (route) => {
		requestedUrl = route.request().url();
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				items: [
					{
						rank: 1,
						playerId: '22222222-2222-2222-2222-222222222222',
						displayName: 'Rival',
						totalAttempts: 5,
						wins: 4,
						failures: 1,
						currentStreak: 4,
						longestStreak: 5,
						practiceAttempts: 0,
						averageGuessCount: 3,
						winRate: 0.8
					},
					{
						rank: 2,
						playerId: '33333333-3333-3333-3333-333333333333',
						displayName: 'Challenger',
						totalAttempts: 3,
						wins: 2,
						failures: 1,
						currentStreak: 2,
						longestStreak: 3,
						practiceAttempts: 0,
						averageGuessCount: 4,
						winRate: 0.66
					}
				],
				total: 2,
				page: 1,
				pageSize: 10,
				totalPages: 1
			})
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await dismissHowToPlayModal(page);

	await expect(page.getByRole('heading', { name: 'Hard mode before noon' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Rival' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Challenger' })).toBeVisible();
	await expect(page.getByRole('cell', { name: '1' })).toBeVisible();
	await expect.poll(() => requestedUrl).toContain('minGames=10');
	await expect(page.getByTestId('leaderboard-min-games-toggle')).not.toBeChecked();
});

test('leaderboard can include players with fewer than ten games', async ({ page }) => {
	await suppressHowToPlayModal(page);
	const requestedUrls: string[] = [];

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

	await page.route('**/api/stats/leaderboard**', async (route) => {
		requestedUrls.push(route.request().url());
		const url = new URL(route.request().url());
		const minGames = url.searchParams.get('minGames');
		const items =
			minGames === '0'
				? [
						{
							rank: 1,
							playerId: '22222222-2222-2222-2222-222222222222',
							displayName: 'Rival',
							totalAttempts: 12,
							wins: 10,
							failures: 2,
							currentStreak: 6,
							longestStreak: 8,
							practiceAttempts: 0,
							averageGuessCount: 3,
							winRate: 0.83
						},
						{
							rank: 2,
							playerId: '33333333-3333-3333-3333-333333333333',
							displayName: 'Newcomer',
							totalAttempts: 3,
							wins: 2,
							failures: 1,
							currentStreak: 2,
							longestStreak: 2,
							practiceAttempts: 0,
							averageGuessCount: 4,
							winRate: 0.66
						}
					]
				: [
						{
							rank: 1,
							playerId: '22222222-2222-2222-2222-222222222222',
							displayName: 'Rival',
							totalAttempts: 12,
							wins: 10,
							failures: 2,
							currentStreak: 6,
							longestStreak: 8,
							practiceAttempts: 0,
							averageGuessCount: 3,
							winRate: 0.83
						}
					];

		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				items,
				total: items.length,
				page: 1,
				pageSize: 10,
				totalPages: 1
			})
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await dismissHowToPlayModal(page);

	await expect(page.getByRole('cell', { name: 'Rival' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Newcomer' })).toHaveCount(0);

	await page.getByTestId('leaderboard-min-games-toggle').check();

	await expect(page.getByRole('cell', { name: 'Newcomer' })).toBeVisible();
	expect(requestedUrls).toHaveLength(2);
	expect(requestedUrls[0]).toContain('minGames=10');
	expect(requestedUrls[1]).toContain('minGames=0');
});
