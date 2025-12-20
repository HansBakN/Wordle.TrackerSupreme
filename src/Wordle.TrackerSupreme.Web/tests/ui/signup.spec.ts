import { test, expect } from '@playwright/test';

test('sign-up form shows validation and backend error', async ({ page }) => {
	await page.route('**/api/Auth/signup', async (route) => {
		await route.fulfill({
			status: 409,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Email already used' })
		});
	});

	await page.goto('/signup');

	await page.getByLabel('Display name').fill('Wordler');
	await page.getByLabel('Email').fill('player@example.com');
	await page.locator('input[name="password"]').fill('secret123');
	await page.locator('input[name="confirmPassword"]').fill('secret123');
	await page.getByRole('button', { name: 'Sign up' }).click();

	const errorBox = page.locator('form .rounded-xl');
	await expect(errorBox).toBeVisible();
	await expect(errorBox).toContainText('Email already used');
});
