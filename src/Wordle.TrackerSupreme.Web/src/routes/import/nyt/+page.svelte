<script lang="ts">
	import { onDestroy } from 'svelte';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { NytImportService } from '$lib/api-client/services/NytImportService';
	import type { NytImportSessionResponse } from '$lib/api-client/models/NytImportSessionResponse';
	import type { NytImportSessionStatusResponse } from '$lib/api-client/models/NytImportSessionStatusResponse';
	import { formatCountdown, hasUnavailableHistory, secondsUntilExpiry } from '$lib/import/nyt';
	import { getApiErrorMessage } from '$lib/api/errors';

	let session = $state<NytImportSessionResponse | null>(null);
	let summary = $state<NytImportSessionStatusResponse | null>(null);
	let creating = $state(false);
	let error = $state<string | null>(null);
	let now = $state(Date.now());
	let clock: ReturnType<typeof setInterval> | null = null;
	let poller: ReturnType<typeof setInterval> | null = null;

	let remaining = $derived(session ? secondsUntilExpiry(session.expiresAt, now) : 0);

	async function createSession() {
		creating = true;
		error = null;
		summary = null;
		try {
			session = await NytImportService.postApiImportNytSession();
			now = Date.now();
			startTimers();
		} catch (err) {
			error = getApiErrorMessage(err, 'Unable to create an import code.');
		} finally {
			creating = false;
		}
	}

	function startTimers() {
		stopTimers();
		clock = setInterval(() => {
			now = Date.now();
			if (session && secondsUntilExpiry(session.expiresAt, now) === 0) stopTimers();
		}, 1000);
		poller = setInterval(() => void refreshStatus(), 2000);
	}

	async function refreshStatus() {
		if (!session) return;
		try {
			const status = await NytImportService.getApiImportNytSession({
				sessionId: session.sessionId
			});
			if (status.completedAt) {
				summary = status;
				stopTimers();
			}
		} catch {
			// A transient polling failure should not discard a still-valid import code.
		}
	}

	function stopTimers() {
		if (clock) clearInterval(clock);
		if (poller) clearInterval(poller);
		clock = null;
		poller = null;
	}

	onDestroy(stopTimers);
</script>

<svelte:head><title>Import from NYT | Wordle Tracker Supreme</title></svelte:head>

{#if !$auth.user}
	<section
		class="mx-auto max-w-2xl rounded-3xl border border-white/10 bg-white/5 p-8 text-center shadow-2xl"
		data-testid="nyt-import-unauthenticated"
	>
		<h1 class="text-3xl font-semibold text-white">Import from NYT</h1>
		<p class="mt-3 text-slate-200/75">Sign in to create a private, short-lived import code.</p>
		<a
			href={resolve('/signin')}
			class="mt-6 inline-flex rounded-full bg-emerald-400 px-5 py-2.5 font-semibold text-slate-900"
			>Sign in</a
		>
	</section>
{:else}
	<div class="mx-auto max-w-4xl space-y-6" data-testid="nyt-import-page">
		<section class="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-2xl md:p-10">
			<div class="grid gap-8 md:grid-cols-[1.3fr_0.7fr] md:items-start">
				<div>
					<h1 class="text-3xl font-semibold text-white md:text-4xl">
						Bring your NYT Wordle history with you
					</h1>
					<p class="mt-4 max-w-2xl leading-7 text-slate-200/80">
						Import completed games from your own New York Times account. They become normal
						NYT-stream attempts in your history, streaks and NYT-inclusive statistics.
					</p>
					<div
						class="mt-6 rounded-2xl border border-emerald-300/20 bg-emerald-400/10 p-4 text-sm leading-6 text-emerald-50"
					>
						Your NYT password, account ID and cookies are never sent to Tracker Supreme. The
						extension only uploads extracted Wordle guesses and result details.
					</div>
				</div>
				<ol class="space-y-4 text-sm text-slate-200/80">
					<li>
						<strong class="text-white">1.</strong> Load the unpacked Tracker Supreme extension in Chrome.
					</li>
					<li>
						<strong class="text-white">2.</strong> Open the official NYT Wordle page and sign in to NYT.
					</li>
					<li>
						<strong class="text-white">3.</strong> Generate a code below, open the extension and enter
						it.
					</li>
				</ol>
			</div>
		</section>

		<section
			class="rounded-3xl border border-white/10 bg-slate-900/70 p-8 shadow-xl"
			data-testid="nyt-import-session"
		>
			{#if !session}
				<h2 class="text-2xl font-semibold text-white">Create a secure import code</h2>
				<p class="mt-2 text-sm text-slate-200/70">
					The code expires in about ten minutes and stops working after one import.
				</p>
				<button
					onclick={() => void createSession()}
					disabled={creating}
					data-testid="generate-import-code"
					class="mt-6 rounded-full bg-emerald-400 px-5 py-3 font-semibold text-slate-900 transition hover:bg-emerald-300 disabled:opacity-60"
					>{creating ? 'Creating code…' : 'Generate import code'}</button
				>
			{:else if summary}
				<h2 class="text-2xl font-semibold text-white">Import complete</h2>
				<div class="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-5" data-testid="import-summary">
					{#each [['Imported', summary.imported], ['Duplicates', summary.duplicates], ['Conflicts', summary.conflicts], ['Rejected', summary.rejected], ['Unavailable', summary.missingFromNyt]] as item (item[0])}
						<div class="rounded-2xl border border-white/10 bg-white/5 p-4">
							<div class="text-2xl font-semibold text-white">{item[1]}</div>
							<div class="mt-1 text-xs text-slate-300/70">{item[0]}</div>
						</div>
					{/each}
				</div>
				{#if hasUnavailableHistory(summary.aggregateGamesPlayed, summary.requested)}
					<p
						class="mt-5 rounded-2xl border border-amber-300/20 bg-amber-400/10 p-4 text-sm text-amber-50"
						data-testid="incomplete-history"
					>
						NYT reports more completed games than individual puzzle states were available for. Those
						games are unavailable or unknown—not assumed to be unplayed—and cannot be reconstructed
						from aggregate statistics.
					</p>
				{/if}
				<div class="mt-6 flex flex-wrap gap-3">
					<button
						onclick={() => {
							session = null;
							summary = null;
						}}
						class="rounded-full border border-white/20 px-5 py-2.5 text-sm font-semibold"
						>Import again</button
					><a
						href="https://www.nytimes.com/games/wordle/index.html#stats"
						target="_blank"
						rel="noreferrer"
						class="rounded-full bg-white/10 px-5 py-2.5 text-sm font-semibold"
						>View NYT Wordle stats</a
					>
				</div>
			{:else}
				<div class="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
					<div>
						<div class="text-sm text-slate-200/70">Enter this code in the Chrome extension</div>
						<div
							class="mt-2 font-mono text-3xl font-bold tracking-[0.15em] text-emerald-300"
							data-testid="import-code"
						>
							{session.code}
						</div>
					</div>
					<div class="text-left sm:text-right">
						<div class="text-sm text-slate-200/70">Expires in</div>
						<div
							class="mt-2 text-2xl font-semibold {remaining === 0 ? 'text-rose-300' : 'text-white'}"
							data-testid="import-countdown"
						>
							{formatCountdown(remaining)}
						</div>
					</div>
				</div>
				<p class="mt-6 animate-pulse text-sm text-cyan-200" data-testid="import-progress">
					Waiting for the extension to finish the import…
				</p>
			{/if}
			{#if error}<p class="mt-4 text-sm text-rose-200" data-testid="import-error">{error}</p>{/if}
		</section>
	</div>
{/if}
