import { expect, test } from '@playwright/test';
import { signUp } from './helpers';

test('keeps Tracker Supreme and NYT attempts isolated', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Streams ${nonce}`,
		email: `e2e.streams.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await expect(page.getByRole('button', { name: 'Tracker Supreme puzzle' })).toHaveAttribute(
		'aria-pressed',
		'true'
	);
	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });

	await page.click('body');
	await page.keyboard.type('CRANE');
	const trackerGuessResponse = page.waitForResponse((response) => {
		return response.url().includes('/api/game/guess') && response.ok();
	});
	await page.keyboard.press('Enter');
	await trackerGuessResponse;
	await expect(page.getByTestId('board-row-0')).toContainText('CRANE');

	await page.getByRole('button', { name: 'New York Times puzzle' }).click();
	await expect(page.getByRole('button', { name: 'New York Times puzzle' })).toHaveAttribute(
		'aria-pressed',
		'true'
	);
	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });
	await expect(page.getByTestId('board-row-0')).not.toContainText('CRANE');

	await page.click('body');
	await page.keyboard.type('SLATE');
	const nytGuessResponse = page.waitForResponse((response) => {
		return (
			response.url().includes('/api/game/guess') &&
			response.url().includes('stream=NewYorkTimes') &&
			response.ok()
		);
	});
	await page.keyboard.press('Enter');
	await nytGuessResponse;
	await expect(page.getByTestId('board-row-0')).toContainText('SLATE');

	await page.getByRole('button', { name: 'Tracker Supreme puzzle' }).click();
	await page.getByTestId('board-row-0').waitFor({ state: 'visible' });
	await expect(page.getByTestId('board-row-0')).toContainText('CRANE');
	await expect(page.getByTestId('board-row-0')).not.toContainText('SLATE');
});
