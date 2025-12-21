import { test, expect } from '@playwright/test';

test('sign-in form shows validation and handles failed auth', async ({ page }) => {
	page.on('pageerror', (error) => {
		console.error('pageerror', error);
	});
	page.on('console', (message) => {
		if (message.type() === 'error') {
			console.error('console', message.text());
		}
	});
	await page.addInitScript(() => {
		window.localStorage.clear();
	});
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

	await page.goto('/signin', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const bodyText = await page.evaluate(() => document.body.innerText ?? '');
	console.log('signin body', bodyText.slice(0, 200));

	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();

	await page.getByLabel('Email').fill('player@example.com');
	await page.getByLabel('Password').fill('secret123');
	await page.getByRole('button', { name: 'Sign in' }).click();

	const errorBox = page.locator('form .rounded-xl');
	await expect(errorBox).toBeVisible();
	await expect(errorBox).not.toHaveText('');
});
