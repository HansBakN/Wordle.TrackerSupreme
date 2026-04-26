import { expect, test } from '@playwright/test';

test('release notes page is publicly reachable from the header', async ({ page }) => {
	await page.addInitScript(() => {
		localStorage.setItem('wts_hasSeenHowToPlay', '1');
	});
	await page.goto('/');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.locator('a[href="/release-notes"]').first().click();

	await expect(page).toHaveURL(/\/release-notes$/);
	await expect(page.getByRole('heading', { name: 'Release notes' })).toBeVisible();
	await expect(page.getByText('Next release')).toBeVisible();
	await expect(page.getByText('Release branch workflow')).toBeVisible();
});
