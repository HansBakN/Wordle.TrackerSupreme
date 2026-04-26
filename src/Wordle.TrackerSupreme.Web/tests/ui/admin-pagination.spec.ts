import { expect, test } from '@playwright/test';

test('admin roster sends search and pagination query parameters', async ({ page }) => {
	const requests: Array<{ search: string | null; page: string | null; pageSize: string | null }> =
		[];

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

	await page.route('**/api/Admin/players**', async (route) => {
		const url = new URL(route.request().url());
		requests.push({
			search: url.searchParams.get('search'),
			page: url.searchParams.get('page'),
			pageSize: url.searchParams.get('pageSize')
		});

		const requestedPage = Number(url.searchParams.get('page') ?? '1');
		const search = url.searchParams.get('search');
		const player =
			search === 'beta'
				? {
						id: '22222222-2222-2222-2222-222222222222',
						displayName: 'Beta Player',
						email: 'beta@example.com',
						createdOn: '2025-01-03T00:00:00Z',
						isAdmin: false,
						attemptCount: 1
					}
				: {
						id:
							requestedPage === 2
								? '33333333-3333-3333-3333-333333333333'
								: '11111111-1111-1111-1111-111111111111',
						displayName: requestedPage === 2 ? 'Second Page' : 'First Page',
						email: requestedPage === 2 ? 'second@example.com' : 'first@example.com',
						createdOn: '2025-01-02T00:00:00Z',
						isAdmin: false,
						attemptCount: requestedPage
					};

		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				players: [player],
				totalCount: search === 'beta' ? 1 : 21,
				page: requestedPage,
				pageSize: 20
			})
		});
	});

	await page.goto('/admin', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.locator('[data-testid="admin-page"]')).toBeVisible();
	await expect(page.getByText('First Page')).toBeVisible();
	await expect(page.locator('[data-testid="admin-page-indicator"]')).toContainText('Page 1 of 2');

	await page.locator('[data-testid="admin-next-page"]').click();
	await expect(page.getByText('Second Page')).toBeVisible();

	await page.locator('[data-testid="admin-search"]').fill('beta');
	await expect(page.getByText('Beta Player')).toBeVisible();

	expect(requests).toContainEqual({ search: null, page: '1', pageSize: '20' });
	expect(requests).toContainEqual({ search: null, page: '2', pageSize: '20' });
	expect(requests).toContainEqual({ search: 'beta', page: '1', pageSize: '20' });
});
