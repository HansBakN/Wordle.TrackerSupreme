<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { StatsService } from '$lib/api-client/services/StatsService';
	import type { PlayerStatsEntryResponse } from '$lib/api-client/models/PlayerStatsEntryResponse';
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

	onMount(() => {
		if ($auth.user) {
			hasLoaded = true;
			void loadStats();
		}
	});

	$effect(() => {
		if ($auth.user && !hasLoaded) {
			hasLoaded = true;
			void loadStats();
		}

		if (!$auth.user && hasLoaded) {
			hasLoaded = false;
			entries = [];
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
							</div>
						{/each}
					</div>
				{/if}
			</div>
		</section>
	</div>
{/if}
