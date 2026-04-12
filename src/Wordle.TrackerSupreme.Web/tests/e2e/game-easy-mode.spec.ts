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

test('guess controls stay locked while a guess is submitting', async ({ page }) => {
	const nonce = Date.now();
	await signUp(page, {
		displayName: `E2E Submit ${nonce}`,
		email: `e2e.submit.${nonce}@example.com`,
		password: 'Supreme!234'
	});

	await page.getByText('Loading today’s puzzle...').waitFor({ state: 'hidden' });

	let guessRequests = 0;
	let resolveGuessResponse!: () => void;
	const guessResponseCompleted = new Promise<void>((resolve) => {
		resolveGuessResponse = resolve;
	});
	await page.route('**/api/game/guess', async (route) => {
		guessRequests += 1;
		await page.waitForTimeout(400);
		const response = await route.fetch();
		await route.fulfill({ response });
		resolveGuessResponse();
	});

	await page.click('body');
	await page.keyboard.type('CRANE');
	const submitPromise = page.keyboard.press('Enter');

	await expect.poll(() => guessRequests).toBe(1);
	await expect(page.getByTestId('submit-guess')).toBeDisabled();
	await expect(page.getByTestId('remove-letter')).toBeDisabled();
	await expect(page.getByTestId('enable-easy-mode')).toBeDisabled();

	await page.keyboard.press('Backspace');
	await expect(page.getByTestId('board-row-0')).toContainText('CRANE');
	await page.keyboard.press('Enter');
	await expect.poll(() => guessRequests).toBe(1);

	await submitPromise;
	await guessResponseCompleted;
	await expect.poll(() => guessRequests).toBe(1);
	await page.unroute('**/api/game/guess');
});
