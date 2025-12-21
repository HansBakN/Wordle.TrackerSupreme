import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('user can sign up, sign out, and sign back in', async ({ page }) => {
	const nonce = Date.now();
	const credentials = {
		displayName: `E2E Auth ${nonce}`,
		email: `e2e.auth.${nonce}@example.com`,
		password: 'Supreme!234'
	};

	await signUp(page, credentials);
	await expect(page.getByText(credentials.displayName)).toBeVisible();

	await page.getByRole('button', { name: 'Sign out' }).click();
	await expect(page.getByRole('link', { name: 'Sign in' })).toBeVisible();

	await page.goto('/signin');
	await page.getByLabel('Email').fill(credentials.email);
	await page.getByLabel('Password').fill(credentials.password);
	await page.getByRole('button', { name: 'Sign in' }).click();
	await page.waitForURL('**/');

	await expect(page.getByText(credentials.displayName)).toBeVisible();
	await expect(page.getByText('Signed in')).toBeVisible();
});
