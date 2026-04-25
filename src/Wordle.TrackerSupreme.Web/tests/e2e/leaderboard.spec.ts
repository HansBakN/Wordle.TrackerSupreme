import { expect, test } from '@playwright/test';
import { getTodaySolution, signUp } from './helpers';

test('leaderboard renders seeded entries for signed-in users', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Player ${nonce}`,
		email: `e2e.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.goto('/leaderboard');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByRole('heading', { name: 'Hard mode before noon' })).toBeVisible();
	await expect(page.getByTestId('leaderboard-table')).toBeVisible();
	await expect(page.getByTestId('leaderboard-row').first()).toBeVisible();
	await expect(page.getByTestId('leaderboard-min-games-toggle')).not.toBeChecked();
});

test('leaderboard can opt in to players with fewer than ten games', async ({ page }) => {
	const nonce = Date.now();
	const displayName = `E2E Rookie ${nonce}`;
	await signUp(page, {
		displayName,
		email: `e2e.rookie.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });
	await page.click('body');
	await page.keyboard.type(getTodaySolution());
	await page.keyboard.press('Enter');
	await expect(page.getByTestId('win-stats')).toBeVisible({ timeout: 10_000 });

	await page.goto('/leaderboard');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByRole('heading', { name: 'Hard mode before noon' })).toBeVisible();
	await expect(page.getByText(displayName)).toHaveCount(0);

	await page.getByTestId('leaderboard-min-games-toggle').check();

	await expect(page.getByText(displayName)).toBeVisible();
});
