import { expect, test } from '@playwright/test';

test('leaderboard prompts for sign in when unauthenticated', async ({ page }) => {
	await page.goto('/');
	await page.evaluate(() => localStorage.clear());
	await page.goto('/leaderboard');
	await expect(page.getByText('Sign in to view the leaderboard.')).toBeVisible();
	await expect(page.getByRole('main').getByRole('link', { name: 'Sign in' })).toBeVisible();
});
