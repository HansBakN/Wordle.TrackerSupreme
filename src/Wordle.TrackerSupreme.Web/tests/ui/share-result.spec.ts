import { expect, test } from '@playwright/test';

const solvedState = {
	puzzleDate: '2026-04-12',
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
		createdOn: '2026-04-12T08:00:00Z',
		completedOn: '2026-04-12T08:05:00Z',
		guesses: [
			{
				guessId: '33333333-3333-3333-3333-333333333333',
				guessNumber: 1,
				guessWord: 'CRANE',
				feedback: [
					{ position: 0, letter: 'C', result: 'Absent' },
					{ position: 1, letter: 'R', result: 'Present' },
					{ position: 2, letter: 'A', result: 'Absent' },
					{ position: 3, letter: 'N', result: 'Absent' },
					{ position: 4, letter: 'E', result: 'Absent' }
				]
			},
			{
				guessId: '44444444-4444-4444-4444-444444444444',
				guessNumber: 2,
				guessWord: 'PLANT',
				feedback: [
					{ position: 0, letter: 'P', result: 'Correct' },
					{ position: 1, letter: 'L', result: 'Correct' },
					{ position: 2, letter: 'A', result: 'Correct' },
					{ position: 3, letter: 'N', result: 'Correct' },
					{ position: 4, letter: 'T', result: 'Correct' }
				]
			}
		]
	},
	solution: null
};

test('copy result button appears after solving and writes emoji grid to clipboard', async ({
	page,
	context
}) => {
	await context.grantPermissions(['clipboard-read', 'clipboard-write']);

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
			body: JSON.stringify(solvedState)
		});
	});

	await page.route('**/api/stats/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				totalAttempts: 1,
				wins: 1,
				failures: 0,
				practiceAttempts: 0,
				currentStreak: 1,
				longestStreak: 1,
				averageGuessCount: 2
			})
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });

	const copyBtn = page.getByTestId('copy-result');
	await expect(copyBtn).toBeVisible();

	await copyBtn.click();

	await expect(copyBtn).toHaveText('✓ Copied!');

	const clipboardText = await page.evaluate(() => navigator.clipboard.readText());
	expect(clipboardText).toContain('Wordle Tracker Supreme');
	expect(clipboardText).toContain('2/6');
	expect(clipboardText).toContain('⬜🟨⬜⬜⬜');
	expect(clipboardText).toContain('🟩🟩🟩🟩🟩');
});
