import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { get } from 'svelte/store';

describe('colorMode store', () => {
	beforeEach(() => {
		localStorage.clear();
	});

	afterEach(() => {
		localStorage.clear();
	});

	it('defaults to false when localStorage has no value', async () => {
		const { colorMode } = await import('./colorMode');
		colorMode.init();
		expect(get(colorMode)).toBe(false);
	});

	it('reads true from localStorage when key is set to 1', async () => {
		localStorage.setItem('wts_highContrastMode', '1');
		const { colorMode } = await import('./colorMode');
		colorMode.init();
		expect(get(colorMode)).toBe(true);
	});

	it('toggle flips the value and persists to localStorage', async () => {
		const { colorMode } = await import('./colorMode');
		colorMode.init();
		expect(get(colorMode)).toBe(false);

		colorMode.toggle();
		expect(get(colorMode)).toBe(true);
		expect(localStorage.getItem('wts_highContrastMode')).toBe('1');

		colorMode.toggle();
		expect(get(colorMode)).toBe(false);
		expect(localStorage.getItem('wts_highContrastMode')).toBe('0');
	});
});
