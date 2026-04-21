import { expect, test } from '@playwright/test';
import { getTodaySolution, signUp } from './helpers';

test('player wins the daily puzzle and sees victory stats', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Win ${nonce}`,
		email: `e2e.win.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });

	const solution = getTodaySolution();

	await page.click('body');
	await page.keyboard.type(solution);
	await page.keyboard.press('Enter');

	await expect(page.getByTestId('confetti')).toBeVisible({ timeout: 10_000 });

	// Wait for the win-stats panel to appear (tile flip + confetti delay ~1.5 s)
	const winStats = page.getByTestId('win-stats');
	await expect(winStats).toBeVisible({ timeout: 10_000 });

	// Victory stats headings are visible
	await expect(winStats.getByText('Wins')).toBeVisible();
	await expect(winStats.getByText('Current streak')).toBeVisible();
	await expect(winStats.getByText('Avg guesses')).toBeVisible();
});

test('player wins the daily puzzle in easy mode and sees victory stats', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E WinEasy ${nonce}`,
		email: `e2e.wineasy.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });

	// Opt into easy mode before guessing
	await page.getByTestId('enable-easy-mode').click();
	await expect(page.getByText('Easy mode enabled for this puzzle.')).toBeVisible();

	const solution = getTodaySolution();

	await page.click('body');
	await page.keyboard.type(solution);
	await page.keyboard.press('Enter');

	await expect(page.getByTestId('confetti')).toBeVisible({ timeout: 10_000 });

	const winStats = page.getByTestId('win-stats');
	await expect(winStats).toBeVisible({ timeout: 10_000 });
	await expect(winStats.getByText('Wins')).toBeVisible();
});
