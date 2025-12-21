<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { enableEasyMode, fetchGameState, submitGuess } from '$lib/game/api';
	import type { GameStateResponse, LetterResult } from '$lib/game/types';
	import { onDestroy, onMount } from 'svelte';

	let checking = true;
	let loadingState = true;
	let state: GameStateResponse | null = null;
	let guess = '';
	let message: string | null = null;
	let error: string | null = null;
	let initialized = false;
	let submitting = false;
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

		onDestroy(() => {
			window.removeEventListener('keydown', keyHandler);
			unsubscribe();
		});
	});

	async function loadEverything() {
		await loadState();
	}

	async function loadState() {
		loadingState = true;
		error = null;
		try {
			state = await fetchGameState();
			message = null;
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load today’s puzzle.';
		} finally {
			loadingState = false;
		}
	}

	async function handleGuess() {
		if (!state || submitting) {
			return;
		}
		if (!state.canGuess) {
			return;
		}
		const targetLength = state.wordLength ?? 5;

		const normalized = guess.trim().toUpperCase();

		if (normalized.length !== targetLength) {
			error = `Guesses must be ${targetLength} letters.`;
			return;
		}

		error = null;
		message = null;
		submitting = true;
		try {
			state = await submitGuess(normalized);
			guess = '';
			message = null;
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to submit guess.';
		} finally {
			submitting = false;
		}
	}

	function pushLetter(letter: string) {
		if (!state || !state.canGuess) {
			return;
		}
		if (guess.length >= state.wordLength) {
			return;
		}
		guess = `${guess}${letter}`;
	}

	function removeLetter() {
		if (!state || !guess.length) {
			return;
		}
		guess = guess.slice(0, -1);
	}

	function submitFromKeyboard() {
		if (!state || !state.canGuess || submitting) {
			return;
		}
		void handleGuess();
	}

	async function handleEnableEasyMode() {
		if (!state || !state.isHardMode) {
			return;
		}
		if (state.attempt && state.attempt.status !== 'InProgress') {
			return;
		}
		error = null;
		message = null;
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

	function keyState(letter: string): LetterResult | 'Used' | null {
		if (!state?.attempt?.guesses?.length) {
			return null;
		}
		let best: LetterResult | 'Used' | null = null;

		for (const guessItem of state.attempt.guesses) {
			for (const fb of guessItem.feedback) {
				if (fb.letter !== letter) {
					continue;
				}
				if (fb.result === 'Correct') {
					return 'Correct';
				}
				if (fb.result === 'Present') {
					best = 'Present';
				} else if (!best) {
					best = 'Used';
				}
			}
			if (!best && guessItem.guessWord.includes(letter)) {
				best = 'Used';
			}
		}

		return best;
	}

	function keyClass(letter: string) {
		const base =
			'flex h-11 items-center justify-center rounded-xl border px-3 text-sm font-semibold uppercase transition';
		const stateKey = keyState(letter);
		if (stateKey === 'Correct') {
			return `${base} border-emerald-400 bg-emerald-400 text-slate-900`;
		}
		if (stateKey === 'Present') {
			return `${base} border-amber-300/70 bg-amber-300 text-slate-900`;
		}
		if (stateKey === 'Used') {
			return `${base} border-white/25 bg-white/50 text-slate-900`;
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
			class="rounded-3xl border border-white/10 bg-gradient-to-br from-white/10 to-white/5 p-8 shadow-2xl"
		>
			<div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
				<div>
					<p class="text-sm tracking-[0.2em] text-emerald-200/80 uppercase">Daily Wordle</p>
					<h1 class="mt-2 text-3xl font-semibold text-white">
						Puzzle for {formatPuzzleDate(state?.puzzleDate)}
					</h1>
					<p class="mt-2 max-w-2xl text-sm text-slate-200/80">
						Solve today’s word before noon to keep your streak alive. After 12:00 PM you can still
						play, but those practice runs will be tracked separately and won’t change your stats.
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
							(state.attempt && state.attempt.status !== 'InProgress')}
					>
						I am a little bitch boi
					</button>
				</div>
			</div>

			{#if loadingState}
				<div
					class="mt-8 rounded-2xl border border-white/10 bg-black/20 p-6 text-center text-slate-200/70"
				>
					Loading today’s puzzle...
				</div>
			{:else if state}
				<div class="mt-6">
					<div class="space-y-4 rounded-2xl border border-white/10 bg-black/20 p-6 shadow-inner">
						<div class="grid grid-rows-6 gap-1.5">
							{#each Array(state.maxGuesses).keys() as rowIndex (rowIndex)}
								<div
									class="grid justify-center gap-1.5"
									style={`grid-template-columns: repeat(${state.wordLength}, 3.5rem);`}
								>
									{#if state.attempt?.guesses[rowIndex]}
										{#each state.attempt.guesses[rowIndex].feedback as fb (fb.position)}
											<div
												class={`${tileClass(fb.result)} ${rowIndex === (state.attempt?.guesses.length ?? 0) - 1 ? `animate-reveal reveal-${fb.result.toLowerCase()}` : ''}`}
												style={`${rowIndex === (state.attempt?.guesses.length ?? 0) - 1 ? `animation-delay:${fb.position * 220}ms` : ''}`}
											>
												{fb.letter}
											</div>
										{/each}
									{:else}
										{#each Array(state.wordLength).keys() as col (col)}
											{#if rowIndex === (state.attempt?.guesses.length ?? 0)}
												<div class={tileClass(null)}>{(guess[col] ?? '').toUpperCase()}</div>
											{:else}
												<div class={tileClass(null)}></div>
											{/if}
										{/each}
									{/if}
								</div>
							{/each}
						</div>

						{#if message}
							<div
								class="rounded-xl border border-emerald-300/40 bg-emerald-400/10 px-4 py-3 text-sm text-emerald-50"
							>
								{message}
							</div>
						{/if}
						{#if error}
							<div
								class="rounded-xl border border-rose-400/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-50"
							>
								{error}
							</div>
						{/if}

						<div class="space-y-3 pt-3">
							{#each keyboardRows as row, rowIndex (rowIndex)}
								<div class="flex items-center justify-center gap-2">
									{#if rowIndex === 2}
										<button
											class="flex h-12 items-center justify-center rounded-xl border border-white/10 bg-white/5 px-4 text-xs font-semibold tracking-[0.15em] text-white/80 uppercase transition hover:border-white/30"
											onclick={removeLetter}
											disabled={!state.canGuess}
										>
											Back
										</button>
									{/if}
									{#each row.split('') as letter (letter)}
										<button
											class={keyClass(letter)}
											onclick={() => pushLetter(letter)}
											disabled={!state.canGuess}
										>
											{letter}
										</button>
									{/each}
									{#if rowIndex === 2}
										<button
											class="flex h-12 items-center justify-center rounded-xl border border-white/10 bg-white/5 px-4 text-xs font-semibold tracking-[0.15em] text-white/80 uppercase transition hover:border-white/30"
											onclick={submitFromKeyboard}
											disabled={!state.canGuess}
										>
											Enter
										</button>
									{/if}
								</div>
							{/each}
						</div>
					</div>
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
</style>
