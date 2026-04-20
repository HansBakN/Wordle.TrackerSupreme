import { expect, test } from '@playwright/test';

test('release notes page is publicly reachable from the header', async ({ page }) => {
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.getByRole('link', { name: 'Release notes' }).click();

	await expect(page).toHaveURL('/release-notes');
	await expect(page.getByRole('heading', { name: 'Release notes' })).toBeVisible();
	await expect(page.getByText('Next release')).toBeVisible();
	await expect(page.getByText('Release branch workflow')).toBeVisible();
});

test('release notes page is reachable from the mobile header', async ({ page }) => {
	await page.setViewportSize({ width: 390, height: 844 });
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.getByRole('link', { name: 'Notes' }).click();

	await expect(page).toHaveURL('/release-notes');
	await expect(page.getByRole('heading', { name: 'Release notes' })).toBeVisible();
});
