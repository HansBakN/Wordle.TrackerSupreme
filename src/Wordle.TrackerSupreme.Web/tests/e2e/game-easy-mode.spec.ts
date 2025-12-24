import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('player can enable easy mode for the daily puzzle', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Easy ${nonce}`,
		email: `e2e.easy.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.getByText('Loading today’s puzzle...').waitFor({ state: 'hidden' });
	await page.getByTestId('enable-easy-mode').click();
	await expect(page.getByText('Easy mode enabled for this puzzle.')).toBeVisible();
});
