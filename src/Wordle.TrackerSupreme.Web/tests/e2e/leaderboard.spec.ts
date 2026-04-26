import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

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
	const anchor = new Date(2025, 0, 1);
	const now = new Date();
	const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
	const msPerDay = 24 * 60 * 60 * 1000;
	const offset = Math.round((today.getTime() - anchor.getTime()) / msPerDay);
	const index = ((offset % words.length) + words.length) % words.length;
	return words[index];
}

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

	await page.getByRole('tab', { name: "Today's puzzle" }).click();

	await expect(page.getByRole('heading', { name: "Today's puzzle" })).toBeVisible();
	await expect(page.getByTestId('leaderboard-table')).toBeVisible();
	await expect(page.getByText('Solved').first()).toBeVisible();
	await expect(page.getByText('In progress').first()).toBeVisible();
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
	const leaderboardTable = page.getByTestId('leaderboard-table');
	await expect(leaderboardTable.getByText(displayName)).toHaveCount(0);

	await page.getByTestId('leaderboard-min-games-toggle').check();

	await expect(leaderboardTable.getByText(displayName)).toBeVisible();
});
