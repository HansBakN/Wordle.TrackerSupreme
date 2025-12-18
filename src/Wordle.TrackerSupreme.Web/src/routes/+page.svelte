<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth, signOut } from '$lib/auth/store';
	import { onMount } from 'svelte';

	let checking = true;

	onMount(() => {
		const unsubscribe = auth.subscribe((state) => {
			if (!state.ready) return;
			checking = false;
			if (!state.user) {
				goto('/signin');
			}
		});

		return () => unsubscribe();
	});
</script>

{#if checking}
	<div class="rounded-2xl border border-white/10 bg-white/5 p-10 text-center text-slate-200/80 shadow-xl">
		Checking your session...
	</div>
{:else if $auth.user}
	<div class="grid gap-6 lg:grid-cols-3">
		<section class="lg:col-span-2 rounded-3xl border border-white/10 bg-gradient-to-br from-white/10 to-white/5 p-8 shadow-2xl">
			<div class="flex items-start justify-between gap-4">
				<div>
					<p class="text-sm uppercase tracking-[0.2em] text-emerald-200/80">Welcome back</p>
					<h1 class="mt-2 text-3xl font-semibold text-white">{$auth.user.displayName}</h1>
					<p class="mt-2 max-w-xl text-sm text-slate-200/80">
						Your Wordle streak guardian is ready. Track attempts, stay accountable, and never lose your
						winning rhythm.
					</p>
				</div>
				<button
					class="rounded-full border border-white/15 bg-white/10 px-4 py-2 text-sm font-semibold text-white transition hover:border-white/40 hover:bg-white/15"
					onclick={() => signOut()}
				>
					Sign out
				</button>
			</div>

			<div class="mt-8 grid gap-4 md:grid-cols-3">
				<div class="rounded-2xl border border-white/10 bg-black/20 p-4 shadow-inner">
					<p class="text-xs uppercase tracking-widest text-slate-200/60">Status</p>
					<p class="mt-2 text-2xl font-semibold text-white">Signed in</p>
				</div>
				<div class="rounded-2xl border border-white/10 bg-black/20 p-4 shadow-inner">
					<p class="text-xs uppercase tracking-widest text-slate-200/60">Member since</p>
					<p class="mt-2 text-xl font-semibold text-white">
						{new Date($auth.user.createdOn).toLocaleDateString()}
					</p>
				</div>
				<div class="rounded-2xl border border-white/10 bg-black/20 p-4 shadow-inner">
					<p class="text-xs uppercase tracking-widest text-slate-200/60">Next step</p>
					<p class="mt-2 text-xl font-semibold text-white">Add your first puzzle log</p>
				</div>
				<div class="rounded-2xl border border-white/10 bg-black/20 p-4 shadow-inner md:col-span-3">
					<p class="text-xs uppercase tracking-widest text-slate-200/60">Email</p>
					<p class="mt-2 text-xl font-semibold text-white break-words">{$auth.user.email}</p>
				</div>
			</div>
		</section>

		<section class="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-xl">
			<h2 class="text-xl font-semibold text-white">What’s next?</h2>
			<ul class="mt-4 space-y-3 text-sm text-slate-200/80">
				<li class="flex items-start gap-3">
					<span class="mt-1 h-2.5 w-2.5 rounded-full bg-emerald-400"></span>
					<div>
						<div class="font-semibold text-white">Record your latest Wordle</div>
						<div>Input guesses and results to start building your streak data.</div>
					</div>
				</li>
				<li class="flex items-start gap-3">
					<span class="mt-1 h-2.5 w-2.5 rounded-full bg-cyan-400"></span>
					<div>
						<div class="font-semibold text-white">Compare performance</div>
						<div>Watch averages, streaks, and time-to-solve improve over time.</div>
					</div>
				</li>
				<li class="flex items-start gap-3">
					<span class="mt-1 h-2.5 w-2.5 rounded-full bg-amber-300"></span>
					<div>
						<div class="font-semibold text-white">Stay accountable</div>
						<div>Sign in here before sharing results with friends.</div>
					</div>
				</li>
			</ul>
		</section>
	</div>
{/if}
