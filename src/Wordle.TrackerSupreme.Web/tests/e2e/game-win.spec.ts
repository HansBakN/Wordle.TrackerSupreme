import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

/**
 * Compute today's puzzle solution using the same deterministic formula as the backend
 * WordSelector. The word list and anchor date must stay in sync with
 * Wordle.TrackerSupreme.Application.Services.Game.WordSelector.
 */
function getTodaySolution(): string {
	const words = [
		'SLATE',
		'CRANE',
		'BRAVE',
		'TRAIN',
		'SHINE',
		'GLASS',
		'FROND',
		'QUIET',
		'PLANT',
		'ROAST',
		'TRAIL',
		'SNAKE',
		'CLOUD',
		'BRINK',
		'DRIVE',
		'STEAM',
		'WATER',
		'GRAPE',
		'PANEL',
		'CROWN',
		'STARE',
		'GHOST',
		'PLUSH',
		'MONEY',
		'LIGHT',
		'RANGE',
		'BRICK',
		'FLAME',
		'WOUND',
		'SCORE',
		'CHIME',
		'PRIDE',
		'STONE',
		'HOUSE',
		'PIVOT',
		'CHALK',
		'FROST',
		'BLINK',
		'SHARD',
		'TOWEL',
		'NORTH',
		'SOUTH',
		'EAGER',
		'QUEST',
		'FRAME',
		'GRIND',
		'WRIST',
		'TRICK',
		'VOICE',
		'YEARN'
	];
	const anchor = new Date('2025-01-01');
	const today = new Date();
	today.setHours(0, 0, 0, 0);
	anchor.setHours(0, 0, 0, 0);
	const msPerDay = 24 * 60 * 60 * 1000;
	const offset = Math.round((today.getTime() - anchor.getTime()) / msPerDay);
	const index = ((offset % words.length) + words.length) % words.length;
	return words[index];
}

test('player wins the daily puzzle and sees victory stats', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Win ${nonce}`,
		email: `e2e.win.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.getByText("Loading today's puzzle...").waitFor({ state: 'hidden' });

	const solution = getTodaySolution();

	await page.click('body');
	await page.keyboard.type(solution);
	await page.keyboard.press('Enter');

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

	await page.getByText("Loading today's puzzle...").waitFor({ state: 'hidden' });

	// Opt into easy mode before guessing
	await page.getByTestId('enable-easy-mode').click();
	await expect(page.getByText('Easy mode enabled for this puzzle.')).toBeVisible();

	const solution = getTodaySolution();

	await page.click('body');
	await page.keyboard.type(solution);
	await page.keyboard.press('Enter');

	const winStats = page.getByTestId('win-stats');
	await expect(winStats).toBeVisible({ timeout: 10_000 });
	await expect(winStats.getByText('Wins')).toBeVisible();
});
