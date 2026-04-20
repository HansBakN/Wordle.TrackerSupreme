import { expect, test } from '@playwright/test';

const MOCK_USER = {
	id: '11111111-1111-1111-1111-111111111111',
	displayName: 'Tester',
	email: 'tester@example.com',
	createdOn: '2025-01-01T00:00:00Z',
	isAdmin: false
};

const MOCK_GAME_STATE = {
	puzzleDate: '2025-01-01',
	cutoffPassed: false,
	solutionRevealed: false,
	allowLatePlay: false,
	wordLength: 5,
	maxGuesses: 6,
	isHardMode: true,
	canGuess: true,
	attempt: null,
	solution: null
};

async function setupRoutes(page: import('@playwright/test').Page) {
	await page.addInitScript(() => {
		localStorage.setItem('wts_enableHowToPlayAutoOpen', '1');
	});
	await page.route('**/api/Auth/me', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(MOCK_USER)
		});
	});
	await page.route('**/api/game/state', async (route) => {
		await route.fulfill({
			status: 200,
			contentType: 'application/json',
			body: JSON.stringify(MOCK_GAME_STATE)
		});
	});
}

test('shows how-to-play modal on first visit when localStorage key is absent', async ({
	page,
	context
}) => {
	await context.clearCookies();
	await setupRoutes(page);

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByTestId('how-to-play-modal')).toBeVisible();
	await expect(page.getByText('How to play')).toBeVisible();
});

test('does not show modal on revisit after dismissal', async ({ page, context }) => {
	await context.clearCookies();
	await setupRoutes(page);

	// Simulate returning visitor by pre-seeding localStorage
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.evaluate(() => localStorage.setItem('wts_hasSeenHowToPlay', '1'));
	await page.reload({ waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByTestId('how-to-play-modal')).not.toBeVisible();
});

test('closes modal when Close button is clicked and sets localStorage', async ({
	page,
	context
}) => {
	await context.clearCookies();
	await setupRoutes(page);

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('how-to-play-modal')).toBeVisible();

	await page.getByTestId('close-how-to-play').click();
	await expect(page.getByTestId('how-to-play-modal')).not.toBeVisible();

	const stored = await page.evaluate(() => localStorage.getItem('wts_hasSeenHowToPlay'));
	expect(stored).toBe('1');
});

test('closes modal when Got it button is clicked', async ({ page, context }) => {
	await context.clearCookies();
	await setupRoutes(page);

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('how-to-play-modal')).toBeVisible();

	await page.getByTestId('got-it-button').click();
	await expect(page.getByTestId('how-to-play-modal')).not.toBeVisible();
});

test('closes modal when Escape key is pressed', async ({ page, context }) => {
	await context.clearCookies();
	await setupRoutes(page);

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	await expect(page.getByTestId('how-to-play-modal')).toBeVisible();

	await page.keyboard.press('Escape');
	await expect(page.getByTestId('how-to-play-modal')).not.toBeVisible();
});

test('help icon in header opens the modal', async ({ page, context }) => {
	await context.clearCookies();
	await setupRoutes(page);

	// Dismiss the auto-open first
	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.evaluate(() => localStorage.setItem('wts_hasSeenHowToPlay', '1'));
	await page.reload({ waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });

	await expect(page.getByTestId('how-to-play-modal')).not.toBeVisible();

	await page.getByTestId('open-how-to-play').click();
	await expect(page.getByTestId('how-to-play-modal')).toBeVisible();
});

test('modal explains tile colours and game modes', async ({ page, context }) => {
	await context.clearCookies();
	await setupRoutes(page);

	await page.goto('/', { waitUntil: 'domcontentloaded' });
	await page.getByText('Loading your session...').waitFor({ state: 'hidden' });
	const modal = page.getByTestId('how-to-play-modal');
	await expect(modal).toBeVisible();

	// Tile colour explanations
	await expect(modal.getByText('Green', { exact: true })).toBeVisible();
	await expect(modal.getByText('Yellow', { exact: true })).toBeVisible();
	await expect(modal.getByText('Grey', { exact: true })).toBeVisible();

	// Game mode explanations
	await expect(modal.getByText('Hard mode', { exact: true })).toBeVisible();
	await expect(modal.getByText('Easy mode', { exact: true })).toBeVisible();

	// Daily cutoff
	await expect(modal.getByText('12:00 PM local time')).toBeVisible();
});
