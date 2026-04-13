import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('shows a friendly message when a guess hits a duplicate-attempt conflict', async ({
	page
}) => {
	await page.route('**/api/game/guess', async (route) => {
		await route.fulfill({
			status: 409,
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({
				message: "You already have an attempt for today's puzzle. Refresh to continue."
			})
		});
	});

	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Conflict ${nonce}`,
		email: `e2e.conflict.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.click('body');
	await page.keyboard.type('CRANE');
	await page.keyboard.press('Enter');

	await expect(
		page.getByText("You already have an attempt for today's puzzle. Refresh to continue.")
	).toBeVisible();
	await expect(page.getByTestId('board-row-0')).toContainText('CRANE');
});
