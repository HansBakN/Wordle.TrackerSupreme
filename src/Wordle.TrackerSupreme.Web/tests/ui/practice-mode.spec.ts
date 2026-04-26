import { expect, test } from '@playwright/test';

test('practice page shows start button when no active game', async ({ page }) => {
	await page.route('**/api/Auth/me', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '00000000-0000-0000-0000-000000000001',
				displayName: 'TestPlayer',
				email: 'test@example.com',
				isAdmin: false
			})
		})
	);
	await page.route('**/api/practice/state', (route) =>
		route.fulfill({
			status: 404,
			contentType: 'application/json',
			body: JSON.stringify({ status: 404, detail: 'No active practice game.' })
		})
	);

	await page.goto('/practice', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('start-practice')).toBeVisible();
});

test('practice page shows game board after starting a game', async ({ page }) => {
	await page.route('**/api/Auth/me', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '00000000-0000-0000-0000-000000000001',
				displayName: 'TestPlayer',
				email: 'test@example.com',
				isAdmin: false
			})
		})
	);

	let started = false;
	await page.route('**/api/practice/state', (route) => {
		if (!started) {
			return route.fulfill({
				status: 404,
				contentType: 'application/json',
				body: JSON.stringify({ status: 404, detail: 'No active practice game.' })
			});
		}
		return route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleId: '11111111-1111-1111-1111-111111111111',
				solutionRevealed: false,
				wordLength: 5,
				maxGuesses: 6,
				canGuess: true,
				attempt: {
					attemptId: '22222222-2222-2222-2222-222222222222',
					status: 'InProgress',
					isAfterReveal: true,
					createdOn: '2025-01-01T00:00:00Z',
					guesses: []
				},
				solution: null
			})
		});
	});
	await page.route('**/api/practice/start', (route) => {
		started = true;
		return route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleId: '11111111-1111-1111-1111-111111111111',
				solutionRevealed: false,
				wordLength: 5,
				maxGuesses: 6,
				canGuess: true,
				attempt: {
					attemptId: '22222222-2222-2222-2222-222222222222',
					status: 'InProgress',
					isAfterReveal: true,
					createdOn: '2025-01-01T00:00:00Z',
					guesses: []
				},
				solution: null
			})
		});
	});

	await page.goto('/practice', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });
	await page.getByTestId('start-practice').click();

	await expect(page.getByTestId('board-row-0')).toBeVisible();
	await expect(page.getByTestId('keyboard-key-A')).toBeVisible();
});

test('practice page shows new game button after completion', async ({ page }) => {
	await page.route('**/api/Auth/me', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '00000000-0000-0000-0000-000000000001',
				displayName: 'TestPlayer',
				email: 'test@example.com',
				isAdmin: false
			})
		})
	);
	await page.route('**/api/practice/state', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleId: '11111111-1111-1111-1111-111111111111',
				solutionRevealed: true,
				wordLength: 5,
				maxGuesses: 6,
				canGuess: false,
				attempt: {
					attemptId: '22222222-2222-2222-2222-222222222222',
					status: 'Solved',
					isAfterReveal: true,
					createdOn: '2025-01-01T00:00:00Z',
					completedOn: '2025-01-01T00:01:00Z',
					guesses: [
						{
							guessId: '33333333-3333-3333-3333-333333333333',
							guessNumber: 1,
							guessWord: 'CRANE',
							feedback: [
								{ position: 0, letter: 'C', result: 'Correct' },
								{ position: 1, letter: 'R', result: 'Correct' },
								{ position: 2, letter: 'A', result: 'Correct' },
								{ position: 3, letter: 'N', result: 'Correct' },
								{ position: 4, letter: 'E', result: 'Correct' }
							]
						}
					]
				},
				solution: 'CRANE'
			})
		})
	);

	await page.goto('/practice', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('new-practice-game')).toBeVisible();
	await expect(page.getByTestId('completed-message')).toContainText('You solved it in 1 guess');
});

test('daily puzzle page shows practice mode link after completion', async ({ page }) => {
	await page.route('**/api/Auth/me', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				id: '00000000-0000-0000-0000-000000000001',
				displayName: 'TestPlayer',
				email: 'test@example.com',
				isAdmin: false
			})
		})
	);
	await page.route('**/api/game/state', (route) =>
		route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleDate: '2025-01-01',
				cutoffPassed: false,
				solutionRevealed: true,
				allowLatePlay: true,
				wordLength: 5,
				maxGuesses: 6,
				isHardMode: true,
				canGuess: false,
				attempt: {
					attemptId: '22222222-2222-2222-2222-222222222222',
					status: 'Solved',
					isAfterReveal: false,
					createdOn: '2025-01-01T00:00:00Z',
					completedOn: '2025-01-01T00:01:00Z',
					guesses: [
						{
							guessId: '33333333-3333-3333-3333-333333333333',
							guessNumber: 1,
							guessWord: 'CRANE',
							feedback: [
								{ position: 0, letter: 'C', result: 'Correct' },
								{ position: 1, letter: 'R', result: 'Correct' },
								{ position: 2, letter: 'A', result: 'Correct' },
								{ position: 3, letter: 'N', result: 'Correct' },
								{ position: 4, letter: 'E', result: 'Correct' }
							]
						}
					]
				},
				solution: 'CRANE'
			})
		})
	);

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('practice-mode-link')).toBeVisible();
});
