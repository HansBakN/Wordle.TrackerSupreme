<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { StatsService } from '$lib/api-client/services/StatsService';
	import type { PlayerStatsEntryResponse } from '$lib/api-client/models/PlayerStatsEntryResponse';
	import type { PuzzleHistoryEntryResponse } from '$lib/api-client/models/PuzzleHistoryEntryResponse';
	import {
		buildStatsFilterRequest,
		defaultStatsFilterState,
		type StatsFilterState
	} from '$lib/stats/filters';

	let loading = $state(false);
	let error = $state<string | null>(null);
	let entries = $state<PlayerStatsEntryResponse[]>([]);
	let hasLoaded = $state(false);
	let filters = $state<StatsFilterState>({ ...defaultStatsFilterState });

	let historyLoading = $state(false);
	let historyError = $state<string | null>(null);
	let history = $state<PuzzleHistoryEntryResponse[]>([]);
	let historyExpanded = $state<Record<string, boolean>>({});

	function formatAverage(value: number | null | undefined) {
		if (value === null || value === undefined) {
			return '—';
		}
		return value.toFixed(2);
	}

	function formatWinRate(total: number, wins: number) {
		if (!total) {
			return '—';
		}
		return `${Math.round((wins / total) * 100)}%`;
	}

	async function loadStats() {
		if (!$auth.user) {
			return;
		}
		loading = true;
		error = null;
		try {
			const request = buildStatsFilterRequest(filters);
			const data = await StatsService.postApiStatsPlayers({ requestBody: request });
			entries = data.filter((entry) => entry.playerId !== $auth.user?.id);
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load player stats.';
		} finally {
			loading = false;
		}
	}

	function resetFilters() {
		filters = { ...defaultStatsFilterState };
		void loadStats();
	}

	async function loadHistory() {
		if (!$auth.user) return;
		historyLoading = true;
		historyError = null;
		try {
			history = await StatsService.getApiStatsMeHistory();
		} catch (err) {
			historyError = err instanceof Error ? err.message : 'Unable to load puzzle history.';
		} finally {
			historyLoading = false;
		}
	}

	function tileColor(result: string | undefined) {
		if (result === 'Correct') return 'bg-emerald-500';
		if (result === 'Present') return 'bg-amber-400';
		return 'bg-slate-600';
	}

	function toggleHistory(attemptId: string) {
		historyExpanded = { ...historyExpanded, [attemptId]: !historyExpanded[attemptId] };
	}

	onMount(() => {
		if ($auth.user) {
			hasLoaded = true;
			void loadStats();
			void loadHistory();
		}
	});

	$effect(() => {
		if ($auth.user && !hasLoaded) {
			hasLoaded = true;
			void loadStats();
			void loadHistory();
		}

		if (!$auth.user && hasLoaded) {
			hasLoaded = false;
			entries = [];
			history = [];
		}
	});
</script>

