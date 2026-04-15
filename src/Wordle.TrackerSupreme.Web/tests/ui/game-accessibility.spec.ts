import { expect, test } from '@playwright/test';

test('game board exposes accessible grid semantics and live feedback', async ({ page }) => {
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
				cutoffPassed: true,
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
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('grid', { name: 'Wordle board' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Remove letter' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Submit guess' })).toBeVisible();

	const status = page.getByRole('status');
	await expect(status).toBeVisible();
	await expect(status).toContainText('You solved it in 1 guess');
});
