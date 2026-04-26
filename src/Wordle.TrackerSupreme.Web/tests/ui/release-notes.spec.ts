import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
	await page.addInitScript(() => {
		localStorage.setItem('wts_hasSeenHowToPlay', '1');
	});
});

test('release notes page renders the upcoming release summary', async ({ page }) => {
	await page.goto('/release-notes', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page).toHaveURL('/release-notes');
	await expect(page.getByRole('heading', { name: 'Release notes' })).toBeVisible();
	await expect(page.getByText('Next release')).toBeVisible();
	await expect(page.getByText('Release branch workflow')).toBeVisible();
});

test('release notes page keeps its core content visible on mobile', async ({ page }) => {
	await page.setViewportSize({ width: 390, height: 844 });
	await page.goto('/release-notes', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page).toHaveURL('/release-notes');
	await expect(page.getByRole('heading', { name: 'Release notes' })).toBeVisible();
});
