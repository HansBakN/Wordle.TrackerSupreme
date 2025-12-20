import { test, expect } from '@playwright/test';

test('sign-in form shows validation and handles failed auth', async ({ page }) => {
	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 401,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Unauthorized' })
		});
	});
	await page.route('**/api/Auth/signin', async (route) => {
		await route.fulfill({
			status: 401,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Invalid credentials' })
		});
	});

	await page.goto('/signin');

	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();

	await page.getByLabel('Email').fill('player@example.com');
	await page.getByLabel('Password').fill('secret123');
	await page.getByRole('button', { name: 'Sign in' }).click();

	const errorBox = page.locator('form .rounded-xl');
	await expect(errorBox).toBeVisible();
	await expect(errorBox).not.toHaveText('');
});
