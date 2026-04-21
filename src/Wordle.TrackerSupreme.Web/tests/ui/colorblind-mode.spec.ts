import { expect, test } from '@playwright/test';

const MOCK_USER = {
	id: '11111111-1111-1111-1111-111111111111',
	displayName: 'Tester',
	email: 'tester@example.com',
	createdOn: '2025-01-01T00:00:00Z',
	isAdmin: false
};

const MOCK_GAME_STATE = {
	puzzleDate: '2025-01-01',
	cutoffPassed: false,
	solutionRevealed: false,
	allowLatePlay: false,
	wordLength: 5,
	maxGuesses: 6,
	isHardMode: true,
	canGuess: true,
	attempt: {
		attemptId: '22222222-2222-2222-2222-222222222222',
		status: 'InProgress',
		isAfterReveal: false,
		createdOn: '2025-01-01T00:00:00Z',
		completedOn: null,
		guesses: [
			{
				guessId: 'gggg-1',
				guessNumber: 1,
				guessWord: 'CRANE',
				feedback: [
					{ position: 0, letter: 'C', result: 'Correct' },
					{ position: 1, letter: 'R', result: 'Present' },
					{ position: 2, letter: 'A', result: 'Absent' },
					{ position: 3, letter: 'N', result: 'Absent' },
					{ position: 4, letter: 'E', result: 'Absent' }
				]
			}
		]
	},
	solution: null
};

async function setupRoutes(page: import('@playwright/test').Page) {
	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(MOCK_USER)
		});
	});
	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(MOCK_GAME_STATE)
		});
	});
}

test('high-contrast toggle is visible in the header', async ({ page }) => {
	await setupRoutes(page);
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByTestId('toggle-high-contrast')).toBeVisible();
});

test('toggling high-contrast applies orange color to correct tiles', async ({ page }) => {
	await setupRoutes(page);
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	// Default: correct tile should be emerald (green)
	const correctTile = page.locator('[role="gridcell"]').first();
	await expect(correctTile).toHaveClass(/bg-emerald-400/);

	// Enable high-contrast
	await page.getByTestId('toggle-high-contrast').click();

	// Now the correct tile should be orange
	await expect(correctTile).toHaveClass(/bg-orange-500/);
});

test('toggling high-contrast applies blue color to present tiles', async ({ page }) => {
	await setupRoutes(page);
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	// Default: present tile uses amber
	const presentTile = page.locator('[role="gridcell"]').nth(1);
	await expect(presentTile).toHaveClass(/bg-amber-300/);

	// Enable high-contrast
	await page.getByTestId('toggle-high-contrast').click();
	await expect(presentTile).toHaveClass(/bg-blue-400/);
});

test('high-contrast preference persists across page reloads', async ({ page }) => {
	await setupRoutes(page);
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	// Enable high-contrast
	await page.getByTestId('toggle-high-contrast').click();
	expect(await page.evaluate(() => localStorage.getItem('wts_highContrastMode'))).toBe('1');

	// Reload and check it's still applied
	await page.reload({ waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const correctTile = page.locator('[role="gridcell"]').first();
	await expect(correctTile).toHaveClass(/bg-orange-500/);
});

test('toggling off restores standard colours', async ({ page }) => {
	await setupRoutes(page);
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const correctTile = page.locator('[role="gridcell"]').first();

	// Turn on then off
	await page.getByTestId('toggle-high-contrast').click();
	await expect(correctTile).toHaveClass(/bg-orange-500/);

	await page.getByTestId('toggle-high-contrast').click();
	await expect(correctTile).toHaveClass(/bg-emerald-400/);
});
