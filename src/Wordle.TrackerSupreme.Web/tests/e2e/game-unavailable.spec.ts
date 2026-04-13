import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('shows error when daily puzzle is unavailable (generic 503)', async ({ page }) => {
	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 503,
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({
				message: "Unable to retrieve today's puzzle. Please try again later."
			})
		});
	});

	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Puzzle ${nonce}`,
		email: `e2e.puzzle.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await expect(
		page.getByText("Unable to retrieve today's puzzle. Please try again later.")
	).toBeVisible();
	await expect(page.getByTestId('daily-puzzle-error')).toBeVisible();
});

test('shows friendly banner when no puzzle is scheduled today', async ({ page }) => {
	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 503,
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({
				code: 'puzzle_unavailable',
				message: "Unable to retrieve today's puzzle. Please try again later."
			})
		});
	});

	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E NoPuzzle ${nonce}`,
		email: `e2e.nopuzzle.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await expect(page.getByTestId('no-puzzle-today')).toBeVisible();
	await expect(page.getByText('No puzzle available today.')).toBeVisible();
});
