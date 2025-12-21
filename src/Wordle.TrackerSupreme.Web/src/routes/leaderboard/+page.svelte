<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { StatsService } from '$lib/api-client/services/StatsService';
	import type { LeaderboardEntryResponse } from '$lib/api-client/models/LeaderboardEntryResponse';

	let loading = $state(false);
	let error = $state<string | null>(null);
	let entries = $state<LeaderboardEntryResponse[]>([]);

	function formatAverage(value: number | null | undefined) {
		if (value === null || value === undefined) {
			return '—';
		}
		return value.toFixed(2);
	}

	function formatWinRate(value: number | null | undefined) {
		if (value === null || value === undefined) {
			return '—';
		}
		return `${Math.round(value * 100)}%`;
	}

	async function loadLeaderboard() {
		loading = true;
		error = null;
		try {
			entries = await StatsService.getApiStatsLeaderboard();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load leaderboard.';
		} finally {
			loading = false;
		}
	}

	onMount(() => {
		void loadLeaderboard();
	});
</script>

{#if !$auth.user}
	<div
		class="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-slate-200/80 shadow-xl"
	>
		<p class="text-lg font-semibold">Sign in to view the leaderboard.</p>
		<p class="mt-2 text-sm text-slate-200/70">
			The hard-mode leaderboard only counts completions before 12 PM local time.
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
		<section
			class="rounded-3xl border border-white/10 bg-gradient-to-br from-white/10 to-white/5 p-8 shadow-2xl"
		>
			<p class="text-sm tracking-[0.2em] text-emerald-200/80 uppercase">Leaderboard</p>
			<h1 class="mt-3 text-3xl font-semibold text-white">Hard mode before noon</h1>
			<p class="mt-2 max-w-2xl text-sm text-slate-200/80">
				Ranked by win rate, then average guesses, across hard-mode attempts submitted before the
				daily reveal.
			</p>
		</section>

		{#if error}
			<div
				class="rounded-2xl border border-rose-400/40 bg-rose-500/10 px-5 py-4 text-sm text-rose-50"
			>
				{error}
			</div>
		{/if}

		{#if loading}
			<div class="rounded-2xl border border-white/10 bg-white/5 p-6 text-sm text-slate-200/70">
				Loading leaderboard...
			</div>
		{:else if entries.length === 0}
			<div class="rounded-2xl border border-white/10 bg-white/5 p-6 text-sm text-slate-200/70">
				No leaderboard entries yet. Solve today’s puzzle to claim the top spot.
			</div>
		{:else}
			<div class="overflow-hidden rounded-3xl border border-white/10 bg-black/30 shadow-xl">
				<table class="w-full text-left text-sm text-slate-200/80" data-testid="leaderboard-table">
					<thead class="bg-white/5 text-xs tracking-[0.2em] text-slate-200/70 uppercase">
						<tr>
							<th class="px-6 py-4">Rank</th>
							<th class="px-6 py-4">Player</th>
							<th class="px-6 py-4">Win rate</th>
							<th class="px-6 py-4">Avg guesses</th>
							<th class="px-6 py-4">Wins</th>
							<th class="px-6 py-4">Attempts</th>
							<th class="px-6 py-4">Current streak</th>
						</tr>
					</thead>
					<tbody>
						{#each entries as entry (entry.playerId)}
							<tr class="border-t border-white/5" data-testid="leaderboard-row">
								<td class="px-6 py-4 text-base font-semibold text-white">{entry.rank}</td>
								<td class="px-6 py-4">
									<div class="text-base font-semibold text-white">{entry.displayName}</div>
									<div class="text-xs tracking-[0.2em] text-slate-200/60 uppercase">
										Longest streak {entry.longestStreak}
									</div>
								</td>
								<td class="px-6 py-4">{formatWinRate(entry.winRate)}</td>
								<td class="px-6 py-4">{formatAverage(entry.averageGuessCount)}</td>
								<td class="px-6 py-4">{entry.wins}</td>
								<td class="px-6 py-4">{entry.totalAttempts}</td>
								<td class="px-6 py-4">{entry.currentStreak}</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{/if}
	</div>
{/if}
