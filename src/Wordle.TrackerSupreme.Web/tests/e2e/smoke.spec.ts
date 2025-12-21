import { expect, test } from '@playwright/test';

test('signup and submit a guess', async ({ page }) => {
	await page.goto('/signup');

	const uniqueSuffix = Date.now();
	await page.getByLabel('Display name').fill(`E2E Player ${uniqueSuffix}`);
	await page.getByLabel('Email').fill(`e2e-${uniqueSuffix}@example.com`);
	await page.getByRole('textbox', { name: 'Password', exact: true }).fill('e2e-pass-123');
	await page.getByRole('textbox', { name: 'Confirm password', exact: true }).fill('e2e-pass-123');
	await page.getByRole('button', { name: 'Sign up' }).click();

	await expect(page.getByText('Puzzle for')).toBeVisible();
	await expect(page.getByTestId('board-row-0')).toBeVisible();

	await page.click('body');
	await page.keyboard.type('CRANE');
	const guessResponse = page.waitForResponse((response) => {
		return response.url().includes('/api/game/guess') && response.ok();
	});
	await page.keyboard.press('Enter');
	await guessResponse;

	await expect(page.getByTestId('board-row-0')).toContainText('CRANE');
});
