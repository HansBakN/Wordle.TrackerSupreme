import { expect, test } from '@playwright/test';

test('stats page prompts for sign in when unauthenticated', async ({ page }) => {
	await page.goto('/');
	await page.evaluate(() => localStorage.clear());
	await page.goto('/stats');
	await expect(page.getByText('Sign in to compare stats.')).toBeVisible();
	await expect(page.getByRole('main').getByRole('link', { name: 'Sign in' })).toBeVisible();
});
