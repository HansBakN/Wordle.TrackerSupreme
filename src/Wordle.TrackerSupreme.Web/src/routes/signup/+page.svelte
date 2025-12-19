<script lang="ts">
import { goto } from '$app/navigation';
import { auth, signUp } from '$lib/auth/store';
import { onMount } from 'svelte';

let displayName = '';
let email = '';
let password = '';
let confirmPassword = '';
let error: string | null = null;
let loading = false;

onMount(() => {
  const unsubscribe = auth.subscribe((state) => {
    if (!state.ready) return;
    if (state.user) {
      goto('/');
    }
  });
  return () => unsubscribe();
});

const handleSubmit = async () => {
  error = null;
  if (password !== confirmPassword) {
    error = 'Passwords do not match.';
    return;
  }

  loading = true;
  try {
    await signUp(displayName, email, password);
    goto('/');
  } catch (err) {
    error = err instanceof Error ? err.message : 'Unable to sign up.';
  } finally {
    loading = false;
  }
};
</script>

<div class="mx-auto max-w-lg rounded-3xl border border-white/10 bg-white/5 p-10 shadow-2xl">
	<p class="text-sm uppercase tracking-[0.2em] text-emerald-200/80">Create account</p>
	<h1 class="mt-2 text-3xl font-semibold text-white">Join Wordle Tracker</h1>
	<p class="mt-2 text-sm text-slate-200/80">
		Pick a display name and a password. You’ll use this combo to sign back in and keep your streaks synced.
	</p>

	<form
		class="mt-6 space-y-5"
		on:submit|preventDefault={handleSubmit}
	>
		<label class="block space-y-2">
			<span class="text-sm font-semibold text-white">Display name</span>
			<input
				name="displayName"
				bind:value={displayName}
				required
				minlength="2"
				class="w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none transition focus:border-emerald-300 focus:bg-black/40"
				placeholder="wordle-wizard"
			/>
		</label>

		<label class="block space-y-2">
			<span class="text-sm font-semibold text-white">Email</span>
			<input
				name="email"
				type="email"
				bind:value={email}
				required
				class="w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none transition focus:border-emerald-300 focus:bg-black/40"
				placeholder="you@example.com"
			/>
		</label>

		<label class="block space-y-2">
			<span class="text-sm font-semibold text-white">Password</span>
			<input
				name="password"
				type="password"
				bind:value={password}
				required
				minlength="6"
				class="w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none transition focus:border-emerald-300 focus:bg-black/40"
				placeholder="••••••••"
			/>
		</label>

		<label class="block space-y-2">
			<span class="text-sm font-semibold text-white">Confirm password</span>
			<input
				name="confirmPassword"
				type="password"
				bind:value={confirmPassword}
				required
				minlength="6"
				class="w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none transition focus:border-emerald-300 focus:bg-black/40"
				placeholder="••••••••"
			/>
		</label>

		{#if error}
			<div class="rounded-xl border border-red-400/40 bg-red-500/10 px-4 py-3 text-sm text-red-100">
				{error}
			</div>
		{/if}

		<button
			type="submit"
			class="flex w-full items-center justify-center gap-2 rounded-2xl bg-emerald-400 px-4 py-3 text-center text-base font-semibold text-slate-900 transition hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
			disabled={loading}
		>
			{loading ? 'Creating account...' : 'Sign up'}
		</button>
	</form>

	<p class="mt-4 text-sm text-slate-200/80">
		Already have an account?
		<a class="font-semibold text-emerald-200 hover:text-emerald-100" href="/signin">Sign in</a>.
	</p>
</div>
