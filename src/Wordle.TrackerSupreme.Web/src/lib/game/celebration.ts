export type AttemptStatus = 'InProgress' | 'Solved' | 'Failed';

const letterRevealDelayMs = 220;
const letterRevealDurationMs = 950;

export function getRevealDurationMs(wordLength: number): number {
	const safeLength = Number.isFinite(wordLength) && wordLength > 0 ? wordLength : 1;
	return Math.max(0, safeLength - 1) * letterRevealDelayMs + letterRevealDurationMs;
}

export function shouldTriggerSolveCelebration(
	previousStatus: AttemptStatus | null | undefined,
	nextStatus: AttemptStatus | null | undefined
): boolean {
	return previousStatus !== 'Solved' && nextStatus === 'Solved';
}
