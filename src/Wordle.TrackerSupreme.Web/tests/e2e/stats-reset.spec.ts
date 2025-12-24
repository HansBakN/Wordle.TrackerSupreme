import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('stats reset restores default filter state', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Reset ${nonce}`,
		email: `e2e.reset.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.goto('/stats');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.getByTestId('filter-easy-mode').setChecked(true);
	await page.getByTestId('filter-after-reveal').setChecked(true);
	await page.getByTestId('filter-min-guesses').fill('3');

	const responsePromise = page.waitForResponse((response) => {
		return response.url().includes('/api/stats/players') && response.request().method() === 'POST';
	});

	await page.getByTestId('stats-reset').click();
	const response = await responsePromise;
	const payload = response.request().postDataJSON();

	expect(payload).toMatchObject({
		includeHardMode: true,
		includeEasyMode: false,
		includeBeforeReveal: true,
		includeAfterReveal: false,
		includeSolved: true,
		includeFailed: true,
		includeInProgress: false,
		countPracticeAttempts: false
	});

	await expect(page.getByTestId('filter-easy-mode')).not.toBeChecked();
	await expect(page.getByTestId('filter-after-reveal')).not.toBeChecked();
	await expect(page.getByTestId('filter-min-guesses')).toHaveValue('');
});
