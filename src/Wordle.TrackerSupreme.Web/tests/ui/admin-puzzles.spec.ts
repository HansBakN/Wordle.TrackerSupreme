import { test, expect } from '@playwright/test';

const mockPuzzles = [
	{ id: 'puzzle-1', puzzleDate: '2025-06-01', solution: 'CRANE', attemptCount: 3 },
	{ id: 'puzzle-2', puzzleDate: '2025-06-02', solution: 'PLANT', attemptCount: 0 }
];

function setupAdminAuth(page: import('@playwright/test').Page) {
	return page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: 'admin-id',
				displayName: 'Admin',
				email: 'admin@example.com',
				isAdmin: true,
				createdOn: '2025-01-01T00:00:00Z'
			})
		});
	});
}

test.describe('admin puzzle management', () => {
	test('displays puzzle list', async ({ page }) => {
		await setupAdminAuth(page);
		await page.route('**/api/Admin/players', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
		});
		await page.route('**/api/Admin/puzzles', async (route) => {
			if (route.request().method() === 'GET') {
				await route.fulfill({
					status: 200,
					contentType: 'application/json',
					body: JSON.stringify(mockPuzzles)
				});
			}
		});

		await page.goto('/admin');
		await expect(page.getByTestId('admin-puzzles')).toBeVisible();
		await expect(page.getByTestId('admin-puzzle-row')).toHaveCount(2);
		await expect(page.getByTestId('admin-puzzle-row').first()).toContainText('CRANE');
	});

	test('can create a new puzzle', async ({ page }) => {
		await setupAdminAuth(page);
		await page.route('**/api/Admin/players', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
		});
		await page.route('**/api/Admin/puzzles', async (route) => {
			if (route.request().method() === 'GET') {
				await route.fulfill({
					status: 200,
					contentType: 'application/json',
					body: '[]'
				});
			} else if (route.request().method() === 'POST') {
				const body = route.request().postDataJSON();
				await route.fulfill({
					status: 201,
					contentType: 'application/json',
					body: JSON.stringify({
						id: 'new-puzzle',
						puzzleDate: body.puzzleDate,
						solution: body.solution.toUpperCase(),
						attemptCount: 0
					})
				});
			}
		});

		await page.goto('/admin');
		await page.getByTestId('admin-puzzles-create').click();
		await expect(page.getByTestId('admin-puzzle-form')).toBeVisible();

		await page.getByTestId('admin-puzzle-date').fill('2025-07-01');
		await page.getByTestId('admin-puzzle-solution').fill('GRAPE');
		await page.getByTestId('admin-puzzle-save').click();

		await expect(page.getByTestId('admin-puzzle-form')).not.toBeVisible();
		await expect(page.getByTestId('admin-puzzle-row')).toHaveCount(1);
		await expect(page.getByTestId('admin-puzzle-row').first()).toContainText('GRAPE');
	});

	test('locked puzzles cannot be edited or deleted', async ({ page }) => {
		await setupAdminAuth(page);
		await page.route('**/api/Admin/players', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
		});
		await page.route('**/api/Admin/puzzles', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify([
					{ id: 'puzzle-locked', puzzleDate: '2025-06-01', solution: 'CRANE', attemptCount: 5 }
				])
			});
		});

		await page.goto('/admin');
		await expect(page.getByTestId('admin-puzzle-row')).toHaveCount(1);
		await expect(page.getByText('Locked')).toBeVisible();
		await expect(page.getByTestId('admin-puzzle-edit')).not.toBeVisible();
		await expect(page.getByTestId('admin-puzzle-delete')).not.toBeVisible();
	});

	test('can delete a puzzle without attempts', async ({ page }) => {
		await setupAdminAuth(page);
		await page.route('**/api/Admin/players', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
		});
		await page.route('**/api/Admin/puzzles', async (route) => {
			if (route.request().method() === 'GET') {
				await route.fulfill({
					status: 200,
					contentType: 'application/json',
					body: JSON.stringify([
						{ id: 'puzzle-del', puzzleDate: '2025-06-05', solution: 'STORM', attemptCount: 0 }
					])
				});
			}
		});
		await page.route('**/api/Admin/puzzles/puzzle-del', async (route) => {
			if (route.request().method() === 'DELETE') {
				await route.fulfill({ status: 204 });
			}
		});

		await page.goto('/admin');
		await expect(page.getByTestId('admin-puzzle-row')).toHaveCount(1);
		await page.getByTestId('admin-puzzle-delete').click();
		await expect(page.getByTestId('admin-puzzle-row')).toHaveCount(0);
	});
});
