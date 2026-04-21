import { expect, test } from '@playwright/test';

test('defaults to Tracker Supreme and switches to isolated NYT game state', async ({ page }) => {
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

	const stateRequests: string[] = [];
	await page.route('**/api/game/state**', async (route) => {
		const url = new URL(route.request().url());
		const stream = url.searchParams.get('stream') ?? 'TrackerSupreme';
		stateRequests.push(stream);
		const isNyt = stream === 'NewYorkTimes';

		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify({
				stream: isNyt ? 'NewYorkTimes' : 'TrackerSupreme',
				puzzleDate: '2025-01-01',
				cutoffPassed: false,
				solutionRevealed: false,
				allowLatePlay: true,
				wordLength: 5,
				maxGuesses: 6,
				isHardMode: true,
				canGuess: true,
				attempt: isNyt
					? {
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
						}
					: null,
				solution: null
			})
		});
	});

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Checking your session...').waitFor({ state: 'hidden' });

	await expect(page.getByRole('button', { name: 'Tracker Supreme puzzle' })).toHaveAttribute(
		'aria-pressed',
		'true'
	);
	await expect.poll(() => stateRequests).toEqual(['TrackerSupreme']);
	await expect(page.getByTestId('board-row-0')).not.toContainText('CRANE');

	await page.getByRole('button', { name: 'New York Times puzzle' }).click();

	await expect(page.getByRole('button', { name: 'New York Times puzzle' })).toHaveAttribute(
		'aria-pressed',
		'true'
	);
	await expect.poll(() => stateRequests).toEqual(['TrackerSupreme', 'NewYorkTimes']);
	await expect(page.getByTestId('board-row-0')).toContainText('CRANE');
});
