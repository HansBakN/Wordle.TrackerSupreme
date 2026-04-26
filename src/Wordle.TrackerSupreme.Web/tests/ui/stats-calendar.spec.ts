import { test, expect } from '@playwright/test';

test('stats page shows streak calendar heatmap with daily outcomes', async ({ page }) => {
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
			body: JSON.stringify([])
		});
	});

	await page.route('**/api/stats/me/calendar*', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				days: [
					{ date: '2025-01-08', outcome: 'won', guessCount: 3, isAfterReveal: false },
					{ date: '2025-01-09', outcome: 'failed', guessCount: null, isAfterReveal: false },
					{ date: '2025-01-10', outcome: 'none', guessCount: null, isAfterReveal: false },
					{ date: '2025-01-11', outcome: 'won', guessCount: 4, isAfterReveal: true },
					{ date: '2025-01-12', outcome: 'in_progress', guessCount: null, isAfterReveal: false }
				]
			})
		});
	});

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const calendar = page.getByTestId('streak-calendar');
	await expect(calendar).toBeVisible();
	await expect(calendar.getByText('Streak calendar')).toBeVisible();
	await expect(calendar.getByText('Last 90 days')).toBeVisible();

	const wonDay = calendar.locator('[data-testid="calendar-day"][data-outcome="won"]');
	await expect(wonDay).toHaveCount(2);

	const failedDay = calendar.locator('[data-testid="calendar-day"][data-outcome="failed"]');
	await expect(failedDay).toHaveCount(1);

	const noneDay = calendar.locator('[data-testid="calendar-day"][data-outcome="none"]');
	await expect(noneDay).toHaveCount(1);

	const inProgressDay = calendar.locator(
		'[data-testid="calendar-day"][data-outcome="in_progress"]'
	);
	await expect(inProgressDay).toHaveCount(1);
});
