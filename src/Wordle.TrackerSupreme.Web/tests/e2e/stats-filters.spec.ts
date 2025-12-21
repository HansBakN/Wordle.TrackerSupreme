import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('stats apply sends chosen filter payload', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Filters ${nonce}`,
		email: `e2e.filters.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.goto('/stats');
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('stats-apply')).toBeEnabled();
	await expect(page.getByTestId('stats-results')).toBeVisible();

	await page.getByTestId('filter-hard-mode').setChecked(false);
	await page.getByTestId('filter-easy-mode').setChecked(true);
	await page.getByTestId('filter-before-reveal').setChecked(false);
	await page.getByTestId('filter-after-reveal').setChecked(true);
	await page.getByTestId('filter-count-practice').setChecked(true);
	await page.getByTestId('filter-solved').setChecked(true);
	await page.getByTestId('filter-failed').setChecked(false);
	await page.getByTestId('filter-in-progress').setChecked(true);
	await page.getByTestId('filter-from-date').fill('2025-01-01');
	await page.getByTestId('filter-to-date').fill('2025-02-01');
	await page.getByTestId('filter-min-guesses').fill('2');
	await page.getByTestId('filter-max-guesses').fill('5');

	const responsePromise = page.waitForResponse((response) => {
		return response.url().includes('/api/stats/players') && response.request().method() === 'POST';
	});
	await page.getByTestId('stats-apply').click();
	const response = await responsePromise;
	const payload = response.request().postDataJSON() as Record<string, unknown>;

	expect(payload).toMatchObject({
		includeHardMode: false,
		includeEasyMode: true,
		includeBeforeReveal: false,
		includeAfterReveal: true,
		includeSolved: true,
		includeFailed: false,
		includeInProgress: true,
		countPracticeAttempts: true,
		fromDate: '2025-01-01',
		toDate: '2025-02-01',
		minGuessCount: 2,
		maxGuessCount: 5
	});
});
