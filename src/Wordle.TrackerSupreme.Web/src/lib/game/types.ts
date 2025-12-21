export type LetterResult = 'Absent' | 'Present' | 'Correct';

export type LetterFeedback = {
	position: number;
	letter: string;
	result: LetterResult;
};

export type GuessResponse = {
	guessId: string;
	guessNumber: number;
	guessWord: string;
	feedback: LetterFeedback[];
};

export type AttemptResponse = {
	attemptId: string;
	status: 'InProgress' | 'Solved' | 'Failed';
	isAfterReveal: boolean;
	createdOn: string;
	completedOn?: string | null;
	guesses: GuessResponse[];
};

export type GameStateResponse = {
	puzzleDate: string;
	cutoffPassed: boolean;
	solutionRevealed: boolean;
	allowLatePlay: boolean;
	wordLength: number;
	maxGuesses: number;
	isHardMode: boolean;
	canGuess: boolean;
	attempt?: AttemptResponse | null;
	solution?: string | null;
};

export type SolutionEntry = {
	playerId: string;
	displayName: string;
	status: 'InProgress' | 'Solved' | 'Failed';
	guessCount?: number | null;
	isAfterReveal: boolean;
	completedOn?: string | null;
	guesses: string[];
};

export type SolutionsResponse = {
	puzzleDate: string;
	solution: string;
	cutoffPassed: boolean;
	entries: SolutionEntry[];
};

export type PlayerStatsResponse = {
	totalAttempts: number;
	wins: number;
	failures: number;
	practiceAttempts: number;
	currentStreak: number;
	longestStreak: number;
	averageGuessCount?: number | null;
};
