import type { Page } from '@playwright/test';

type SignUpInput = {
	displayName: string;
	email: string;
	password: string;
};

export async function signUp(page: Page, input: SignUpInput) {
	await page.goto('/signup');
	await page.getByLabel('Display name').fill(input.displayName);
	await page.getByLabel('Email').fill(input.email);
	await page.getByLabel('Password', { exact: true }).fill(input.password);
	await page.getByLabel('Confirm password').fill(input.password);
	await page.getByRole('button', { name: 'Sign up' }).click();
	await page.waitForURL('**/');
}
