<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/auth/store';
	import type { CalendarDayResponse } from '$lib/api-client/models/CalendarDayResponse';
	import { fetchMyCalendar } from '$lib/game/api';

	let days = $state<CalendarDayResponse[]>([]);
	let loading = $state(false);
	let error = $state<string | null>(null);

	function outcomeColor(day: CalendarDayResponse): string {
		if (day.outcome === 'none') return 'bg-white/5';
		if (day.outcome === 'in_progress') return 'bg-amber-400/60';
		if (day.outcome === 'failed') return 'bg-rose-500/70';
		if (day.isAfterReveal) return 'bg-sky-400/60';
		return 'bg-emerald-500/70';
	}

	function outcomeLabel(day: CalendarDayResponse): string {
		if (day.outcome === 'none') return 'No attempt';
		if (day.outcome === 'in_progress') return 'In progress';
		if (day.outcome === 'failed') return 'Failed';
		const timing = day.isAfterReveal ? ' (practice)' : '';
		return `Won in ${day.guessCount}${timing}`;
	}

	async function load() {
		if (!$auth.user) return;
		loading = true;
		error = null;
		try {
			const res = await fetchMyCalendar(90);
			days = res.days;
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load calendar.';
		} finally {
			loading = false;
		}
	}

	onMount(() => {
		if ($auth.user) void load();
	});

	$effect(() => {
		if ($auth.user && days.length === 0 && !loading) void load();
	});

	const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

	function getWeeks(daysList: CalendarDayResponse[]) {
		if (daysList.length === 0) return [];
		const firstDate = new Date(daysList[0].date + 'T00:00:00');
		const startDow = (firstDate.getDay() + 6) % 7;
		const padded: (CalendarDayResponse | null)[] = Array(startDow).fill(null);
		padded.push(...daysList);
		const weeks: (CalendarDayResponse | null)[][] = [];
		for (let i = 0; i < padded.length; i += 7) {
			weeks.push(padded.slice(i, i + 7));
		}
		return weeks;
	}
</script>

<section
	class="rounded-3xl border border-white/10 bg-white/5 p-6 shadow-xl"
	data-testid="streak-calendar"
>
	<h2 class="text-sm font-semibold tracking-[0.2em] text-slate-200/70 uppercase">
		Streak calendar
	</h2>
	<p class="mt-1 text-xs text-slate-200/50">Last 90 days</p>

	{#if loading}
		<div class="mt-4 text-sm text-slate-200/70">Loading calendar...</div>
	{:else if error}
		<div class="mt-4 text-sm text-rose-300">{error}</div>
	{:else if days.length > 0}
		<div class="mt-4 overflow-x-auto">
			<div class="inline-grid gap-[2px]" style="grid-template-columns: auto repeat(7, 1fr)">
				<div></div>
				{#each weekdays as wd (wd)}
					<div class="px-1 text-center text-[10px] text-slate-200/50">{wd}</div>
				{/each}

				{#each getWeeks(days) as week, wi (wi)}
					<div class="flex items-center pr-1 text-[10px] text-slate-200/40">
						{#if wi === 0 || wi === Math.floor(getWeeks(days).length / 2) || wi === getWeeks(days).length - 1}
							{@const firstDay = week.find((d) => d !== null)}
							{#if firstDay}
								{new Date(firstDay.date + 'T00:00:00').toLocaleDateString('en', {
									month: 'short',
									day: 'numeric'
								})}
							{/if}
						{/if}
					</div>
					{#each week as day, di (day?.date ?? `empty-${wi}-${di}`)}
						{#if day === null}
							<div class="h-4 w-4"></div>
						{:else}
							<div
								class="h-4 w-4 rounded-sm {outcomeColor(day)}"
								title="{day.date}: {outcomeLabel(day)}"
								data-testid="calendar-day"
								data-date={day.date}
								data-outcome={day.outcome}
							></div>
						{/if}
					{/each}
				{/each}
			</div>
		</div>

		<div class="mt-4 flex flex-wrap items-center gap-3 text-[10px] text-slate-200/60">
			<span class="flex items-center gap-1">
				<span class="inline-block h-3 w-3 rounded-sm bg-emerald-500/70"></span> Won
			</span>
			<span class="flex items-center gap-1">
				<span class="inline-block h-3 w-3 rounded-sm bg-sky-400/60"></span> Won (practice)
			</span>
			<span class="flex items-center gap-1">
				<span class="inline-block h-3 w-3 rounded-sm bg-rose-500/70"></span> Failed
			</span>
			<span class="flex items-center gap-1">
				<span class="inline-block h-3 w-3 rounded-sm bg-amber-400/60"></span> In progress
			</span>
			<span class="flex items-center gap-1">
				<span class="inline-block h-3 w-3 rounded-sm bg-white/5"></span> No attempt
			</span>
		</div>
	{:else}
		<div class="mt-4 text-sm text-slate-200/70">No data available.</div>
	{/if}
</section>
