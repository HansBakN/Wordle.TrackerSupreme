import { test, expect } from '@playwright/test';

const AUTH_MOCK = {
	id: '11111111-1111-1111-1111-111111111111',
	displayName: 'Tester',
	email: 'tester@example.com',
	createdOn: '2025-01-01T00:00:00Z'
};

const EMPTY_PLAYERS_MOCK = JSON.stringify([]);

async function mockAuth(page: import('@playwright/test').Page) {
	await page.route('**/api/Auth/me', (route) =>
		route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(AUTH_MOCK) })
	);
	await page.route('**/api/stats/players', (route) =>
		route.fulfill({ status: 200, contentType: 'application/json', body: EMPTY_PLAYERS_MOCK })
	);
}

test('stats page shows puzzle history section when authenticated', async ({ page }) => {
	await mockAuth(page);
	await page.route('**/api/stats/me/history', (route) =>
		route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
	);

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.locator('[data-testid="puzzle-history-section"]')).toBeVisible();
	await expect(page.getByText('Your past attempts')).toBeVisible();
});

test('stats page shows empty message when no puzzle history', async ({ page }) => {
	await mockAuth(page);
	await page.route('**/api/stats/me/history', (route) =>
		route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
	);

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.locator('[data-testid="history-empty"]')).toBeVisible();
	await expect(page.locator('[data-testid="history-list"]')).toHaveCount(0);
});

test('stats page renders puzzle history entries', async ({ page }) => {
	await mockAuth(page);
	await page.route('**/api/stats/me/history', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([
				{
					puzzleDate: '2025-01-03',
					solution: 'CRANE',
					status: 'Solved',
					playedInHardMode: true,
					isAfterReveal: false,
					guessCount: 3,
					guesses: []
				},
				{
					puzzleDate: '2025-01-02',
					solution: null,
					status: 'Failed',
					playedInHardMode: false,
					isAfterReveal: true,
					guessCount: 6,
					guesses: []
				}
			])
		})
	);

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const entries = page.locator('[data-testid="history-entry"]');
	await expect(entries).toHaveCount(2);

	// First entry: solved, hard mode, solution visible
	const first = entries.nth(0);
	await expect(first.locator('[data-testid="history-date"]')).toContainText('2025-01-03');
	await expect(first.locator('[data-testid="history-status"]')).toContainText('Solved in 3');
	await expect(first.locator('[data-testid="history-hard-mode"]')).toBeVisible();
	await expect(first.locator('[data-testid="history-solution"]')).toContainText('CRANE');

	// Second entry: failed, practice, no solution
	const second = entries.nth(1);
	await expect(second.locator('[data-testid="history-date"]')).toContainText('2025-01-02');
	await expect(second.locator('[data-testid="history-status"]')).toContainText('Failed');
	await expect(second.locator('[data-testid="history-practice"]')).toBeVisible();
	await expect(second.locator('[data-testid="history-solution"]')).toHaveCount(0);
});

test('stats page expands puzzle history entry to show guess tiles', async ({ page }) => {
	await mockAuth(page);
	await page.route('**/api/stats/me/history', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([
				{
					puzzleDate: '2025-01-01',
					solution: 'CRANE',
					status: 'Solved',
					playedInHardMode: false,
					isAfterReveal: false,
					guessCount: 2,
					guesses: [
						{
							guessId: 'aaaaaaaa-0000-0000-0000-000000000001',
							guessNumber: 1,
							guessWord: 'SLATE',
							feedback: [
								{ position: 0, letter: 'S', result: 'Absent' },
								{ position: 1, letter: 'L', result: 'Absent' },
								{ position: 2, letter: 'A', result: 'Present' },
								{ position: 3, letter: 'T', result: 'Absent' },
								{ position: 4, letter: 'E', result: 'Present' }
							]
						},
						{
							guessId: 'aaaaaaaa-0000-0000-0000-000000000002',
							guessNumber: 2,
							guessWord: 'CRANE',
							feedback: [
								{ position: 0, letter: 'C', result: 'Correct' },
								{ position: 1, letter: 'R', result: 'Correct' },
								{ position: 2, letter: 'A', result: 'Correct' },
								{ position: 3, letter: 'N', result: 'Correct' },
								{ position: 4, letter: 'E', result: 'Correct' }
							]
						}
					]
				}
			])
		})
	);

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	// Guesses are hidden initially
	await expect(page.locator('[data-testid="history-guesses"]')).toHaveCount(0);

	// Click the toggle to expand
	await page.locator('[data-testid="history-toggle"]').click();

	// Guess rows should now be visible
	const guessRows = page.locator('[data-testid="history-guess-row"]');
	await expect(guessRows).toHaveCount(2);

	// Each row has 5 tiles
	const tiles = page.locator('[data-testid="history-tile"]');
	await expect(tiles).toHaveCount(10);
});

test('stats page shows error when history fails to load', async ({ page }) => {
	await mockAuth(page);
	await page.route('**/api/stats/me/history', (route) =>
		route.fulfill({
			status: 500,
			contentType: 'application/json',
			body: JSON.stringify({ detail: 'Internal server error' })
		})
	);

	await page.goto('/stats', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.locator('[data-testid="history-error"]')).toBeVisible();
});
