import { test, expect } from '@playwright/test';

test('players can switch to easy mode mid-game', async ({ page }) => {
	await page.addInitScript(() => {
		window.localStorage.setItem('wts_auth_token', 'test-token');
	});

	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '11111111-1111-1111-1111-111111111111',
				displayName: 'Tester',
				email: 'tester@example.com',
				createdOn: '2025-01-01T00:00:00Z'
			})
		});
	});

	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleDate: '2025-01-01',
				cutoffPassed: false,
				solutionRevealed: false,
				allowLatePlay: true,
				wordLength: 5,
				maxGuesses: 6,
				isHardMode: true,
				canGuess: true,
				attempt: null,
				solution: null
			})
		});
	});

	await page.route('**/api/game/easy-mode', async (route) => {
		expect(route.request().method()).toBe('POST');
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleDate: '2025-01-01',
				cutoffPassed: false,
				solutionRevealed: false,
				allowLatePlay: true,
				wordLength: 5,
				maxGuesses: 6,
				isHardMode: false,
				canGuess: true,
				attempt: null,
				solution: null
			})
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByText('Hard mode')).toBeVisible();

	const easyModeButton = page.getByTestId('enable-easy-mode');
	await expect(easyModeButton).toBeEnabled();
	await easyModeButton.click();

	await expect(page.getByText('Easy mode', { exact: true })).toBeVisible();
	await expect(easyModeButton).toBeDisabled();
});
