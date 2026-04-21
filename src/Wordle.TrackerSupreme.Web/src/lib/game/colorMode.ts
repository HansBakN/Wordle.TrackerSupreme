import { writable } from 'svelte/store';

const STORAGE_KEY = 'wts_highContrastMode';

function createColorModeStore() {
	let current = false;
	const { subscribe, set } = writable(false);

	return {
		subscribe,
		init() {
			if (typeof localStorage !== 'undefined') {
				current = localStorage.getItem(STORAGE_KEY) === '1';
				set(current);
			}
		},
		toggle() {
			current = !current;
			localStorage.setItem(STORAGE_KEY, current ? '1' : '0');
			set(current);
		}
	};
}

export const colorMode = createColorModeStore();
