import { expect, test } from '@playwright/test';

test('admin attempt cards identify puzzle streams', async ({ page }) => {
	await page.addInitScript(() => {
		window.localStorage.setItem('wts_auth_token', 'test-token');
	});

	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '11111111-1111-1111-1111-111111111111',
				displayName: 'Admin',
				email: 'admin@example.com',
				createdOn: '2025-01-01T00:00:00Z',
				isAdmin: true
			})
		});
	});

	await page.route('**/api/Admin/players', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify([
				{
					id: '22222222-2222-2222-2222-222222222222',
					displayName: 'Stream Player',
					email: 'stream@example.com',
					createdOn: '2025-01-02T00:00:00Z',
					isAdmin: false,
					attemptCount: 2
				}
			])
		});
	});

	await page.route('**/api/Admin/players/22222222-2222-2222-2222-222222222222', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '22222222-2222-2222-2222-222222222222',
				displayName: 'Stream Player',
				email: 'stream@example.com',
				createdOn: '2025-01-02T00:00:00Z',
				isAdmin: false,
				attempts: [
					{
						attemptId: '33333333-3333-3333-3333-333333333333',
						puzzleDate: '2025-02-01',
						stream: 'TrackerSupreme',
						status: 'Solved',
						playedInHardMode: true,
						createdOn: '2025-02-01T09:00:00Z',
						completedOn: '2025-02-01T09:20:00Z',
						guesses: []
					},
					{
						attemptId: '44444444-4444-4444-4444-444444444444',
						puzzleDate: '2025-02-01',
						stream: 'NewYorkTimes',
						status: 'Failed',
						playedInHardMode: false,
						createdOn: '2025-02-01T10:00:00Z',
						completedOn: '2025-02-01T10:20:00Z',
						guesses: []
					}
				]
			})
		});
	});

	await page.goto('/admin', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });
	await page.getByRole('button', { name: /Stream Player/ }).click();

	const attemptCards = page.getByTestId('admin-attempt-card');
	await expect(attemptCards).toHaveCount(2);
	await expect(attemptCards.first()).toContainText('Tracker Supreme');
	await expect(attemptCards.nth(1)).toContainText('New York Times');
});
