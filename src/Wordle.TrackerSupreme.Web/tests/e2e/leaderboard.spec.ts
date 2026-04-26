import { expect, test } from '@playwright/test';
import { getTodaySolution, signUp } from './helpers';

test('leaderboard renders seeded entries for signed-in users', async ({ page }) => {
	const nonce = Date.now();
	const solution = getTodaySolution();
	const inProgressGuess = solution === 'CRANE' ? 'SLATE' : 'CRANE';

	await signUp(page, {
		displayName: `E2E Solved ${nonce}`,
		email: `e2e.solved.${nonce}@example.com`,
		password: 'Supreme!234'
	});
	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });
	await page.click('body');
	await page.keyboard.type(solution);
	const solvedGuessResponse = page.waitForResponse((response) => {
		return response.url().includes('/api/game/guess') && response.ok();
	});
	await page.keyboard.press('Enter');
	await solvedGuessResponse;
	await expect(page.getByTestId('win-stats')).toBeVisible({ timeout: 10_000 });

	await page.getByRole('button', { name: 'Sign out' }).click();
	await signUp(page, {
		displayName: `E2E Progress ${nonce}`,
		email: `e2e.progress.${nonce}@example.com`,
		password: 'Supreme!234'
	});
	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });
	await page.click('body');
	await page.keyboard.type(inProgressGuess);
	const inProgressGuessResponse = page.waitForResponse((response) => {
		return response.url().includes('/api/game/guess') && response.ok();
	});
	await page.keyboard.press('Enter');
	await inProgressGuessResponse;

	await page.goto('/leaderboard');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByRole('heading', { name: 'Hard mode before noon' })).toBeVisible();
	await expect(page.getByTestId('leaderboard-table')).toBeVisible();
	await expect(page.getByTestId('leaderboard-row').first()).toBeVisible();

	await page.getByRole('tab', { name: "Today's puzzle" }).click();

	await expect(page.getByRole('heading', { name: "Today's puzzle" })).toBeVisible();
	await expect(page.getByTestId('leaderboard-table')).toBeVisible();
	await expect(page.getByText('Solved').first()).toBeVisible();
	await expect(page.getByText('In progress').first()).toBeVisible();
});
