import type { Page } from '@playwright/test';

type SignUpInput = {
	displayName: string;
	email: string;
	password: string;
};

export async function signUp(page: Page, input: SignUpInput) {
	await page.goto('/signup');
	await page.getByLabel('Display name').fill(input.displayName);
	await page.getByLabel('Email').fill(input.email);
	await page.getByLabel('Password', { exact: true }).fill(input.password);
	await page.getByLabel('Confirm password').fill(input.password);
	await page.getByRole('button', { name: 'Sign up' }).click();
	await page.waitForURL('**/');
}

/**
 * Compute today's puzzle solution using the same deterministic formula as the backend
 * WordSelector. The word list and anchor date must stay in sync with
 * Wordle.TrackerSupreme.Application.Services.Game.WordSelector.
 */
export function getTodaySolution(): string {
	const words = [
		'SLATE',
		'CRANE',
		'BRAVE',
		'TRAIN',
		'SHINE',
		'GLASS',
		'FROND',
		'QUIET',
		'PLANT',
		'ROAST',
		'TRAIL',
		'SNAKE',
		'CLOUD',
		'BRINK',
		'DRIVE',
		'STEAM',
		'WATER',
		'GRAPE',
		'PANEL',
		'CROWN',
		'STARE',
		'GHOST',
		'PLUSH',
		'MONEY',
		'LIGHT',
		'RANGE',
		'BRICK',
		'FLAME',
		'WOUND',
		'SCORE',
		'CHIME',
		'PRIDE',
		'STONE',
		'HOUSE',
		'PIVOT',
		'CHALK',
		'FROST',
		'BLINK',
		'SHARD',
		'TOWEL',
		'NORTH',
		'SOUTH',
		'EAGER',
		'QUEST',
		'FRAME',
		'GRIND',
		'WRIST',
		'TRICK',
		'VOICE',
		'YEARN'
	];
	const anchor = new Date(2025, 0, 1);
	const now = new Date();
	const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
	const msPerDay = 24 * 60 * 60 * 1000;
	const offset = Math.round((today.getTime() - anchor.getTime()) / msPerDay);
	const index = ((offset % words.length) + words.length) % words.length;
	return words[index];
}
