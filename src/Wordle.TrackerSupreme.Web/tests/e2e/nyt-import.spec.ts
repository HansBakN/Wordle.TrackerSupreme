import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('signed-in player can create a short-lived NYT import session', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E NYT Import ${nonce}`,
		email: `e2e.nyt.import.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.goto('/import/nyt');
	await expect(page.getByTestId('nyt-import-page')).toBeVisible();
	await page.getByTestId('generate-import-code').click();
	await expect(page.getByTestId('import-code')).toHaveText(/^[A-Z2-9]{4}-[A-Z2-9]{4}$/);
	await expect(page.getByTestId('import-countdown')).not.toHaveText('0:00');
	await expect(page.getByTestId('import-progress')).toContainText('Waiting for the extension');
});
