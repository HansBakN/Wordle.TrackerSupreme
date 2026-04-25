import { test, expect } from '@playwright/test';

test('leaderboard shows ranked hard mode entries', async ({ page }) => {
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
			body: JSON.stringify([
				{
					rank: 1,
					playerId: '22222222-2222-2222-2222-222222222222',
					displayName: 'Rival',
					totalAttempts: 15,
					wins: 12,
					failures: 3,
					currentStreak: 4,
					longestStreak: 9,
					practiceAttempts: 0,
					averageGuessCount: 3,
					winRate: 0.8
				},
				{
					rank: 2,
					playerId: '33333333-3333-3333-3333-333333333333',
					displayName: 'Challenger',
					totalAttempts: 11,
					wins: 7,
					failures: 4,
					currentStreak: 2,
					longestStreak: 6,
					practiceAttempts: 0,
					averageGuessCount: 4,
					winRate: 0.64
				}
			])
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('heading', { name: 'Hard mode before noon' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Rival' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Challenger' })).toBeVisible();
	await expect(page.getByRole('cell', { name: '1', exact: true })).toBeVisible();
	await expect.poll(() => requestedUrl).toContain('minGames=10');
	await expect(page.getByTestId('leaderboard-min-games-toggle')).not.toBeChecked();
});

test('leaderboard can include players with fewer than ten games', async ({ page }) => {
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
		const body =
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
			body: JSON.stringify(body)
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('cell', { name: 'Rival' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Newcomer' })).toHaveCount(0);

	await page.getByTestId('leaderboard-min-games-toggle').check();

	await expect(page.getByRole('cell', { name: 'Newcomer' })).toBeVisible();
	expect(requestedUrls).toHaveLength(2);
	expect(requestedUrls[0]).toContain('minGames=10');
	expect(requestedUrls[1]).toContain('minGames=0');
});
