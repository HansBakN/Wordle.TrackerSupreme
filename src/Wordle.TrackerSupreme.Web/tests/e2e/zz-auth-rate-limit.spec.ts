import { expect, test } from '@playwright/test';

test('sign-in shows a clear message after too many failed attempts', async ({ page }) => {
	await page.goto('/signin');
	await page.getByLabel('Email').fill('admin@wordle.supreme');
	const message = page.getByText('Too many authentication attempts. Please try again in a minute.');

	for (let attempt = 0; attempt < 40; attempt += 1) {
		await page.getByLabel('Password').fill('wrong-password');
		await page.getByRole('button', { name: 'Sign in' }).click();

		if (await message.isVisible()) {
			break;
		}
	}

	await expect(message).toBeVisible();
	await expect(page).toHaveURL(/\/signin$/);
});
