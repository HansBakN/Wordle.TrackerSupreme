<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import {
		ApiResponseError,
		enableEasyMode,
		fetchGameState,
		fetchMyStats,
		submitGuess
	} from '$lib/game/api';
	import { describeTileForScreenReader } from '$lib/game/accessibility';
	import { getRevealDurationMs, shouldTriggerSolveCelebration } from '$lib/game/celebration';
	import {
		buildConfettiPieces,
		defaultConfettiPieceCount,
		type ConfettiPiece
	} from '$lib/game/confetti';
	import { getKeyboardLetterState } from '$lib/game/keyboard';
	import type {
		GameStateResponse,
		GuessResponse,
		LetterResult,
		PlayerStatsResponse
	} from '$lib/game/types';
	import { onMount, tick } from 'svelte';

	let checking = true;
	let loadingState = true;
	let state: GameStateResponse | null = null;
	let guess = '';
	let message: string | null = null;
	let error: string | null = null;
	let noPuzzleToday = false;
	let initialized = false;
	let submitting = false;
	let animatedGuessId: string | null = null;
	let showConfetti = false;
	let showWinStats = false;
	let winStats: PlayerStatsResponse | null = null;
	let statsError: string | null = null;
	let confettiPieces: ConfettiPiece[] = [];
	let confettiTimer: ReturnType<typeof setTimeout> | null = null;
	let statsTimer: ReturnType<typeof setTimeout> | null = null;
	let shakingRow: number | null = null;
	let shakeTimer: ReturnType<typeof setTimeout> | null = null;
	let countdownInterval: ReturnType<typeof setInterval> | null = null;
	let countdown = '';
	let announcement: string | null = null;
	let announcementIsError = false;
	const keyboardRows = ['QWERTYUIOP', 'ASDFGHJKL', 'ZXCVBNM'];
	const confettiDurationMs = 1200;

	onMount(() => {
		const unsubscribe = auth.subscribe(async (current) => {
			if (!current.ready) {
				return;
			}
			checking = false;

			if (!current.user) {
				goto(resolve('/signin'));
				return;
			}

			if (!initialized) {
				initialized = true;
				await loadEverything();
			}
		});

		const keyHandler = (evt: KeyboardEvent) => {
			if (!state) {
				return;
			}
			if (evt.key === 'Enter') {
				evt.preventDefault();
				void handleGuess();
				return;
			}
			if (evt.key === 'Backspace') {
				evt.preventDefault();
				removeLetter();
				return;
			}
			if (/^[a-zA-Z]$/.test(evt.key)) {
				evt.preventDefault();
				pushLetter(evt.key.toUpperCase());
			}
		};

		window.addEventListener('keydown', keyHandler);
		return () => {
			window.removeEventListener('keydown', keyHandler);
			stopCountdown();
			unsubscribe();
		};
	});

	$: {
		const status = state?.attempt?.status;
		if (status === 'Solved' || status === 'Failed') {
			startCountdown();
		} else {
			stopCountdown();
		}
	}

	$: guessInputLocked = !state || !state.canGuess || submitting;
	$: announcement = error ?? message;
	$: announcementIsError = error !== null;

	async function loadEverything() {
		await loadState();
	}

	async function loadState() {
		loadingState = true;
		error = null;
		noPuzzleToday = false;
		animatedGuessId = null;
		try {
			state = await fetchGameState();
			message = completedMessage(state);
		} catch (err) {
			if (
				err instanceof ApiResponseError &&
				err.status === 503 &&
				err.code === 'puzzle_unavailable'
			) {
				noPuzzleToday = true;
			} else {
				error = err instanceof Error ? err.message : "Unable to load today's puzzle.";
			}
		} finally {
			loadingState = false;
		}
	}

	function triggerShake() {
		if (shakeTimer) clearTimeout(shakeTimer);
		shakingRow = state?.attempt?.guesses.length ?? 0;
		shakeTimer = setTimeout(() => {
			shakingRow = null;
			shakeTimer = null;
		}, 600);
	}

	function completedMessage(s: GameStateResponse | null): string | null {
		if (!s?.attempt) return null;
		const guessCount = s.attempt.guesses.length;
		if (s.attempt.status === 'Solved') {
			return `You solved it in ${guessCount} ${guessCount === 1 ? 'guess' : 'guesses'}! Come back tomorrow.`;
		}
		if (s.attempt.status === 'Failed') {
			const word = s.solutionRevealed ? ` The word was ${s.solution}.` : '';
			return `No more guesses — better luck tomorrow!${word}`;
		}
		return null;
	}

	async function handleGuess() {
		if (isGuessInputLocked()) {
			return;
		}

		const currentState = state!;
		const targetLength = currentState.wordLength ?? 5;

		const normalized = guess.trim().toUpperCase();

		if (normalized.length !== targetLength) {
			error = `Guesses must be ${targetLength} letters.`;
			triggerShake();
			return;
		}

		error = null;
		message = null;
		const previousStatus = currentState.attempt?.status ?? null;
		const previousGuessId = currentState.attempt?.guesses.at(-1)?.guessId ?? null;
		submitting = true;
		await tick();
		try {
			state = await submitGuess(normalized);
			const latestGuessId = state.attempt?.guesses.at(-1)?.guessId ?? null;
			animatedGuessId = latestGuessId !== previousGuessId ? latestGuessId : null;
			guess = '';
			message = null;
			const nextStatus = state.attempt?.status ?? null;
			if (shouldTriggerSolveCelebration(previousStatus, nextStatus)) {
				void triggerWinCelebration();
			}
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to submit guess.';
			triggerShake();
		} finally {
			submitting = false;
		}
	}

	function isGuessInputLocked() {
		return !state || !state.canGuess || submitting;
	}

	function pushLetter(letter: string) {
		if (isGuessInputLocked()) {
			return;
		}

		const currentState = state!;
		if (guess.length >= currentState.wordLength) {
			return;
		}

		guess = `${guess}${letter}`;
	}

	function removeLetter() {
		if (!state || !guess.length || submitting) {
			return;
		}

		guess = guess.slice(0, -1);
	}

	function submitFromKeyboard() {
		if (isGuessInputLocked()) {
			return;
		}

		void handleGuess();
	}

	async function handleEnableEasyMode() {
		if (!state || !state.isHardMode || submitting) {
			return;
		}

		if (state.attempt && state.attempt.status !== 'InProgress') {
			return;
		}

		error = null;
		message = null;
		animatedGuessId = null;
		try {
			state = await enableEasyMode();
			message = 'Easy mode enabled for this puzzle.';
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to switch to easy mode.';
		}
	}

	function tileClass(result: LetterResult | null) {
		const base =
			'flex h-14 w-14 items-center justify-center rounded-xl border text-lg font-semibold transition';
		if (result === 'Correct') {
			return `${base} border-emerald-400 bg-emerald-400 text-slate-900 shadow-lg`;
		}
		if (result === 'Present') {
			return `${base} border-amber-300/70 bg-amber-300 text-slate-900 shadow`;
		}
		if (result === 'Absent') {
			return `${base} border-white/15 bg-white/5 text-white/60`;
		}
		return `${base} border-white/10 bg-white/5 text-white/30`;
	}

	function tileAnimationClass(guessId: string, result: LetterResult) {
		if (guessId !== animatedGuessId) {
			return '';
		}

		return `animate-reveal reveal-${result.toLowerCase()}`;
	}

	function tileAnimationDelay(guessId: string, position: number) {
		if (guessId !== animatedGuessId) {
			return '';
		}

		return `animation-delay:${position * 220}ms`;
	}

	function keyState(
		letter: string,
		guesses: GuessResponse[] | null | undefined
	): LetterResult | null {
		return getKeyboardLetterState(guesses, letter);
	}

	function keyClass(letter: string, guesses: GuessResponse[] | null | undefined) {
		const base =
			'flex h-11 items-center justify-center rounded-xl border px-3 text-sm font-semibold uppercase transition';
		const stateKey = keyState(letter, guesses);
		if (stateKey === 'Correct') {
			return `${base} border-emerald-400 bg-emerald-400 text-slate-900`;
		}
		if (stateKey === 'Present') {
			return `${base} border-amber-300/70 bg-amber-300 text-slate-900`;
		}
		if (stateKey === 'Absent') {
			return `${base} border-slate-500/70 bg-slate-600 text-slate-100`;
		}
		return `${base} border-white/60 bg-white/90 text-slate-900 shadow hover:border-white hover:bg-white`;
	}

	function formatPuzzleDate(value: string | null | undefined) {
		const date = value ? new Date(value) : new Date();
		const day = String(date.getDate()).padStart(2, '0');
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const year = date.getFullYear();
		return `${day}/${month}-${year}`;
	}

	function computeCountdown(): string {
		const now = new Date();
		const midnight = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
		const diffMs = midnight.getTime() - now.getTime();
		const totalSecs = Math.max(0, Math.floor(diffMs / 1000));
		const h = Math.floor(totalSecs / 3600);
		const m = Math.floor((totalSecs % 3600) / 60);
		const s = totalSecs % 60;
		return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
	}

	function startCountdown() {
		if (countdownInterval) return;
		countdown = computeCountdown();
		countdownInterval = setInterval(() => {
			countdown = computeCountdown();
		}, 1000);
	}

	function stopCountdown() {
		if (countdownInterval) {
			clearInterval(countdownInterval);
			countdownInterval = null;
		}
	}

	function resetCelebration() {
		if (confettiTimer) {
			clearTimeout(confettiTimer);
			confettiTimer = null;
		}
		if (statsTimer) {
			clearTimeout(statsTimer);
			statsTimer = null;
		}
		showConfetti = false;
		showWinStats = false;
		winStats = null;
		statsError = null;
	}

	async function triggerWinCelebration() {
		if (!state) {
			return;
		}
		resetCelebration();
		confettiPieces = buildConfettiPieces(defaultConfettiPieceCount);
		const revealDelayMs = getRevealDurationMs(state.wordLength ?? 5);
		const statsPromise = fetchMyStats().catch((err) => {
			statsError = err instanceof Error ? err.message : 'Unable to load stats.';
			return null;
		});

		confettiTimer = setTimeout(() => {
			showConfetti = true;
		}, revealDelayMs);

		statsTimer = setTimeout(async () => {
			showConfetti = false;
			winStats = await statsPromise;
			showWinStats = true;
		}, revealDelayMs + confettiDurationMs);
	}
