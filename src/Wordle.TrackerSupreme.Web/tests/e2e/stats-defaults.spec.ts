import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('stats page loads with default filters and results', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Stats ${nonce}`,
		email: `e2e.stats.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.goto('/stats');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('stats-apply')).toBeEnabled();

	await expect(page.getByTestId('filter-hard-mode')).toBeChecked();
	await expect(page.getByTestId('filter-easy-mode')).not.toBeChecked();
	await expect(page.getByTestId('filter-before-reveal')).toBeChecked();
	await expect(page.getByTestId('filter-after-reveal')).not.toBeChecked();
	await expect(page.getByTestId('filter-count-practice')).not.toBeChecked();
	await expect(page.getByTestId('filter-solved')).toBeChecked();
	await expect(page.getByTestId('filter-failed')).toBeChecked();
	await expect(page.getByTestId('filter-in-progress')).not.toBeChecked();
	await expect(page.getByTestId('filter-from-date')).toHaveValue('');
	await expect(page.getByTestId('filter-to-date')).toHaveValue('');
	await expect(page.getByTestId('filter-min-guesses')).toHaveValue('');
	await expect(page.getByTestId('filter-max-guesses')).toHaveValue('');

	await expect(page.getByTestId('stats-results')).toBeVisible();
	await expect(page.getByTestId('stats-card').first()).toBeVisible();
});
