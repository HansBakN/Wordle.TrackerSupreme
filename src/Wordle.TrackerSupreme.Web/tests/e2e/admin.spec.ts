import { expect, test } from '@playwright/test';

test('admin can manage player profiles and attempts', async ({ page }) => {
	await page.goto('/signin');
	await page.getByLabel('Email').fill('admin@wordle.supreme');
	await page.getByLabel('Password').fill('dev-password');
	await page.getByRole('button', { name: 'Sign in' }).click();
	await page.waitForURL('**/');

	await page.getByRole('link', { name: 'Admin' }).click();
	await page.waitForURL('**/admin');

	const firstPlayer = page.getByTestId('admin-player-row').first();
	await firstPlayer.click();

	const attemptCards = page.getByTestId('admin-attempt-card');
	await expect(attemptCards.first()).toBeVisible();

	await page.getByTestId('admin-password').fill('Reset!234');
	await page.getByTestId('admin-password-reset').click();
	await expect(page.getByText('Password reset successfully.')).toBeVisible();

	await page.getByTestId('admin-attempt-edit').first().click();
	await page.getByTestId('admin-attempt-guess').first().fill('CRANE');
	await page.getByTestId('admin-attempt-save').click();
	await expect(attemptCards.first()).toContainText('CRANE');

	const beforeCount = await attemptCards.count();
	await page.getByTestId('admin-attempt-reset').first().click();
	await expect(attemptCards).toHaveCount(beforeCount - 1);
});