{#if !$auth.user}
	<div
		class="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-slate-200/80 shadow-xl"
	>
		<p class="text-lg font-semibold">Sign in to compare stats.</p>
		<p class="mt-2 text-sm text-slate-200/70">
			Track how the rest of the league is performing and filter by any combination of attempts.
		</p>
		<a
			href={resolve('/signin')}
			class="mt-6 inline-flex rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-900 transition hover:bg-emerald-300"
		>
			Sign in
		</a>
	</div>
{:else}
	<div class="space-y-6">
		<section class="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-2xl">
			<div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
				<div>
					<p class="text-sm tracking-[0.2em] text-emerald-200/80 uppercase">Community stats</p>
					<h1 class="mt-3 text-3xl font-semibold text-white">Every player, every lens</h1>
					<p class="mt-2 max-w-2xl text-sm text-slate-200/80">
						Default filters track hard mode completions before noon. Toggle any combination to
						explore practice runs, easy mode, or specific date windows.
					</p>
				</div>
				<div class="flex flex-wrap items-center gap-3">
					<button
						class="rounded-full border border-white/20 bg-white/10 px-4 py-2 text-xs font-semibold tracking-[0.2em] text-white/80 uppercase transition hover:border-white/40 hover:bg-white/15"
						on:click={loadStats}
						disabled={loading}
						data-testid="stats-apply"
					>
						{loading ? 'Loading...' : 'Apply filters'}
					</button>
					<button
						class="rounded-full border border-white/20 px-4 py-2 text-xs font-semibold tracking-[0.2em] text-white/70 uppercase transition hover:border-white/40 hover:text-white"
						on:click={resetFilters}
						disabled={loading}
						data-testid="stats-reset"
					>
						Reset defaults
					</button>
				</div>
			</div>
		</section>

		<section
			class="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-2xl"
			data-testid="puzzle-history-section"
		>
			<p class="text-sm tracking-[0.2em] text-emerald-200/80 uppercase">Puzzle history</p>
			<h2 class="mt-3 text-2xl font-semibold text-white">Your past attempts</h2>

			{#if historyError}
				<div
					class="mt-4 rounded-2xl border border-rose-400/40 bg-rose-500/10 px-5 py-4 text-sm text-rose-50"
					data-testid="history-error"
				>
					{historyError}
				</div>
			{:else if historyLoading}
				<div class="mt-4 text-sm text-slate-200/70" data-testid="history-loading">
					Loading puzzle history...
				</div>
			{:else if history.length === 0}
				<div class="mt-4 text-sm text-slate-200/70" data-testid="history-empty">
					No puzzle history yet. Play today's puzzle to get started!
				</div>
			{:else}
				<div class="mt-4 space-y-3" data-testid="history-list">
					{#each history as entry (entry.puzzleDate)}
						{@const expanded = historyExpanded[entry.puzzleDate ?? ''] ?? false}
						<div
							class="overflow-hidden rounded-2xl border border-white/10 bg-black/20"
							data-testid="history-entry"
						>
							<button
								class="flex w-full items-center justify-between gap-4 px-5 py-4 text-left transition hover:bg-white/5"
								on:click={() => toggleHistory(entry.puzzleDate ?? '')}
								aria-expanded={expanded}
								data-testid="history-toggle"
							>
								<div class="flex items-center gap-4">
									<span class="text-sm font-semibold text-white" data-testid="history-date">
										{entry.puzzleDate}
									</span>
									{#if entry.status === 'Solved'}
										<span
											class="rounded-full bg-emerald-500/20 px-2 py-0.5 text-xs font-semibold text-emerald-300"
											data-testid="history-status"
										>
											Solved in {entry.guessCount}
										</span>
									{:else if entry.status === 'Failed'}
										<span
											class="rounded-full bg-rose-500/20 px-2 py-0.5 text-xs font-semibold text-rose-300"
											data-testid="history-status"
										>
											Failed
										</span>
									{:else}
										<span
											class="rounded-full bg-slate-500/20 px-2 py-0.5 text-xs font-semibold text-slate-300"
											data-testid="history-status"
										>
											In progress
										</span>
									{/if}
									{#if entry.playedInHardMode}
										<span
											class="rounded-full bg-amber-500/20 px-2 py-0.5 text-xs font-semibold text-amber-300"
											data-testid="history-hard-mode"
										>
											Hard
										</span>
									{/if}
									{#if entry.isAfterReveal}
										<span
											class="rounded-full bg-slate-500/20 px-2 py-0.5 text-xs font-semibold text-slate-400"
											data-testid="history-practice"
										>
											Practice
										</span>
									{/if}
									{#if entry.solution}
										<span
											class="font-mono text-xs tracking-widest text-slate-300/70 uppercase"
											data-testid="history-solution"
										>
											{entry.solution}
										</span>
									{/if}
								</div>
								<span class="text-xs text-slate-400 transition" class:rotate-180={expanded}>▲</span>
							</button>

							{#if expanded && entry.guesses && entry.guesses.length > 0}
								<div class="border-t border-white/10 px-5 py-4" data-testid="history-guesses">
									<div class="space-y-1.5">
										{#each entry.guesses as guess (guess.guessNumber)}
											<div class="flex gap-1.5" data-testid="history-guess-row">
												{#each guess.feedback ?? [] as fb (fb.position)}
													<div
														class="flex h-9 w-9 items-center justify-center rounded text-sm font-bold text-white {tileColor(
															fb.result
														)}"
														data-testid="history-tile"
													>
														{fb.letter?.toUpperCase() ?? ''}
													</div>
												{/each}
											</div>
										{/each}
									</div>
								</div>
							{/if}
						</div>
					{/each}
				</div>
			{/if}
		</section>

		<section class="grid gap-6 lg:grid-cols-[minmax(0,_1fr)_minmax(0,_2fr)]">
			<div class="rounded-3xl border border-white/10 bg-black/30 p-6 shadow-xl">
				<h2 class="text-sm font-semibold tracking-[0.2em] text-slate-200/70 uppercase">Filters</h2>
				<div class="mt-4 space-y-5 text-sm text-slate-100/80">
					<div class="space-y-3">
						<p class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">Mode</p>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.includeHardMode}
								data-testid="filter-hard-mode"
							/>
							Hard mode attempts
						</label>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.includeEasyMode}
								data-testid="filter-easy-mode"
							/>
							Easy mode attempts
						</label>
					</div>

					<div class="space-y-3">
						<p class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">Timing</p>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.includeBeforeReveal}
								data-testid="filter-before-reveal"
							/>
							Before 12 PM reveal
						</label>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.includeAfterReveal}
								data-testid="filter-after-reveal"
							/>
							After 12 PM practice
						</label>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.countPracticeAttempts}
								data-testid="filter-count-practice"
							/>
							Count practice in totals
						</label>
					</div>

					<div class="space-y-3">
						<p class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">Status</p>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.includeSolved}
								data-testid="filter-solved"
							/>
							Solved
						</label>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.includeFailed}
								data-testid="filter-failed"
							/>
							Failed
						</label>
						<label class="flex items-center gap-2">
							<input
								type="checkbox"
								bind:checked={filters.includeInProgress}
								data-testid="filter-in-progress"
							/>
							In progress
						</label>
					</div>

					<div class="space-y-3">
						<p class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">Date range</p>
						<label class="grid gap-2">
							<span class="text-xs text-slate-200/70">From</span>
							<input
								type="date"
								class="rounded-lg border border-white/10 bg-white/5 px-3 py-2 text-sm text-white"
								bind:value={filters.fromDate}
								data-testid="filter-from-date"
							/>
						</label>
						<label class="grid gap-2">
							<span class="text-xs text-slate-200/70">To</span>
							<input
								type="date"
								class="rounded-lg border border-white/10 bg-white/5 px-3 py-2 text-sm text-white"
								bind:value={filters.toDate}
								data-testid="filter-to-date"
							/>
						</label>
					</div>

					<div class="space-y-3">
						<p class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">Guess range</p>
						<label class="grid gap-2">
							<span class="text-xs text-slate-200/70">Min guesses</span>
							<input
								type="number"
								min="1"
								class="rounded-lg border border-white/10 bg-white/5 px-3 py-2 text-sm text-white"
								bind:value={filters.minGuessCount}
								data-testid="filter-min-guesses"
							/>
						</label>
						<label class="grid gap-2">
							<span class="text-xs text-slate-200/70">Max guesses</span>
							<input
								type="number"
								min="1"
								class="rounded-lg border border-white/10 bg-white/5 px-3 py-2 text-sm text-white"
								bind:value={filters.maxGuessCount}
								data-testid="filter-max-guesses"
							/>
						</label>
					</div>
				</div>
			</div>

			<div class="space-y-4">
				{#if error}
					<div
						class="rounded-2xl border border-rose-400/40 bg-rose-500/10 px-5 py-4 text-sm text-rose-50"
					>
						{error}
					</div>
				{/if}

				{#if loading}
					<div class="rounded-2xl border border-white/10 bg-white/5 p-6 text-sm text-slate-200/70">
						Loading filtered stats...
					</div>
				{:else if entries.length === 0}
					<div class="rounded-2xl border border-white/10 bg-white/5 p-6 text-sm text-slate-200/70">
						No players match the current filters.
					</div>
				{:else}
					<div class="grid gap-4" data-testid="stats-results">
						{#each entries as entry (entry.playerId)}
							<div
								class="rounded-2xl border border-white/10 bg-white/5 p-6 shadow-xl"
								data-testid="stats-card"
							>
								<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
									<div>
										<div class="text-lg font-semibold text-white">{entry.displayName}</div>
										<div class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">
											Win rate {formatWinRate(entry.stats.totalAttempts, entry.stats.wins)}
										</div>
									</div>
									<div class="text-sm text-slate-200/70">
										Average guesses: {formatAverage(entry.stats.averageGuessCount)}
									</div>
								</div>
								<div class="mt-4 grid gap-3 text-sm text-slate-200/80 sm:grid-cols-3">
									<div>
										<span class="text-xs tracking-[0.2em] text-slate-200/60 uppercase"
											>Attempts</span
										>
										<div class="text-lg font-semibold text-white">{entry.stats.totalAttempts}</div>
									</div>
									<div>
										<span class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">Wins</span>
										<div class="text-lg font-semibold text-white">{entry.stats.wins}</div>
									</div>
									<div>
										<span class="text-xs tracking-[0.2em] text-slate-200/60 uppercase"
											>Failures</span
										>
										<div class="text-lg font-semibold text-white">{entry.stats.failures}</div>
									</div>
									<div>
										<span class="text-xs tracking-[0.2em] text-slate-200/60 uppercase"
											>Practice</span
										>
										<div class="text-lg font-semibold text-white">
											{entry.stats.practiceAttempts}
										</div>
									</div>
									<div>
										<span class="text-xs tracking-[0.2em] text-slate-200/60 uppercase"
											>Current streak</span
										>
										<div class="text-lg font-semibold text-white">{entry.stats.currentStreak}</div>
									</div>
									<div>
										<span class="text-xs tracking-[0.2em] text-slate-200/60 uppercase"
											>Longest streak</span
										>
										<div class="text-lg font-semibold text-white">{entry.stats.longestStreak}</div>
									</div>
								</div>
								{#if entry.stats.guessDistribution && Object.keys(entry.stats.guessDistribution).length > 0}
									{@const dist = entry.stats.guessDistribution}
									{@const maxCount = Math.max(...Object.values(dist))}
									<div class="mt-4" data-testid="guess-distribution">
										<p class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">
											Guess distribution
										</p>
										<div class="mt-2 space-y-1">
											{#each [1, 2, 3, 4, 5, 6] as n (n)}
												{@const count = dist[n] ?? 0}
												<div class="flex items-center gap-2 text-xs">
													<span class="w-3 shrink-0 text-right text-slate-200/70">{n}</span>
													<div class="flex-1">
														<div
															class="h-5 rounded-sm bg-emerald-500/70 transition-all"
															style="width: {maxCount > 0
																? Math.max((count / maxCount) * 100, count > 0 ? 4 : 0)
																: 0}%"
														></div>
													</div>
													<span class="w-5 text-right text-slate-200/80">{count}</span>
												</div>
											{/each}
										</div>
									</div>
								{/if}
							</div>
						{/each}
					</div>
				{/if}
			</div>
		</section>
	</div>
{/if}
