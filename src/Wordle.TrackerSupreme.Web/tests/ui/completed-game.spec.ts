import { expect, test } from '@playwright/test';

const authRoutes = async (page: import('@playwright/test').Page) => {
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
};

test('shows solved message when revisiting a completed winning attempt', async ({ page }) => {
	await authRoutes(page);

	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
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

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const msg = page.getByTestId('completed-message');
	await expect(msg).toBeVisible();
	await expect(msg).toContainText('You solved it in 1 guess');
	await expect(msg).toContainText('Come back tomorrow');
});

test('shows failure message with solution when revisiting a failed attempt', async ({ page }) => {
	await authRoutes(page);

	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				puzzleDate: '2025-01-01',
				cutoffPassed: true,
				solutionRevealed: true,
				allowLatePlay: true,
				wordLength: 5,
				maxGuesses: 6,
				isHardMode: true,
				canGuess: false,
				attempt: {
					attemptId: '22222222-2222-2222-2222-222222222222',
					status: 'Failed',
					isAfterReveal: false,
					createdOn: '2025-01-01T00:00:00Z',
					completedOn: '2025-01-01T00:10:00Z',
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
				solution: 'STOMP'
			})
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	const msg = page.getByTestId('completed-message');
	await expect(msg).toBeVisible();
	await expect(msg).toContainText('better luck tomorrow');
	await expect(msg).toContainText('STOMP');
});
