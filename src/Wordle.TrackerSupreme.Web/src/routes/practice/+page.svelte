<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import {
		ApiResponseError,
		fetchPracticeState,
		startPracticeGame,
		submitPracticeGuess
	} from '$lib/game/api';
	import { describeTileForScreenReader } from '$lib/game/accessibility';
	import { getKeyboardLetterState } from '$lib/game/keyboard';
	import type { GuessResponse, LetterResult, PracticeStateResponse } from '$lib/game/types';
	import { onMount, tick } from 'svelte';

	let checking = true;
	let loading = true;
	let state: PracticeStateResponse | null = null;
	let guess = '';
	let message: string | null = null;
	let error: string | null = null;
	let initialized = false;
	let submitting = false;
	let animatedGuessId: string | null = null;
	let shakingRow: number | null = null;
	let shakeTimer: ReturnType<typeof setTimeout> | null = null;
	let announcement: string | null = null;
	let announcementIsError = false;
	const keyboardRows = ['QWERTYUIOP', 'ASDFGHJKL', 'ZXCVBNM'];

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
				await loadState();
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
			unsubscribe();
		};
	});

	$: guessInputLocked = !state || !state.canGuess || submitting;
	$: announcement = error ?? message;
	$: announcementIsError = error !== null;

	async function loadState() {
		loading = true;
		error = null;
		try {
			state = await fetchPracticeState();
			message = completedMessage(state);
		} catch (err) {
			if (err instanceof ApiResponseError && err.status === 404) {
				state = null;
			} else {
				error = err instanceof Error ? err.message : 'Unable to load practice game.';
			}
		} finally {
			loading = false;
		}
	}

	async function handleStartGame() {
		loading = true;
		error = null;
		message = null;
		try {
			state = await startPracticeGame();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to start practice game.';
		} finally {
			loading = false;
		}
	}

	async function handleNewGame() {
		state = null;
		guess = '';
		message = null;
		error = null;
		animatedGuessId = null;
		await handleStartGame();
	}

	function completedMessage(s: PracticeStateResponse | null): string | null {
		if (!s?.attempt) return null;
		const guessCount = s.attempt.guesses.length;
		if (s.attempt.status === 'Solved') {
			return `You solved it in ${guessCount} ${guessCount === 1 ? 'guess' : 'guesses'}!`;
		}
		if (s.attempt.status === 'Failed') {
			const word = s.solutionRevealed ? ` The word was ${s.solution}.` : '';
			return `No more guesses — better luck next time!${word}`;
		}
		return null;
	}

	function triggerShake() {
		if (shakeTimer) clearTimeout(shakeTimer);
		shakingRow = state?.attempt?.guesses.length ?? 0;
		shakeTimer = setTimeout(() => {
			shakingRow = null;
			shakeTimer = null;
		}, 600);
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
		const previousGuessId = currentState.attempt?.guesses.at(-1)?.guessId ?? null;
		submitting = true;
		await tick();
		try {
			state = await submitPracticeGuess(normalized);
			const latestGuessId = state.attempt?.guesses.at(-1)?.guessId ?? null;
			animatedGuessId = latestGuessId !== previousGuessId ? latestGuessId : null;
			guess = '';
			message = completedMessage(state);
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
</script>

<svelte:head>
	<title>Practice | Wordle Tracker Supreme</title>
</svelte:head>

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
			<div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
				<div>
					<p class="text-sm tracking-[0.2em] text-cyan-200/80 uppercase">Practice Mode</p>
					<h1 class="mt-2 text-3xl font-semibold text-white">Random Word Practice</h1>
					<p class="mt-2 max-w-2xl text-sm text-slate-200/80">
						Play unlimited practice games with random words. Practice games don't affect your stats,
						streaks, or leaderboard position.
					</p>
				</div>
				<a
					href={resolve('/')}
					class="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-xs font-semibold tracking-[0.2em] text-white/80 uppercase transition hover:border-white/40 hover:bg-white/10"
					data-testid="back-to-daily"
				>
					Daily puzzle
				</a>
			</div>

			{#if loading}
				<div
					class="mt-8 rounded-2xl border border-white/10 bg-black/20 p-6 text-center text-slate-200/70"
				>
					Loading...
				</div>
			{:else if !state}
				<div class="mt-8 flex flex-col items-center gap-4">
					<p class="text-slate-200/80">Ready to practice?</p>
					<button
						class="rounded-full border border-cyan-300/40 bg-cyan-400/10 px-6 py-3 text-sm font-semibold tracking-[0.2em] text-cyan-100 uppercase transition hover:border-cyan-300/60 hover:bg-cyan-400/20"
						onclick={handleStartGame}
						data-testid="start-practice"
					>
						Start practice game
					</button>
				</div>
			{:else}
				<div class="mt-6">
					<div class="space-y-4 rounded-2xl border border-white/10 bg-black/20 p-6 shadow-inner">
						<div class="grid grid-rows-6 gap-1.5" role="grid" aria-label="Practice board">
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

						{#if state?.attempt?.status === 'Solved' || state?.attempt?.status === 'Failed'}
							<div class="mt-4 flex justify-center">
								<button
									class="rounded-full border border-cyan-300/40 bg-cyan-400/10 px-6 py-3 text-sm font-semibold tracking-[0.2em] text-cyan-100 uppercase transition hover:border-cyan-300/60 hover:bg-cyan-400/20"
									onclick={handleNewGame}
									data-testid="new-practice-game"
								>
									New practice game
								</button>
							</div>
						{/if}
					</div>
				</div>
			{/if}

			{#if !loading && error && !state}
				<div
					class="mt-8 rounded-2xl border border-rose-400/50 bg-rose-500/10 p-6 text-center text-sm text-rose-100"
					data-testid="practice-error"
				>
					{error}
				</div>
			{/if}
		</section>
	</div>
{/if}
