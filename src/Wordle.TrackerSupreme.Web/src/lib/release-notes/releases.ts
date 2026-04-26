export type ReleaseNote = {
	slug: string;
	title: string;
	status: 'In progress' | 'Released';
	date: string;
	summary: string;
	userFacing: string[];
	operations: string[];
};

export const releaseNotes: ReleaseNote[] = [
	{
		slug: 'next-release',
		title: 'Next release',
		status: 'In progress',
		date: 'Release branch',
		summary:
			'Features and fixes staged for the next deployment are collected here before they are promoted to main.',
		userFacing: [
			'Player-facing changes are summarized in plain language before each deploy.',
			'Fixes are grouped with the feature area they affect so testers can scan what changed.'
		],
		operations: [
			'Merge completed work into a release branch first, then update this page before the release branch is merged to main.',
			'Keep verification notes in the pull request; keep this page focused on what changed for players and operators.'
		]
	}
];
