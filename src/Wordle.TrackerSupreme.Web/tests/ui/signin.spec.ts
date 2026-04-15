import { expect, test } from '@playwright/test';

test('sign-in form shows validation and handles failed auth', async ({ page }) => {
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

test('sign-in fields remain editable after signing out from the game page', async ({ page }) => {
	let authMeCalls = 0;

	await page.route('**/api/Auth/me', async (route) => {
		authMeCalls += 1;

		if (authMeCalls === 1) {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					id: '11111111-1111-1111-1111-111111111111',
					displayName: 'Tester',
					email: 'tester@example.com',
					createdOn: '2025-01-01T00:00:00Z',
					isAdmin: false
				})
			});
			return;
		}

		await route.fulfill({
			status: 401,
			contentType: 'application/json',
			body: JSON.stringify({ message: 'Unauthorized' })
		});
	});

	await page.route('**/api/auth/signout', async (route) => {
		await route.fulfill({ status: 204, body: '' });
	});

	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleDate: '2025-01-01',
				cutoffPassed: false,
				solutionRevealed: false,
				allowLatePlay: false,
				wordLength: 5,
				maxGuesses: 6,
				isHardMode: false,
				canGuess: true,
				attempt: {
					attemptId: '22222222-2222-2222-2222-222222222222',
					status: 'InProgress',
					isAfterReveal: false,
					createdOn: '2025-01-01T00:00:00Z',
					completedOn: null,
					guesses: []
				},
				solution: null
			})
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible();

	await page.getByRole('button', { name: 'Sign out' }).click();
	await page.waitForURL('**/signin');

	const emailInput = page.getByLabel('Email');
	await emailInput.click();
	await emailInput.pressSequentially('player@example.com');

	await expect(emailInput).toHaveValue('player@example.com');
});
