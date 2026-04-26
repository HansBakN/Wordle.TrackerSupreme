import { test, expect } from '@playwright/test';

test('leaderboard shows ranked hard mode entries', async ({ page }) => {
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

	await expect(page.getByRole('heading', { name: 'Hard mode before noon' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Rival' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Challenger' })).toBeVisible();
	await expect(page.getByRole('cell', { name: '1' })).toBeVisible();
});

test('leaderboard requests the selected all-time sort', async ({ page }) => {
	const requestedSorts: string[] = [];

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

	await page.route(
		(url) => url.href.includes('/api/stats/leaderboard') && !url.href.includes('/today'),
		async (route) => {
			const url = new URL(route.request().url());
			const sortBy = url.searchParams.get('sortBy') ?? 'winRate';
			requestedSorts.push(sortBy);

			const items =
				sortBy === 'wins'
					? [
							{
								rank: 1,
								playerId: '22222222-2222-2222-2222-222222222222',
								displayName: 'Volume',
								totalAttempts: 8,
								wins: 7,
								failures: 1,
								currentStreak: 4,
								longestStreak: 5,
								practiceAttempts: 0,
								averageGuessCount: 4,
								winRate: 0.875
							},
							{
								rank: 2,
								playerId: '33333333-3333-3333-3333-333333333333',
								displayName: 'Precision',
								totalAttempts: 4,
								wins: 3,
								failures: 1,
								currentStreak: 3,
								longestStreak: 4,
								practiceAttempts: 0,
								averageGuessCount: 2,
								winRate: 0.75
							}
						]
					: [
							{
								rank: 1,
								playerId: '33333333-3333-3333-3333-333333333333',
								displayName: 'Precision',
								totalAttempts: 4,
								wins: 3,
								failures: 1,
								currentStreak: 3,
								longestStreak: 4,
								practiceAttempts: 0,
								averageGuessCount: 2,
								winRate: 0.75
							},
							{
								rank: 2,
								playerId: '22222222-2222-2222-2222-222222222222',
								displayName: 'Volume',
								totalAttempts: 8,
								wins: 7,
								failures: 1,
								currentStreak: 4,
								longestStreak: 5,
								practiceAttempts: 0,
								averageGuessCount: 4,
								winRate: 0.875
							}
						];

			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					items,
					total: 2,
					page: 1,
					pageSize: 10,
					totalPages: 1
				})
			});
		}
	);

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('cell', { name: 'Precision' }).first()).toBeVisible();

	await page.getByLabel('Sort leaderboard by').selectOption('wins');

	await expect(page.getByRole('cell', { name: 'Volume' }).first()).toBeVisible();
	expect(requestedSorts).toEqual(['winRate', 'wins']);
});
