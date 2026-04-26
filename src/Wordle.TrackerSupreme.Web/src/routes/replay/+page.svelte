<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { onMount } from 'svelte';

	let checking = $state(true);
	let selectedDate = $state('');
	let error = $state<string | null>(null);
	const today = new Date().toISOString().slice(0, 10);

	onMount(() => {
		const unsubscribe = auth.subscribe((current) => {
			if (!current.ready) {
				return;
			}
			checking = false;
			if (!current.user) {
				goto(resolve('/signin'));
			}
		});
		return () => unsubscribe();
	});

	function isValidDate(value: string): boolean {
		if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
		const parsed = new Date(value);
		return !Number.isNaN(parsed.getTime()) && value <= today;
	}

	function handleSubmit(event: Event) {
		event.preventDefault();
		error = null;
		if (!selectedDate) {
			error = 'Pick a date to replay.';
			return;
		}
		if (!isValidDate(selectedDate)) {
			error = 'Pick a date that is today or earlier.';
			return;
		}
		goto(resolve(`/replay/${selectedDate}`));
	}
</script>

{#if checking}
	<div
		class="rounded-2xl border border-white/10 bg-white/5 p-10 text-center text-slate-200/80 shadow-xl"
	>
		Checking your session...
	</div>
{:else if $auth.user}
	<section class="mx-auto max-w-xl space-y-6">
		<header class="space-y-2">
			<p class="text-sm tracking-[0.2em] text-cyan-200/80 uppercase">Replay</p>
			<h1 class="text-3xl font-semibold text-white">Play a past puzzle</h1>
			<p class="text-sm text-slate-200/80">
				Pick a date to revisit any historical Wordle Tracker Supreme puzzle. Replays are tracked as
				practice and won't change your streak or leaderboard rank.
			</p>
		</header>
		<form
			class="space-y-4 rounded-3xl border border-white/10 bg-white/5 p-6 shadow-xl"
			onsubmit={handleSubmit}
			data-testid="replay-form"
		>
			<label class="block space-y-2 text-sm text-slate-200/80">
				<span class="text-xs font-semibold tracking-[0.2em] text-slate-200/60 uppercase">
					Puzzle date
				</span>
				<input
					type="date"
					bind:value={selectedDate}
					max={today}
					required
					class="block w-full rounded-xl border border-white/10 bg-black/30 px-4 py-3 text-base text-white transition outline-none focus:border-cyan-300/60"
					data-testid="replay-date"
				/>
			</label>
			{#if error}
				<p class="text-sm text-rose-200" data-testid="replay-error">{error}</p>
			{/if}
			<button
				type="submit"
				class="w-full rounded-full bg-emerald-400 px-4 py-3 font-semibold text-slate-900 transition hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-50"
				disabled={!selectedDate}
				data-testid="replay-submit"
			>
				Play this puzzle
			</button>
		</form>
		<p class="text-center text-xs text-slate-200/60">
			Looking for today's puzzle?
			<a
				href={resolve('/')}
				class="ml-1 underline decoration-slate-300/40 underline-offset-4 transition hover:text-white"
			>
				Play today
			</a>
		</p>
	</section>
{/if}
