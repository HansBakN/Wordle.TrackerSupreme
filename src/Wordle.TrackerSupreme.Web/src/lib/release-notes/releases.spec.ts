import { describe, expect, it } from 'vitest';
import { releaseNotes } from './releases';

describe('release notes', () => {
	it('keeps the upcoming release first with user-facing notes and operational notes', () => {
		expect(releaseNotes[0]).toMatchObject({
			slug: 'next-release',
			title: 'Next release',
			status: 'In progress'
		});
		expect(releaseNotes[0].userFacing.length).toBeGreaterThan(0);
		expect(releaseNotes[0].operations.length).toBeGreaterThan(0);
	});

	it('uses unique slugs for stable release note anchors', () => {
		const slugs = releaseNotes.map((release) => release.slug);
		expect(new Set(slugs).size).toBe(slugs.length);
	});
});
