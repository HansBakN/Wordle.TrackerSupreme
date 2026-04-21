import { expect, test } from '@playwright/test';

test('on-screen keyboard updates to reflect letter states after submitting a guess', async ({
	page
}) => {
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
								{ position: 1, letter: 'R', result: 'Present' },
								{ position: 2, letter: 'A', result: 'Absent' },
								{ position: 3, letter: 'N', result: 'Correct' },
								{ position: 4, letter: 'E', result: 'Absent' }
							]
						}
					]
				},
				solution: null
			})
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });

	// All keys should be unused before any guess
	await expect(page.getByTestId('keyboard-key-C')).toHaveAttribute('data-state', 'unused');
	await expect(page.getByTestId('keyboard-key-Z')).toHaveAttribute('data-state', 'unused');

	// Submit a guess
	await page.click('body');
	await page.keyboard.type('CRANE');
	await page.keyboard.press('Enter');

	// Keyboard should update to reflect the evaluated letters
	await expect(page.getByTestId('keyboard-key-C')).toHaveAttribute('data-state', 'absent');
	await expect(page.getByTestId('keyboard-key-R')).toHaveAttribute('data-state', 'present');
	await expect(page.getByTestId('keyboard-key-N')).toHaveAttribute('data-state', 'correct');
	await expect(page.getByTestId('keyboard-key-A')).toHaveAttribute('data-state', 'absent');
	await expect(page.getByTestId('keyboard-key-E')).toHaveAttribute('data-state', 'absent');
	// Unguessed letter stays unused
	await expect(page.getByTestId('keyboard-key-Z')).toHaveAttribute('data-state', 'unused');
});
