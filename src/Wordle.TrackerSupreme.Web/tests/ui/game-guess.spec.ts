import { test, expect } from '@playwright/test';

test('submitting a guess only calls the API once', async ({ page }) => {
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

	let guessRequests = 0;
	await page.route('**/api/game/guess', async (route) => {
		guessRequests += 1;
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
				},
				solution: null
			})
		});
	});

	await page.goto('/');

	await page.keyboard.type('CRANE');
	await page.keyboard.press('Enter');

	await expect.poll(() => guessRequests).toBe(1);
});
