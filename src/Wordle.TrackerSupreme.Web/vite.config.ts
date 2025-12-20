import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import path from 'node:path';
import { defineConfig } from 'vite';

const isTest = process.env.VITEST === 'true';

export default defineConfig({
	plugins: [tailwindcss(), ...(isTest ? [] : [sveltekit()])],
	test: {
		environment: 'jsdom',
		globals: true,
		setupFiles: ['./vitest.setup.ts'],
		exclude: ['tests/ui/**', '**/node_modules/**'],
		resolveSnapshotPath: (testPath, snapExt) => testPath + snapExt,
		alias: {
			$lib: path.resolve('./src/lib')
		}
	}
});
