<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth, signOut } from '$lib/auth/store';
	import { fetchGameState, fetchMyStats, fetchSolutions, submitGuess } from '$lib/game/api';
	import type { GameStateResponse, LetterResult, PlayerStatsResponse, SolutionsResponse } from '$lib/game/types';
	import { onMount } from 'svelte';

	let checking = true;
	let loadingState = true;
	let loadingGuess = false;
	let state: GameStateResponse | null = null;
	let solutions: SolutionsResponse | null = null;
	let stats: PlayerStatsResponse | null = null;
	let guess = '';
	let message: string | null = null;
	let error: string | null = null;
	let initialized = false;

	onMount(() => {
		const unsubscribe = auth.subscribe(async (current) => {
			if (!current.ready) return;
			checking = false;

			if (!current.user) {
				goto('/signin');
				return;
			}

			if (!initialized) {
				initialized = true;
				await loadEverything();
			}
		});

		return () => unsubscribe();
	});

	async function loadEverything() {
		await loadState();
		await loadStats();
	}

	async function loadState() {
		loadingState = true;
		error = null;
		try {
			state = await fetchGameState();
			message = state.solutionRevealed && state.solution ? `Solution: ${state.solution}` : null;
			if (state.cutoffPassed) {
				await loadSolutions();
			}
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load today’s puzzle.';
		} finally {
			loadingState = false;
		}
	}

	async function loadSolutions() {
		try {
			solutions = await fetchSolutions();
		} catch (err) {
			// If we’re before the cutoff this will 403 – ignore and wait.
			if ((err as Error)?.message) {
				console.debug('Solutions unavailable yet:', (err as Error).message);
			}
		}
	}

	async function loadStats() {
		try {
			stats = await fetchMyStats();
		} catch (err) {
			console.debug('Stats unavailable', err);
		}
	}

	async function handleGuess() {
		if (!state) return;
		const targetLength = state.wordLength ?? 5;

		const normalized = guess.trim().toUpperCase();

		if (normalized.length !== targetLength) {
			error = `Guesses must be ${targetLength} letters.`;
			return;
		}

		error = null;
		message = null;
		loadingGuess = true;
		try {
			state = await submitGuess(normalized);
			guess = '';
			if (state.solutionRevealed && state.solution) {
				message = `Solution: ${state.solution}`;
				await loadSolutions();
			}
			await loadStats();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to submit guess.';
		} finally {
			loadingGuess = false;
		}
	}

	function tileClass(result: LetterResult | null) {
		const base = 'flex h-14 items-center justify-center rounded-xl border text-lg font-semibold transition';
		if (result === 'Correct') return `${base} border-emerald-400 bg-emerald-400 text-slate-900 shadow-lg`;
		if (result === 'Present') return `${base} border-amber-300/70 bg-amber-300 text-slate-900 shadow`;
		if (result === 'Absent') return `${base} border-white/15 bg-white/5 text-white/60`;
		return `${base} border-white/10 bg-white/5 text-white/30`;
	}

	function statusCopy(status: string | undefined) {
		if (!status) return 'In progress';
		if (status === 'Solved') return 'Solved';
		if (status === 'Failed') return 'Failed';
		return 'In progress';
	}

	function statusBadge(status: string | undefined) {
		if (status === 'Solved') return 'bg-emerald-500/20 text-emerald-100 border-emerald-300/40';
		if (status === 'Failed') return 'bg-rose-500/15 text-rose-100 border-rose-400/30';
		return 'bg-white/5 text-white border-white/15';
	}
</script>

