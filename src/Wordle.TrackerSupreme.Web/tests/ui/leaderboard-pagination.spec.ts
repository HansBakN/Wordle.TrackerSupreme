import { test, expect } from '@playwright/test';

test('leaderboard shows pagination controls when more than one page', async ({ page }) => {
	const requestedPages: number[] = [];

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
			const requestedPage = parseInt(url.searchParams.get('page') ?? '1', 10);
			requestedPages.push(requestedPage);

			const entry = (rank: number, name: string, id: string) => ({
				rank,
				playerId: id,
				displayName: name,
				totalAttempts: 5,
				wins: 4,
				failures: 1,
				currentStreak: 4,
				longestStreak: 5,
				practiceAttempts: 0,
				averageGuessCount: 3,
				winRate: 0.8
			});

			if (requestedPage === 1) {
				await route.fulfill({
					status: 200,
					contentType: 'application/json',
					body: JSON.stringify({
						items: [entry(1, 'Alice', '11111111-0000-0000-0000-000000000001')],
						total: 15,
						page: 1,
						pageSize: 10,
						totalPages: 2
					})
				});
			} else {
				await route.fulfill({
					status: 200,
					contentType: 'application/json',
					body: JSON.stringify({
						items: [entry(11, 'Zack', '11111111-0000-0000-0000-000000000002')],
						total: 15,
						page: 2,
						pageSize: 10,
						totalPages: 2
					})
				});
			}
		}
	);

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	// Pagination controls should be visible
	const pagination = page.locator('[data-testid="leaderboard-pagination"]');
	await expect(pagination).toBeVisible();
	await expect(page.locator('[data-testid="leaderboard-prev"]')).toBeDisabled();
	await expect(page.locator('[data-testid="leaderboard-next"]')).toBeEnabled();

	// Navigate to page 2
	await page.locator('[data-testid="leaderboard-next"]').click();

	await expect(page.getByRole('cell', { name: 'Zack' })).toBeVisible();
	await expect(page.locator('[data-testid="leaderboard-prev"]')).toBeEnabled();
	await expect(page.locator('[data-testid="leaderboard-next"]')).toBeDisabled();

	expect(requestedPages).toContain(2);
});

test('leaderboard hides pagination when only one page', async ({ page }) => {
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
						displayName: 'Solo',
						totalAttempts: 1,
						wins: 1,
						failures: 0,
						currentStreak: 1,
						longestStreak: 1,
						practiceAttempts: 0,
						averageGuessCount: 2,
						winRate: 1
					}
				],
				total: 1,
				page: 1,
				pageSize: 10,
				totalPages: 1
			})
		});
	});

	await page.goto('/leaderboard', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('cell', { name: 'Solo' })).toBeVisible();
	await expect(page.locator('[data-testid="leaderboard-pagination"]')).toHaveCount(0);
});
