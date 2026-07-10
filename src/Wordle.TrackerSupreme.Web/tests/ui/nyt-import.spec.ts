import { expect, test } from '@playwright/test';

const player = {
	id: '11111111-1111-1111-1111-111111111111',
	displayName: 'Tester',
	email: 'tester@example.com',
	createdOn: '2025-01-01T00:00:00Z',
	isAdmin: false
};

test('opens import page, creates a code and displays completed summary', async ({ page }) => {
	await page.route('**/api/Auth/me', (route) =>
		route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(player) })
	);
	await page.route('**/api/import/nyt/session**', async (route) => {
		if (route.request().method() === 'POST') {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					sessionId: '22222222-2222-2222-2222-222222222222',
					code: 'ABCD-EFGH',
					expiresAt: new Date(Date.now() + 600_000).toISOString()
				})
			});
		} else {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					sessionId: '22222222-2222-2222-2222-222222222222',
					expiresAt: new Date(Date.now() + 600_000).toISOString(),
					completedAt: new Date().toISOString(),
					aggregateGamesPlayed: 8,
					requested: 5,
					imported: 2,
					duplicates: 1,
					conflicts: 1,
					rejected: 1,
					missingFromNyt: 3
				})
			});
		}
	});
	await page.goto('/import/nyt');
	await expect(page.getByTestId('nyt-import-page')).toBeVisible();
	await page.getByTestId('generate-import-code').click();
	await expect(page.getByTestId('import-code')).toHaveText('ABCD-EFGH');
	await expect(page.getByTestId('import-countdown')).not.toHaveText('0:00');
	await expect(page.getByTestId('import-summary')).toBeVisible({ timeout: 5000 });
	await expect(page.getByTestId('incomplete-history')).toContainText('unavailable or unknown');
});

test('shows a sign-in action without creating a session when unauthenticated', async ({ page }) => {
	await page.route('**/api/Auth/me', (route) => route.fulfill({ status: 401, body: '{}' }));
	await page.goto('/import/nyt');
	const prompt = page.getByTestId('nyt-import-unauthenticated');
	await expect(prompt).toBeVisible();
	await expect(prompt.getByRole('link', { name: 'Sign in' })).toBeVisible();
});
