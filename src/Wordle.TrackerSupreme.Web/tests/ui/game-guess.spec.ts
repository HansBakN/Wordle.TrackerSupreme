import { test, expect } from '@playwright/test';

const GAME_STATE_STUB = {
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
};

const GUESS_RESPONSE_STUB = {
	...GAME_STATE_STUB,
	attempt: {
		attemptId: '22222222-2222-2222-2222-222222222222',
		status: 'InProgress',
		isAfterReveal: false,
		createdOn: '2025-01-01T00:00:00Z',
		completedOn: null,
		guesses: [
			{
				guessId: '33333333-3333-3333-3333-333333333333',
				guessNumber: 1,
				guessWord: 'CRANE',
				feedback: [
					{ position: 0, letter: 'C', result: 'Absent' },
					{ position: 1, letter: 'R', result: 'Absent' },
					{ position: 2, letter: 'A', result: 'Absent' },
					{ position: 3, letter: 'N', result: 'Absent' },
					{ position: 4, letter: 'E', result: 'Absent' }
				]
			}
		]
	}
};

async function setupPage(page: import('@playwright/test').Page) {
	page.on('pageerror', (error) => console.error('pageerror', error));
	page.on('console', (msg) => {
		if (msg.type() === 'error') console.error('console', msg.text());
	});

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
			body: JSON.stringify(GAME_STATE_STUB)
		});
	});
}

test('submitting a guess via keyboard Enter only calls the API once', async ({ page }) => {
	await setupPage(page);

	let guessRequests = 0;
	await page.route('**/api/game/guess', async (route) => {
		guessRequests += 1;
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(GUESS_RESPONSE_STUB)
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.click('body');
	await page.keyboard.type('CRANE');
	await page.keyboard.press('Enter');

	await expect.poll(() => guessRequests).toBe(1);
});

test('on-screen Enter button is disabled while a submission is in-flight', async ({ page }) => {
	await setupPage(page);

	// Use a slow route so we can inspect button state during the in-flight request
	let resolveGuess!: () => void;
	await page.route('**/api/game/guess', async (route) => {
		await new Promise<void>((resolve) => {
			resolveGuess = resolve;
		});
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(GUESS_RESPONSE_STUB)
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	// Type a word using the keyboard
	await page.click('body');
	await page.keyboard.type('CRANE');

	// Submit via keyboard Enter — this leaves the request in-flight
	await page.keyboard.press('Enter');

	// The on-screen Enter button must become disabled while submitting
	const enterButton = page.getByRole('button', { name: 'Enter' });
	await expect(enterButton).toBeDisabled();

	// Complete the request
	resolveGuess();
	await expect(enterButton).toBeEnabled({ timeout: 5000 });
});

test('clicking on-screen Enter while submitting does not send a second API request', async ({
	page
}) => {
	await setupPage(page);

	let guessRequests = 0;
	let resolveGuess!: () => void;
	await page.route('**/api/game/guess', async (route) => {
		guessRequests += 1;
		await new Promise<void>((resolve) => {
			resolveGuess = resolve;
		});
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(GUESS_RESPONSE_STUB)
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.click('body');
	await page.keyboard.type('CRANE');

	// First submission via keyboard
	await page.keyboard.press('Enter');

	// While in-flight, attempt to click the on-screen Enter button
	const enterButton = page.getByRole('button', { name: 'Enter' });
	await expect(enterButton).toBeDisabled();
	// Force-click to bypass the disabled attribute — this simulates the bug
	await enterButton.click({ force: true });

	// Allow the first request to complete
	resolveGuess();

	// Confirm only exactly one request was ever made
	await expect.poll(() => guessRequests).toBe(1);
});
