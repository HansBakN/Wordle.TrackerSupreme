import { expect, test } from '@playwright/test';

test('leaderboard can switch from all-time rankings to today standings', async ({ page }) => {
	let allTimeRequests = 0;
	let todayRequests = 0;

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
			allTimeRequests += 1;
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
						}
					],
					total: 1,
					page: 1,
					pageSize: 10,
					totalPages: 1
				})
			});
		}
	);

	await page.route('**/api/stats/leaderboard/today', async (route) => {
		todayRequests += 1;
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([
				{
					rank: 1,
					playerId: '33333333-3333-3333-3333-333333333333',
					displayName: 'Today Winner',
					result: 'Solved',
					guessCount: 3,
					playedInHardMode: true
				},
				{
					rank: 2,
					playerId: '44444444-4444-4444-4444-444444444444',
					displayName: 'Still Playing',
					result: 'In progress',
					guessCount: 4,
					playedInHardMode: false
				}
			])
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('tab', { name: 'All-time' })).toHaveAttribute(
		'aria-selected',
		'true'
	);
	await expect(page.getByRole('tab', { name: 'All-time' })).toHaveAttribute('tabindex', '0');
	await expect(page.getByRole('tab', { name: "Today's puzzle" })).toHaveAttribute('tabindex', '-1');
	await expect(page.getByRole('cell', { name: 'Rival' })).toBeVisible();
	expect(allTimeRequests).toBe(1);
	expect(todayRequests).toBe(0);

	await page.getByRole('tab', { name: 'All-time' }).focus();
	await page.keyboard.press('ArrowRight');

	await expect(page.getByRole('tab', { name: "Today's puzzle" })).toHaveAttribute(
		'aria-selected',
		'true'
	);
	await expect(page.getByRole('tab', { name: "Today's puzzle" })).toHaveAttribute('tabindex', '0');
	await expect(page.getByRole('tab', { name: "Today's puzzle" })).toBeFocused();

	await expect(page.getByRole('heading', { name: "Today's puzzle" })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Today Winner' })).toBeVisible();
	await expect(page.getByRole('cell', { name: 'Solved' })).toBeVisible();
	await expect(page.getByText('4 guesses so far • Easy mode')).toBeVisible();
	expect(todayRequests).toBe(1);
});