{#if checking}
	<div class="rounded-2xl border border-white/10 bg-white/5 p-10 text-center text-slate-200/80 shadow-xl">
		Checking your session...
	</div>
{:else if $auth.user}
	<div class="grid gap-6 lg:grid-cols-3">
		<section class="lg:col-span-2 rounded-3xl border border-white/10 bg-gradient-to-br from-white/10 to-white/5 p-8 shadow-2xl">
			<div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
				<div>
					<p class="text-sm uppercase tracking-[0.2em] text-emerald-200/80">Daily Wordle</p>
					<h1 class="mt-2 text-3xl font-semibold text-white">Puzzle for {state ? new Date(state.puzzleDate).toLocaleDateString() : new Date().toLocaleDateString()}</h1>
					<p class="mt-2 max-w-2xl text-sm text-slate-200/80">
						Solve today’s word before noon to keep your streak alive. After 12:00 PM you can still play, but
						those practice runs will be tracked separately and won’t change your stats.
					</p>
				</div>
				<button
					class="self-start rounded-full border border-white/20 bg-white/10 px-4 py-2 text-sm font-semibold text-white transition hover:border-white/40 hover:bg-white/15"
					onclick={() => signOut()}
				>
					Sign out
				</button>
			</div>

			{#if loadingState}
				<div class="mt-8 rounded-2xl border border-white/10 bg-black/20 p-6 text-center text-slate-200/70">
					Loading today’s puzzle...
				</div>
			{:else if state}
				<div class="mt-6 flex flex-wrap items-center gap-3 text-sm text-slate-200/80">
					<span class={`rounded-full border px-3 py-1 ${statusBadge(state.attempt?.status)}`}>
						{statusCopy(state.attempt?.status)}
					</span>
					{#if state.cutoffPassed}
						<span class="rounded-full border border-amber-300/40 bg-amber-400/15 px-3 py-1 text-amber-50">
							Solutions unlocked — new plays are practice only
						</span>
					{:else}
						<span class="rounded-full border border-emerald-300/30 bg-emerald-400/15 px-3 py-1 text-emerald-50">
							Counts toward streaks until 12:00 PM
						</span>
					{/if}
					<span class="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-white/80">
						Max {state.maxGuesses} guesses · {state.wordLength}-letter words
					</span>
				</div>

				<div class="mt-6 grid gap-6 lg:grid-cols-[1.4fr_1fr]">
					<div class="space-y-4 rounded-2xl border border-white/10 bg-black/20 p-6 shadow-inner">
						<div class="grid grid-rows-6 gap-2">
							{#each Array(state.maxGuesses) as _, rowIndex}
								<div class="grid gap-2" style={`grid-template-columns: repeat(${state.wordLength}, minmax(0, 1fr));`}>
									{#if state.attempt?.guesses[rowIndex]}
										{#each state.attempt.guesses[rowIndex].feedback as fb}
											<div class={tileClass(fb.result)}>{fb.letter}</div>
										{/each}
									{:else}
										{#each Array(state.wordLength) as __, col}
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
							<div class="rounded-xl border border-emerald-300/40 bg-emerald-400/10 px-4 py-3 text-sm text-emerald-50">
								{message}
							</div>
						{/if}
						{#if error}
							<div class="rounded-xl border border-rose-400/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-50">
								{error}
							</div>
						{/if}

						<div class="flex flex-col gap-3 sm:flex-row sm:items-center">
							<input
								name="guess"
								bind:value={guess}
								maxlength={state.wordLength}
								minlength={state.wordLength}
								class="w-full rounded-2xl border border-white/15 bg-white/5 px-4 py-3 text-lg font-semibold uppercase tracking-[0.3em] text-white outline-none transition focus:border-emerald-300 focus:bg-black/30"
								placeholder="Enter guess"
								onkeydown={(evt) => {
									if (evt.key === 'Enter') {
										evt.preventDefault();
										handleGuess();
									}
								}}
							/>
							<button
								class="w-full rounded-2xl bg-emerald-400 px-4 py-3 text-center text-base font-semibold text-slate-900 transition hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60 sm:w-auto"
								onclick={handleGuess}
								disabled={loadingGuess || !state.canGuess}
							>
								{loadingGuess ? 'Checking...' : 'Submit guess'}
							</button>
						</div>
						<p class="text-xs text-slate-200/60">
							You can always submit after noon to practice — those attempts are marked and won’t affect your
							streaks.
						</p>
					</div>

					<div class="space-y-4">
						<div class="rounded-2xl border border-white/10 bg-white/5 p-5">
							<div class="text-sm uppercase tracking-[0.2em] text-emerald-200/80">Today’s status</div>
							<div class="mt-2 text-2xl font-semibold text-white">{statusCopy(state.attempt?.status)}</div>
							<div class="mt-2 text-sm text-slate-200/80">
								{#if state.attempt?.isAfterReveal}
									Counted as practice — logged separately from stats.
								{:else if state.cutoffPassed}
									Any new attempts now are practice only; your earlier results stay intact.
								{:else}
									Aim to finish before noon to keep your streak alive.
								{/if}
							</div>
						</div>

						{#if stats}
							<div class="grid grid-cols-2 gap-3 rounded-2xl border border-white/10 bg-black/30 p-4">
								<div>
									<div class="text-xs uppercase tracking-[0.2em] text-slate-200/70">Total wins</div>
									<div class="text-2xl font-semibold text-white">{stats.wins}</div>
								</div>
								<div>
									<div class="text-xs uppercase tracking-[0.2em] text-slate-200/70">Current streak</div>
									<div class="text-2xl font-semibold text-white">{stats.currentStreak}</div>
								</div>
								<div>
									<div class="text-xs uppercase tracking-[0.2em] text-slate-200/70">Longest streak</div>
									<div class="text-2xl font-semibold text-white">{stats.longestStreak}</div>
								</div>
								<div>
									<div class="text-xs uppercase tracking-[0.2em] text-slate-200/70">Practice runs</div>
									<div class="text-2xl font-semibold text-white">{stats.practiceAttempts}</div>
								</div>
								<div class="col-span-2">
									<div class="text-xs uppercase tracking-[0.2em] text-slate-200/70">Avg guesses</div>
									<div class="text-xl font-semibold text-white">
										{stats.averageGuessCount ? stats.averageGuessCount.toFixed(2) : '—'}
									</div>
								</div>
							</div>
						{/if}

						<div class="rounded-2xl border border-white/10 bg-white/5 p-4">
							<div class="flex items-center justify-between">
								<div>
									<div class="text-xs uppercase tracking-[0.2em] text-slate-200/70">Account</div>
									<div class="text-lg font-semibold text-white break-words">{$auth.user.email}</div>
								</div>
							</div>
							<div class="mt-3 text-sm text-slate-200/70">
								Member since {new Date($auth.user.createdOn).toLocaleDateString()}
							</div>
						</div>
					</div>
				</div>
			{/if}
		</section>

		<section class="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-xl">
			<h2 class="text-xl font-semibold text-white">Solutions after noon</h2>
			<p class="mt-2 text-sm text-slate-200/75">
				Once the clock hits 12:00 PM, everyone’s boards unlock here. Practice games after noon are labeled and
				kept out of streak calculations.
			</p>

			{#if !state?.cutoffPassed}
				<div class="mt-4 rounded-2xl border border-white/10 bg-black/30 p-4 text-sm text-slate-200/75">
					Solutions unlock at noon. Finish early to keep your streak alive.
				</div>
			{:else if !solutions}
				<div class="mt-4 rounded-2xl border border-white/10 bg-black/30 p-4 text-sm text-slate-200/75">
					Loading solutions...
				</div>
			{:else if solutions.entries.length === 0}
				<div class="mt-4 rounded-2xl border border-white/10 bg-black/30 p-4 text-sm text-slate-200/75">
					No attempts logged yet.
				</div>
			{:else}
				<div class="mt-4 rounded-2xl border border-emerald-300/30 bg-emerald-400/10 p-3 text-sm text-emerald-50">
					Solution: {solutions.solution}
				</div>
				<div class="mt-4 space-y-3">
					{#each solutions.entries as entry}
						<div class="rounded-2xl border border-white/10 bg-black/25 p-4">
							<div class="flex flex-wrap items-center justify-between gap-3">
								<div>
									<div class="text-base font-semibold text-white">{entry.displayName}</div>
									<div class="text-xs uppercase tracking-[0.2em] text-slate-200/60">
										{entry.isAfterReveal ? 'Practice run' : 'Official attempt'}
									</div>
								</div>
								<span class={`rounded-full border px-3 py-1 text-xs font-semibold ${statusBadge(entry.status)}`}>
									{statusCopy(entry.status)}
								</span>
							</div>
							<div class="mt-3 flex flex-wrap items-center gap-2 text-sm text-slate-200/80">
								<span class="rounded-full border border-white/10 bg-white/5 px-2 py-1">
									Guesses: {entry.guessCount ?? '—'}
								</span>
								{#if entry.completedOn}
									<span class="rounded-full border border-white/10 bg-white/5 px-2 py-1">
										Completed {new Date(entry.completedOn).toLocaleTimeString()}
									</span>
								{/if}
							</div>
							{#if entry.guesses.length > 0}
								<div class="mt-3 flex flex-wrap gap-2 text-xs uppercase tracking-[0.2em] text-white/80">
									{#each entry.guesses as g}
										<span class="rounded-full border border-white/10 bg-white/5 px-3 py-1">{g}</span>
									{/each}
								</div>
							{/if}
						</div>
					{/each}
				</div>
			{/if}
		</section>
	</div>
{/if}
