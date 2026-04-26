import { test, expect } from '@playwright/test';

const authedUser = {
	id: '11111111-1111-1111-1111-111111111111',
	displayName: 'Tester',
	email: 'tester@example.com',
	createdOn: '2025-01-01T00:00:00Z'
};

const replayState = {
	puzzleDate: '2025-04-01',
	cutoffPassed: true,
	solutionRevealed: false,
	allowLatePlay: true,
	wordLength: 5,
	maxGuesses: 6,
	isHardMode: true,
	canGuess: true,
	attempt: null,
	solution: null,
	isReplay: true
};

async function stubAuth(page: import('@playwright/test').Page) {
	await page.addInitScript(() => {
		window.localStorage.setItem('wts_auth_token', 'test-token');
	});
	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(authedUser)
		});
	});
}

test('replay landing page navigates to the chosen historical puzzle', async ({ page }) => {
	await stubAuth(page);

	await page.goto('/replay', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.getByTestId('replay-date').fill('2025-04-01');

	await page.route('**/api/game/state**', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(replayState)
		});
	});

	await page.getByTestId('replay-submit').click();

	await expect(page).toHaveURL(/\/replay\/2025-04-01$/);
	await expect(page.getByText('Puzzle for 01/04-2025')).toBeVisible();
	await expect(page.getByText('Practice', { exact: true })).toBeVisible();
});

test('replay page sends the date query parameter when fetching state', async ({ page }) => {
	await stubAuth(page);

	let requestedUrl: string | null = null;
	await page.route('**/api/game/state**', async (route) => {
		requestedUrl = route.request().url();
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(replayState)
		});
	});

	await page.goto('/replay/2025-04-01', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect.poll(() => requestedUrl).toContain('date=2025-04-01');
});

test('replay page shows a not-found message when the puzzle is missing', async ({ page }) => {
	await stubAuth(page);

	await page.route('**/api/game/state**', async (route) => {
		await route.fulfill({
			status: 404,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'No puzzle found for 2024-12-25.' })
		});
	});

	await page.goto('/replay/2024-12-25', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByTestId('puzzle-not-found')).toBeVisible();
});
