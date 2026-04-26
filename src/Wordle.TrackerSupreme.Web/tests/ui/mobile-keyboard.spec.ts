import { expect, test } from '@playwright/test';

test('mobile layout keeps board in viewport and submits with on-screen keyboard', async ({
	page
}) => {
	const viewport = { width: 375, height: 640 };
	await page.setViewportSize(viewport);

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

	let submittedGuess: string | undefined;
	await page.route('**/api/game/guess', async (route) => {
		submittedGuess = route.request().postDataJSON().guess;
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

	const firstRow = page.getByTestId('board-row-0');
	await expect(firstRow).toBeVisible();

	const rowBox = await firstRow.boundingBox();
	expect(rowBox).not.toBeNull();
	expect(rowBox!.x).toBeGreaterThanOrEqual(0);
	expect(rowBox!.x + rowBox!.width).toBeLessThanOrEqual(viewport.width);

	const keyboard = page.getByRole('group', { name: 'On-screen keyboard' });
	const keyboardBox = await keyboard.boundingBox();
	expect(keyboardBox).not.toBeNull();
	expect(keyboardBox!.x).toBeGreaterThanOrEqual(0);
	expect(keyboardBox!.x + keyboardBox!.width).toBeLessThanOrEqual(viewport.width);
	expect(keyboardBox!.y + keyboardBox!.height).toBeGreaterThanOrEqual(viewport.height - 16);
	expect(keyboardBox!.y + keyboardBox!.height).toBeLessThanOrEqual(viewport.height);

	for (const letter of ['C', 'R', 'A', 'N', 'E']) {
		await page.getByTestId(`keyboard-key-${letter}`).click();
	}

	await expect(firstRow).toContainText('CRANE');
	await page.getByTestId('submit-guess').click();

	await expect.poll(() => submittedGuess).toBe('CRANE');
	await expect(page.getByTestId('keyboard-key-N')).toHaveAttribute('data-state', 'correct');
});
