import { test, expect } from '@playwright/test';

test('password strength hints appear after typing and reflect requirements', async ({ page }) => {
	await page.addInitScript(() => window.localStorage.clear());
	await page.goto('/signup', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const passwordInput = page.getByTestId('password-input');
	const requirements = page.getByTestId('password-requirements');
	const reqMin = page.getByTestId('req-min-length');
	const reqMax = page.getByTestId('req-max-length');

	// Hints are hidden until the user starts typing
	await expect(requirements).toBeHidden();

	// Short password: min-length requirement shown as unmet
	await passwordInput.fill('abc');
	await expect(requirements).toBeVisible();
	await expect(reqMin).toContainText('✗');
	await expect(reqMax).toContainText('✓');

	// Valid password: both requirements met
	await passwordInput.fill('correct');
	await expect(reqMin).toContainText('✓');
	await expect(reqMax).toContainText('✓');
});

test('sign-up form shows validation and backend error', async ({ page }) => {
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
	await page.route('**/api/Auth/signup', async (route) => {
		await route.fulfill({
			status: 409,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Email already used' })
		});
	});

	await page.goto('/signup', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.getByLabel('Display name').fill('Wordler');
	await page.getByLabel('Email').fill('player@example.com');
	await page.locator('input[name="password"]').fill('secret123');
	await page.locator('input[name="confirmPassword"]').fill('secret123');
	await page.getByRole('button', { name: 'Sign up' }).click();

	const errorBox = page.locator('form .rounded-xl');
	await expect(errorBox).toBeVisible();
	await expect(errorBox).toContainText('Email already used');
});
