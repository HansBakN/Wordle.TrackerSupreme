import { expect, test } from '@playwright/test';

test('leaderboard renders seeded entries for signed-in users', async ({ page }) => {
	const nonce = Date.now();
	await page.goto('/signup');
	await page.getByLabel('Display name').fill(`E2E Player ${nonce}`);
	await page.getByLabel('Email').fill(`e2e.${nonce}@example.com`);
	await page.getByLabel('Password', { exact: true }).fill('Supreme!234');
	await page.getByLabel('Confirm password').fill('Supreme!234');
	await page.getByRole('button', { name: 'Sign up' }).click();
	await page.waitForURL('**/');

	await page.goto('/leaderboard');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByRole('heading', { name: 'Hard mode before noon' })).toBeVisible();
	await expect(page.getByTestId('leaderboard-table')).toBeVisible();
	await expect(page.getByTestId('leaderboard-row').first()).toBeVisible();

	await page.getByRole('tab', { name: "Today's puzzle" }).click();

	await expect(page.getByRole('heading', { name: "Today's puzzle" })).toBeVisible();
	await expect(page.getByTestId('leaderboard-table')).toBeVisible();
	await expect(page.getByText('Solved').first()).toBeVisible();
	await expect(page.getByText('In progress').first()).toBeVisible();
});

test('leaderboard requests a new all-time sort when the selection changes', async ({ page }) => {
	const nonce = Date.now();
	await page.goto('/signup');
	await page.getByLabel('Display name').fill(`E2E Sorter ${nonce}`);
	await page.getByLabel('Email').fill(`e2e.sort.${nonce}@example.com`);
	await page.getByLabel('Password', { exact: true }).fill('Supreme!234');
	await page.getByLabel('Confirm password').fill('Supreme!234');
	await page.getByRole('button', { name: 'Sign up' }).click();
	await page.waitForURL('**/');

	const initialRequest = page.waitForRequest((request) => {
		return request.url().includes('/api/stats/leaderboard?page=1&pageSize=10');
	});

	await page.goto('/leaderboard');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await initialRequest;

	const sortedRequest = page.waitForRequest((request) => {
		return request.url().includes('/api/stats/leaderboard?page=1&pageSize=10&sortBy=wins');
	});

	await page.getByLabel('Sort leaderboard by').selectOption('wins');

	await sortedRequest;
	await expect(page.getByTestId('leaderboard-table')).toBeVisible();
	await expect(page.getByTestId('leaderboard-row').first()).toBeVisible();
});