</script>

{#if checking}
	<div
		class="rounded-2xl border border-white/10 bg-white/5 p-10 text-center text-slate-200/80 shadow-xl"
	>
		Checking your session...
	</div>
{:else if $auth.user}
	<div class="mx-auto grid max-w-6xl gap-6">
		<section
			class="relative rounded-3xl border border-white/10 bg-gradient-to-br from-white/10 to-white/5 p-8 shadow-2xl"
		>
			{#if showConfetti}
				<div class="confetti-layer" data-testid="confetti">
					{#each confettiPieces as piece (piece.id)}
						<span
							class="confetti-piece"
							style={`--dx:${piece.dx}px; --dy:${piece.dy}px; --rot:${piece.rotation}deg; --hue:${piece.hue}; --delay:${piece.delay}ms; --dur:${piece.duration}ms; --size:${piece.size}px;`}
						></span>
					{/each}
				</div>
			{/if}
			<div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
				<div>
					<p class="text-sm tracking-[0.2em] text-emerald-200/80 uppercase">Daily Wordle</p>
					<h1 class="mt-2 text-3xl font-semibold text-white">
						Puzzle for {formatPuzzleDate(state?.puzzleDate)}
					</h1>
					<p class="mt-2 max-w-2xl text-sm text-slate-200/80">
						Solve today's word before noon to keep your streak alive. After 12:00 PM you can still
						play, but those practice runs will be tracked separately and won't change your stats.
					</p>
				</div>
				<div class="flex flex-col items-start gap-3 sm:items-end">
					<span
						class={`rounded-full border px-3 py-1 text-xs font-semibold tracking-[0.2em] uppercase ${(state?.isHardMode ?? true) ? 'border-emerald-300/60 bg-emerald-400/10 text-emerald-100' : 'border-amber-300/50 bg-amber-400/10 text-amber-50'}`}
					>
						{(state?.isHardMode ?? true) ? 'Hard mode' : 'Easy mode'}
					</span>
					<button
						class="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-xs font-semibold tracking-[0.2em] text-white/80 uppercase transition hover:border-white/40 hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-40"
						onclick={handleEnableEasyMode}
						disabled={!state ||
							!state.isHardMode ||
							(state.attempt && state.attempt.status !== 'InProgress') ||
							submitting}
						data-testid="enable-easy-mode"
					>
						Enable easy mode
					</button>
				</div>
			</div>

			{#if loadingState}
				<div
					class="mt-8 rounded-2xl border border-white/10 bg-black/20 p-6 text-center text-slate-200/70"
				>
					Loading today's puzzle...
				</div>
			{:else if state}
				<div class="mt-6">
					<div class="space-y-4 rounded-2xl border border-white/10 bg-black/20 p-6 shadow-inner">
						<div class="grid grid-rows-6 gap-1.5" role="grid" aria-label="Wordle board">
							{#each Array(state.maxGuesses).keys() as rowIndex (rowIndex)}
								<div
									class={`grid justify-center gap-1.5${shakingRow === rowIndex ? ' animate-shake' : ''}`}
									style={`grid-template-columns: repeat(${state.wordLength}, 3.5rem);`}
									data-testid={`board-row-${rowIndex}`}
									role="row"
									aria-label={`Guess row ${rowIndex + 1}`}
								>
									{#if state.attempt?.guesses[rowIndex]}
										{#each state.attempt.guesses[rowIndex].feedback as fb (fb.position)}
											<div
												class={`${tileClass(fb.result)} ${tileAnimationClass(state.attempt.guesses[rowIndex].guessId, fb.result)}`}
												style={tileAnimationDelay(
													state.attempt.guesses[rowIndex].guessId,
													fb.position
												)}
												role="gridcell"
												aria-label={describeTileForScreenReader(fb.letter, fb.result)}
											>
												{fb.letter}
											</div>
										{/each}
									{:else}
										{#each Array(state.wordLength).keys() as col (col)}
											{#if rowIndex === (state.attempt?.guesses.length ?? 0)}
												<div
													class={tileClass(null)}
													role="gridcell"
													aria-label={describeTileForScreenReader(guess[col] ?? '', null)}
												>
													{(guess[col] ?? '').toUpperCase()}
												</div>
											{:else}
												<div
													class={tileClass(null)}
													role="gridcell"
													aria-label={describeTileForScreenReader('', null)}
												></div>
											{/if}
										{/each}
									{/if}
								</div>
							{/each}
						</div>

						{#if announcement}
							<div
								class={`rounded-xl border px-4 py-3 text-sm ${announcementIsError ? 'border-rose-400/40 bg-rose-500/10 text-rose-50' : state?.attempt?.status === 'Failed' ? 'border-amber-300/40 bg-amber-400/10 text-amber-50' : 'border-emerald-300/40 bg-emerald-400/10 text-emerald-50'}`}
								data-testid={announcementIsError ? 'error-message' : 'completed-message'}
								role="status"
								aria-live="polite"
								aria-atomic="true"
							>
								{announcement}
							</div>
						{/if}

						<div class="space-y-3 pt-3" role="group" aria-label="On-screen keyboard">
							{#each keyboardRows as row, rowIndex (rowIndex)}
								<div class="flex items-center justify-center gap-2">
									{#if rowIndex === 2}
										<button
											class="flex h-12 items-center justify-center rounded-xl border border-white/10 bg-white/5 px-4 text-xs font-semibold tracking-[0.15em] text-white/80 uppercase transition hover:border-white/30"
											onclick={removeLetter}
											disabled={guessInputLocked}
											data-testid="remove-letter"
											aria-label="Remove letter"
										>
											Back
										</button>
									{/if}
									{#each row.split('') as letter (letter)}
										<button
											class={keyClass(letter, state?.attempt?.guesses)}
											onclick={() => pushLetter(letter)}
											disabled={guessInputLocked}
											data-testid={`keyboard-key-${letter}`}
											data-state={(
												keyState(letter, state?.attempt?.guesses) ?? 'unused'
											).toLowerCase()}
										>
											{letter}
										</button>
									{/each}
									{#if rowIndex === 2}
										<button
											class="flex h-12 items-center justify-center rounded-xl border border-white/10 bg-white/5 px-4 text-xs font-semibold tracking-[0.15em] text-white/80 uppercase transition hover:border-white/30"
											onclick={submitFromKeyboard}
											disabled={guessInputLocked}
											data-testid="submit-guess"
											aria-label="Submit guess"
										>
											Enter
										</button>
									{/if}
								</div>
							{/each}
						</div>
						{#if showWinStats}
							<div
								class="mt-6 rounded-2xl border border-emerald-300/40 bg-emerald-500/10 p-5 text-white shadow-xl"
								data-testid="win-stats"
							>
								<h2 class="text-sm font-semibold tracking-[0.2em] text-emerald-100 uppercase">
									Victory stats
								</h2>
								{#if winStats}
									<div class="mt-4 grid gap-3 sm:grid-cols-3">
										<div>
											<div class="text-xs text-emerald-100/80 uppercase">Wins</div>
											<div class="text-xl font-semibold">{winStats.wins}</div>
										</div>
										<div>
											<div class="text-xs text-emerald-100/80 uppercase">Current streak</div>
											<div class="text-xl font-semibold">{winStats.currentStreak}</div>
										</div>
										<div>
											<div class="text-xs text-emerald-100/80 uppercase">Avg guesses</div>
											<div class="text-xl font-semibold">
												{winStats.averageGuessCount?.toFixed(2) ?? '—'}
											</div>
										</div>
									</div>
								{:else if statsError}
									<div class="mt-3 text-sm text-emerald-50/80">{statsError}</div>
								{/if}
							</div>
						{/if}
						{#if state?.attempt?.status === 'Solved' || state?.attempt?.status === 'Failed'}
							<div
								class="mt-4 rounded-xl border border-white/10 bg-white/5 px-4 py-3 text-center text-sm text-slate-200/80"
								data-testid="countdown-timer"
							>
								Next puzzle in: <span class="font-mono font-semibold text-white">{countdown}</span>
							</div>
						{/if}
					</div>
				</div>
			{/if}

			{#if !loadingState && noPuzzleToday}
				<div
					class="mt-8 rounded-2xl border border-amber-400/50 bg-amber-500/10 p-6 text-center text-amber-100"
					data-testid="no-puzzle-today"
				>
					<p class="text-base font-semibold">No puzzle available today.</p>
					<p class="mt-1 text-sm text-amber-100/80">
						Check back later — today's puzzle hasn't been scheduled yet.
					</p>
				</div>
			{:else if !loadingState && !state && error}
				<div
					class="mt-8 rounded-2xl border border-rose-400/50 bg-rose-500/10 p-6 text-center text-sm text-rose-100"
					data-testid="daily-puzzle-error"
				>
					{error ?? "Unable to load today's puzzle."}
				</div>
			{/if}
		</section>
	</div>
{/if}

<style>
	:global(.animate-reveal) {
		--start-bg: rgba(255, 255, 255, 0.08);
		--start-border: rgba(255, 255, 255, 0.15);
		--start-text: #e5e7eb;
		--end-bg: rgba(255, 255, 255, 0.08);
		--end-border: rgba(255, 255, 255, 0.15);
		--end-text: #e5e7eb;
		background: var(--start-bg);
		border-color: var(--start-border);
		color: var(--start-text);
		animation: reveal-flip 950ms ease-in-out forwards;
		transform-style: preserve-3d;
	}

	:global(.reveal-correct) {
		--end-bg: #34d399;
		--end-border: #34d399;
		--end-text: #0f172a;
	}

	:global(.reveal-present) {
		--end-bg: #fbbf24;
		--end-border: #fbbf24;
		--end-text: #0f172a;
	}

	:global(.reveal-absent) {
		--end-bg: rgba(255, 255, 255, 0.08);
		--end-border: rgba(255, 255, 255, 0.2);
		--end-text: rgba(255, 255, 255, 0.6);
	}

	@keyframes reveal-flip {
		0% {
			transform: rotateX(-90deg) scale(0.98);
			opacity: 0;
			background: var(--start-bg);
			border-color: var(--start-border);
			color: var(--start-text);
		}
		50% {
			transform: rotateX(0deg) scale(1);
			opacity: 1;
			background: var(--end-bg);
			border-color: var(--end-border);
			color: var(--end-text);
		}
		100% {
			transform: rotateX(0deg);
			opacity: 1;
			background: var(--end-bg);
			border-color: var(--end-border);
			color: var(--end-text);
		}
	}

	.confetti-layer {
		position: absolute;
		inset: 0;
		pointer-events: none;
		overflow: hidden;
	}

	.confetti-piece {
		position: absolute;
		left: 50%;
		top: 35%;
		width: var(--size);
		height: calc(var(--size) * 0.7);
		background: hsl(var(--hue) 80% 60%);
		border-radius: 999px;
		opacity: 0;
		transform: translate(-50%, -50%) rotate(var(--rot));
		animation: confetti-burst var(--dur) ease-out forwards;
		animation-delay: var(--delay);
	}

	@keyframes confetti-burst {
		0% {
			transform: translate(-50%, -50%) scale(0.8) rotate(var(--rot));
			opacity: 0;
		}
		15% {
			opacity: 1;
		}
		100% {
			transform: translate(calc(-50% + var(--dx)), calc(-50% + var(--dy))) rotate(var(--rot));
			opacity: 0;
		}
	}

	:global(.animate-shake) {
		animation: shake 600ms ease-in-out;
	}

	@keyframes shake {
		0%,
		100% {
			transform: translateX(0);
		}
		15% {
			transform: translateX(-8px);
		}
		30% {
			transform: translateX(7px);
		}
		45% {
			transform: translateX(-6px);
		}
		60% {
			transform: translateX(5px);
		}
		75% {
			transform: translateX(-3px);
		}
		90% {
			transform: translateX(2px);
		}
	}
</style>
