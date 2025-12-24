import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('shows error when daily puzzle is unavailable', async ({ page }) => {
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
});
