import { expect, test } from '@playwright/test';

test('shows confetti and win stats after solving', async ({ page }) => {
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

	await page.route('**/api/game/guess', async (route) => {
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
				canGuess: false,
				attempt: {
					attemptId: '22222222-2222-2222-2222-222222222222',
					status: 'Solved',
					isAfterReveal: false,
					createdOn: '2025-01-01T00:00:00Z',
					completedOn: '2025-01-01T00:05:00Z',
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
		});
	});

	await page.route('**/api/stats/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				totalAttempts: 10,
				wins: 8,
				failures: 2,
				practiceAttempts: 1,
				currentStreak: 4,
				longestStreak: 6,
				averageGuessCount: 3.5
			})
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await page.click('body');
	await page.keyboard.type('CRANE');
	await page.keyboard.press('Enter');

	await page.getByTestId('confetti').waitFor({ state: 'visible', timeout: 5000 });
	await page.getByTestId('win-stats').waitFor({ state: 'visible', timeout: 7000 });

	await expect(page.getByTestId('win-stats')).toContainText('Wins');
	await expect(page.getByTestId('win-stats')).toContainText('Current streak');
});
