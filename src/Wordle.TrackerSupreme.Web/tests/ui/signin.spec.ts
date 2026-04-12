import { test, expect } from '@playwright/test';

test('sign-in form shows validation and handles failed auth', async ({ page }) => {
	await page.addInitScript(() => {
		window.localStorage.clear();
	});
	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 401,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Unauthorized' })
		});
	});
	await page.route('**/api/Auth/signin', async (route) => {
		await route.fulfill({
			status: 401,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Invalid credentials' })
		});
	});

	await page.goto('/signin', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();

	await page.getByLabel('Email').fill('player@example.com');
	await page.getByLabel('Password').fill('secret123');
	await page.getByRole('button', { name: 'Sign in' }).click();

	const errorBox = page.locator('form .rounded-xl');
	await expect(errorBox).toBeVisible();
	await expect(errorBox).not.toHaveText('');
});

test('keyboard input works on sign-in form after visiting the game page', async ({ page }) => {
	// Simulate having been authenticated (game page was mounted with its keydown handler)
	await page.addInitScript(() => {
		window.localStorage.setItem('wts_auth_token', 'test-token');
	});

	const PLAYER_RESPONSE = {
		id: '11111111-1111-1111-1111-111111111111',
		displayName: 'Tester',
		email: 'tester@example.com',
		createdOn: '2025-01-01T00:00:00Z'
	};

	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(PLAYER_RESPONSE)
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

	// Land on game page — this mounts the component with its window keydown handler
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByRole('heading', { name: /Puzzle for/ })).toBeVisible();

	// Sign out: clear the token so subsequent auth checks return 401, then navigate away
	await page.evaluate(() => {
		window.localStorage.removeItem('wts_auth_token');
	});
	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({ status: 401, contentType: 'application/json', body: '{}' });
	});

	// Navigate to sign-in — this should destroy the game page and remove its keydown listener
	await page.goto('/signin', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	// Type into the email field using keyboard events
	// If the game page's keydown handler is still alive it will preventDefault on letters,
	// and the field will remain empty.
	const emailInput = page.getByLabel('Email');
	await emailInput.click();
	await page.keyboard.type('hello@example.com');
	await expect(emailInput).toHaveValue('hello@example.com');
});
