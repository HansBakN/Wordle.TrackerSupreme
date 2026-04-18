<script lang="ts">
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';
	import { auth, bootstrapAuth, signOut } from '$lib/auth/store';
	import { colorMode } from '$lib/game/colorMode';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';

	let { children } = $props();
	let booting = $state(true);
	const isTestMode = import.meta.env.MODE === 'test';

	onMount(async () => {
		colorMode.init();

		if (isTestMode) {
			booting = false;
			void bootstrapAuth();
			return;
		}

		await bootstrapAuth();
		booting = false;
	});
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>

<div class="bg-surface min-h-screen text-slate-50">
	<header class="border-b border-white/10 bg-white/5 backdrop-blur">
		<div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
			<div class="flex items-center gap-3">
				<div
					class="flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-400 to-cyan-500 font-semibold text-black"
				>
					WT
				</div>
				<div>
					<div class="text-lg font-semibold tracking-tight">Wordle Tracker Supreme</div>
					<div class="text-xs text-slate-200/70">Keep your streaks honest.</div>
				</div>
			</div>
			{#if $auth.user}
				<nav
					class="hidden items-center gap-4 text-xs font-semibold tracking-[0.2em] text-slate-200/70 uppercase md:flex"
				>
					<a href={resolve('/')} class="transition hover:text-white">Play</a>
					<a href={resolve('/stats')} class="transition hover:text-white">Stats</a>
					<a href={resolve('/leaderboard')} class="transition hover:text-white">Leaderboard</a>
					{#if $auth.user?.isAdmin}
						<a href={resolve('/admin')} class="transition hover:text-white">Admin</a>
					{/if}
				</nav>
			{/if}
			<div class="flex items-center gap-3 text-sm">
				<button
					class={`flex h-8 w-8 items-center justify-center rounded-full border transition ${$colorMode ? 'border-blue-400/60 bg-blue-400/15 text-blue-300 hover:border-blue-400/80 hover:bg-blue-400/25' : 'border-white/20 bg-white/5 text-white/60 hover:border-white/40 hover:bg-white/10 hover:text-white'}`}
					onclick={() => colorMode.toggle()}
					aria-label={$colorMode ? 'Disable high-contrast mode' : 'Enable high-contrast mode'}
					aria-pressed={$colorMode}
					data-testid="toggle-high-contrast"
					title={$colorMode ? 'High-contrast: on' : 'High-contrast: off'}
				>
					<svg
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						class="h-4 w-4"
					>
						<circle cx="12" cy="12" r="10" />
						<path d="M12 2a10 10 0 0 1 0 20V2z" fill="currentColor" stroke="none" />
					</svg>
				</button>
				{#if $auth.user}
					<div class="hidden text-right sm:block">
						<div class="font-semibold">{$auth.user.displayName}</div>
						<div class="text-xs text-slate-200/70">Signed in</div>
					</div>
					<button
						class="rounded-full border border-white/20 bg-white/10 px-4 py-2 font-medium text-white transition hover:border-white/40 hover:bg-white/15"
						onclick={() => void signOut()}
					>
						Sign out
					</button>
				{:else}
					<a
						href={resolve('/signin')}
						class="rounded-full border border-white/20 bg-white/5 px-4 py-2 font-medium text-white transition hover:border-white/40 hover:bg-white/10"
						>Sign in</a
					>
					<a
						href={resolve('/signup')}
						class="rounded-full bg-emerald-400 px-4 py-2 font-semibold text-slate-900 transition hover:bg-emerald-300"
						>Sign up</a
					>
				{/if}
			</div>
		</div>
	</header>

	{#if $auth.error}
		<div class="border-b border-amber-300/30 bg-amber-500/15 text-amber-50">
			<div class="mx-auto max-w-6xl px-4 py-2 text-sm">{$auth.error}</div>
		</div>
	{/if}

	<main class="mx-auto max-w-6xl px-4 py-10">
		{#if booting}
			<div
				class="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-slate-200/80 shadow-xl"
			>
				Loading your session...
			</div>
		{:else}
			{@render children()}
		{/if}
	</main>
</div>
