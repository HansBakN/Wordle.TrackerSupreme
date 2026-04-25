<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { StatsService } from '$lib/api-client/services/StatsService';
	import type { LeaderboardEntryResponse } from '$lib/api-client/models/LeaderboardEntryResponse';
	import type { TodayLeaderboardEntryResponse } from '$lib/api-client/models/TodayLeaderboardEntryResponse';
	import { formatTodayLeaderboardMeta } from '$lib/stats/leaderboard';

	const PAGE_SIZE = 10;

	type LeaderboardTab = 'all-time' | 'today';

	type LeaderboardTabContent = {
		title: string;
		description: string;
		emptyState: string;
	};

	const tabContent: Record<LeaderboardTab, LeaderboardTabContent> = {
		'all-time': {
			title: 'Hard mode before noon',
			description:
				'Ranked by win rate, then average guesses, across hard-mode attempts submitted before the daily reveal.',
			emptyState: "No leaderboard entries yet. Solve today's puzzle to claim the top spot."
		},
		today: {
			title: "Today's puzzle",
			description:
				"See who has finished today's puzzle, how many guesses it took, and who is still playing right now.",
			emptyState: "No one has submitted an attempt for today's puzzle yet."
		}
	};

	let loading = $state(false);
	let error = $state<string | null>(null);
	let allTimeEntries = $state<LeaderboardEntryResponse[]>([]);
	let allTimePage = $state(1);
	let allTimeTotalPages = $state(1);
	let allTimeTotal = $state(0);
	let todayEntries = $state<TodayLeaderboardEntryResponse[]>([]);
	let hasLoaded = $state(false);
	let activeTab = $state<LeaderboardTab>('all-time');
	let loadedTabs = $state<Record<LeaderboardTab, boolean>>({
		'all-time': false,
		today: false
	});
	let allTimeTabButton = $state<HTMLButtonElement | null>(null);
	let todayTabButton = $state<HTMLButtonElement | null>(null);

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

	function getTabContent(tab: LeaderboardTab): LeaderboardTabContent {
		return tabContent[tab];
	}

	function getTodayResultClasses(result: string | null | undefined) {
		if (result === 'Solved') {
			return 'bg-emerald-400/15 text-emerald-200 ring-1 ring-emerald-300/20';
		}
		if (result === 'Failed') {
			return 'bg-rose-500/15 text-rose-100 ring-1 ring-rose-300/20';
		}
		return 'bg-amber-400/15 text-amber-100 ring-1 ring-amber-300/20';
	}

	async function loadAllTime(page: number) {
		if (!$auth.user) return;
		loading = true;
		error = null;
		try {
			const data = await StatsService.getApiStatsLeaderboard({ page, pageSize: PAGE_SIZE });
			allTimeEntries = data.items ?? [];
			allTimePage = data.page ?? 1;
			allTimeTotalPages = data.totalPages ?? 1;
			allTimeTotal = data.total ?? 0;
			loadedTabs['all-time'] = true;
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load leaderboard.';
		} finally {
			loading = false;
		}
	}

	async function loadLeaderboard(tab: LeaderboardTab) {
		if (!$auth.user) {
			return;
		}
		loading = true;
		error = null;
		try {
			if (tab === 'all-time') {
				await loadAllTime(1);
				return;
			} else {
				todayEntries = await StatsService.getApiStatsLeaderboardToday();
			}
			loadedTabs[tab] = true;
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load leaderboard.';
		} finally {
			loading = false;
		}
	}

	async function goToPage(page: number) {
		await loadAllTime(page);
	}

	async function selectTab(tab: LeaderboardTab) {
		activeTab = tab;
		if (!$auth.user || loadedTabs[tab]) {
			return;
		}
		await loadLeaderboard(tab);
	}

	function focusTab(tab: LeaderboardTab) {
		if (tab === 'all-time') {
			allTimeTabButton?.focus();
		} else {
			todayTabButton?.focus();
		}
	}

	async function handleTabKeydown(evt: KeyboardEvent, tab: LeaderboardTab) {
		if (
			evt.key !== 'ArrowLeft' &&
			evt.key !== 'ArrowRight' &&
			evt.key !== 'Home' &&
			evt.key !== 'End'
		) {
			return;
		}

		evt.preventDefault();

		const tabs: LeaderboardTab[] = ['all-time', 'today'];
		const currentIndex = tabs.indexOf(tab);
		let nextTab = tab;

		if (evt.key === 'ArrowLeft') {
			nextTab = tabs[(currentIndex - 1 + tabs.length) % tabs.length];
		} else if (evt.key === 'ArrowRight') {
			nextTab = tabs[(currentIndex + 1) % tabs.length];
		} else if (evt.key === 'Home') {
			nextTab = tabs[0];
		} else if (evt.key === 'End') {
			nextTab = tabs[tabs.length - 1];
		}

		await selectTab(nextTab);
		focusTab(nextTab);
	}

	onMount(() => {
		if ($auth.user) {
			hasLoaded = true;
			void loadLeaderboard(activeTab);
		}
	});

	$effect(() => {
		if ($auth.user && !hasLoaded) {
			hasLoaded = true;
			void loadLeaderboard(activeTab);
		}

		if (!$auth.user && hasLoaded) {
			hasLoaded = false;
			allTimeEntries = [];
			todayEntries = [];
			activeTab = 'all-time';
			loadedTabs = {
				'all-time': false,
				today: false
			};
		}
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
			<div
				class="mt-5 inline-flex rounded-full border border-white/10 bg-black/25 p-1"
				role="tablist"
			>
				<button
					type="button"
					role="tab"
					bind:this={allTimeTabButton}
					class={`rounded-full px-4 py-2 text-sm font-semibold transition ${
						activeTab === 'all-time'
							? 'bg-emerald-400 text-slate-950'
							: 'text-slate-200/70 hover:text-white'
					}`}
					aria-selected={activeTab === 'all-time'}
					tabindex={activeTab === 'all-time' ? 0 : -1}
					onclick={() => void selectTab('all-time')}
					onkeydown={(evt) => void handleTabKeydown(evt, 'all-time')}
				>
					All-time
				</button>
				<button
					type="button"
					role="tab"
					bind:this={todayTabButton}
					class={`rounded-full px-4 py-2 text-sm font-semibold transition ${
						activeTab === 'today'
							? 'bg-emerald-400 text-slate-950'
							: 'text-slate-200/70 hover:text-white'
					}`}
					aria-selected={activeTab === 'today'}
					tabindex={activeTab === 'today' ? 0 : -1}
					onclick={() => void selectTab('today')}
					onkeydown={(evt) => void handleTabKeydown(evt, 'today')}
				>
					Today's puzzle
				</button>
			</div>
			<h1 class="mt-4 text-3xl font-semibold text-white">{getTabContent(activeTab).title}</h1>
			<p class="mt-2 max-w-2xl text-sm text-slate-200/80">{getTabContent(activeTab).description}</p>
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
		{:else if activeTab === 'all-time' && allTimeEntries.length === 0}
			<div class="rounded-2xl border border-white/10 bg-white/5 p-6 text-sm text-slate-200/70">
				{getTabContent(activeTab).emptyState}
			</div>
		{:else if activeTab === 'today' && todayEntries.length === 0}
			<div class="rounded-2xl border border-white/10 bg-white/5 p-6 text-sm text-slate-200/70">
				{getTabContent(activeTab).emptyState}
			</div>
		{:else if activeTab === 'all-time'}
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
						{#each allTimeEntries as entry (entry.playerId)}
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
			{#if allTimeTotalPages > 1}
				<div
					class="mt-4 flex items-center justify-between text-sm text-slate-200/70"
					data-testid="leaderboard-pagination"
				>
					<span>{allTimeTotal} players · page {allTimePage} of {allTimeTotalPages}</span>
					<div class="flex gap-2">
						<button
							class="rounded-full border border-white/20 bg-white/10 px-4 py-2 text-xs font-semibold text-white/80 transition hover:bg-white/15 disabled:cursor-not-allowed disabled:opacity-40"
							disabled={allTimePage <= 1 || loading}
							data-testid="leaderboard-prev"
							onclick={() => void goToPage(allTimePage - 1)}
						>
							← Previous
						</button>
						<button
							class="rounded-full border border-white/20 bg-white/10 px-4 py-2 text-xs font-semibold text-white/80 transition hover:bg-white/15 disabled:cursor-not-allowed disabled:opacity-40"
							disabled={allTimePage >= allTimeTotalPages || loading}
							data-testid="leaderboard-next"
							onclick={() => void goToPage(allTimePage + 1)}
						>
							Next →
						</button>
					</div>
				</div>
			{/if}
		{:else}
			<div class="overflow-hidden rounded-3xl border border-white/10 bg-black/30 shadow-xl">
				<table class="w-full text-left text-sm text-slate-200/80" data-testid="leaderboard-table">
					<thead class="bg-white/5 text-xs tracking-[0.2em] text-slate-200/70 uppercase">
						<tr>
							<th class="px-6 py-4">Rank</th>
							<th class="px-6 py-4">Player</th>
							<th class="px-6 py-4">Result</th>
							<th class="px-6 py-4">Details</th>
						</tr>
					</thead>
					<tbody>
						{#each todayEntries as entry (entry.playerId)}
							<tr class="border-t border-white/5" data-testid="leaderboard-row">
								<td class="px-6 py-4 text-base font-semibold text-white">{entry.rank}</td>
								<td class="px-6 py-4">
									<div class="text-base font-semibold text-white">{entry.displayName}</div>
								</td>
								<td class="px-6 py-4">
									<span
										class={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${getTodayResultClasses(entry.result)}`}
									>
										{entry.result}
									</span>
								</td>
								<td class="px-6 py-4 text-slate-100">
									{formatTodayLeaderboardMeta(entry)}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{/if}
	</div>
{/if}
