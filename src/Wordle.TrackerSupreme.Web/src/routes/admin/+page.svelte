<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/auth/store';
	import { AdminService } from '$lib/api-client/services/AdminService';
	import type { AdminPlayerAttemptResponse as ApiAdminPlayerAttemptResponse } from '$lib/api-client/models/AdminPlayerAttemptResponse';
	import type { AdminPlayerDetailResponse as ApiAdminPlayerDetailResponse } from '$lib/api-client/models/AdminPlayerDetailResponse';
	import type { AdminPlayerSummaryResponse as ApiAdminPlayerSummaryResponse } from '$lib/api-client/models/AdminPlayerSummaryResponse';
	import type { GuessResponse } from '$lib/api-client/models/GuessResponse';

	const wordLength = 5;
	const maxGuesses = 6;

	type AdminGuess = {
		id: string;
		guessWord: string;
		guessNumber: number;
	};
	type AdminPlayerAttempt = {
		attemptId: string;
		puzzleDate: string;
		status: string;
		playedInHardMode: boolean;
		createdOn: string;
		completedOn: string | null;
		guesses: AdminGuess[];
	};
	type AdminPlayerDetail = {
		id: string;
		displayName: string;
		email: string;
		createdOn: string;
		isAdmin: boolean;
		attempts: AdminPlayerAttempt[];
	};
	type AdminPlayerSummary = {
		id: string;
		displayName: string;
		email: string;
		createdOn: string;
		isAdmin: boolean;
		attemptCount: number;
	};

	let loading = $state(false);
	let error = $state<string | null>(null);
	let players = $state<AdminPlayerSummary[]>([]);
	let selectedPlayer = $state<AdminPlayerDetail | null>(null);
	let query = $state('');
	let filteredPlayers = $state<AdminPlayerSummary[]>([]);

	let displayNameDraft = $state('');
	let emailDraft = $state('');
	let profileSaving = $state(false);
	let profileError = $state<string | null>(null);

	let passwordDraft = $state('');
	let passwordSaving = $state(false);
	let passwordMessage = $state<string | null>(null);

	let adminSaving = $state(false);
	let adminError = $state<string | null>(null);

	let editingAttemptId = $state<string | null>(null);
	let draftGuesses = $state<string[]>([]);
	let draftHardMode = $state(true);
	let attemptSaving = $state(false);
	let attemptError = $state<string | null>(null);
	let deletingAttemptId = $state<string | null>(null);

	$effect(() => {
		const term = query.trim().toLowerCase();
		if (!term) {
			filteredPlayers = players;
			return;
		}

		filteredPlayers = players.filter((player) => {
			return (
				player.displayName.toLowerCase().includes(term) || player.email.toLowerCase().includes(term)
			);
		});
	});

	function formatDate(value: string) {
		const date = new Date(value);
		if (Number.isNaN(date.getTime())) {
			return value;
		}
		return date.toLocaleDateString();
	}

	async function loadPlayers() {
		if (!$auth.user?.isAdmin) {
			return;
		}
		loading = true;
		error = null;
		try {
			const response = await AdminService.getApiAdminPlayers();
			players = response.map(normalizeSummary);
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load admin roster.';
		} finally {
			loading = false;
		}
	}

	async function selectPlayer(playerId: string) {
		if (!$auth.user?.isAdmin) {
			return;
		}
		loading = true;
		error = null;
		try {
			const detail = await AdminService.getApiAdminPlayers1({ playerId });
			selectedPlayer = normalizeDetail(detail);
			displayNameDraft = detail.displayName ?? '';
			emailDraft = detail.email ?? '';
			passwordDraft = '';
			passwordMessage = null;
			profileError = null;
			adminError = null;
			stopEditing();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unable to load player details.';
		} finally {
			loading = false;
		}
	}

	function stopEditing() {
		editingAttemptId = null;
		draftGuesses = [];
		draftHardMode = true;
		attemptError = null;
	}

	function startEditing(attempt: AdminPlayerAttempt) {
		editingAttemptId = attempt.attemptId;
		draftGuesses = attempt.guesses.map((guess) => guess.guessWord);
		draftHardMode = attempt.playedInHardMode;
		attemptError = null;
	}

	function updateGuess(index: number, value: string) {
		const next = [...draftGuesses];
		next[index] = value.toUpperCase();
		draftGuesses = next;
	}

	function addGuess() {
		if (draftGuesses.length >= maxGuesses) {
			return;
		}
		draftGuesses = [...draftGuesses, ''];
	}

	function removeGuess(index: number) {
		draftGuesses = draftGuesses.filter((_, idx) => idx !== index);
	}

	async function saveProfile() {
		if (!selectedPlayer) {
			return;
		}
		profileSaving = true;
		profileError = null;
		try {
			const detail = await AdminService.putApiAdminPlayers({
				playerId: selectedPlayer.id,
				requestBody: {
					displayName: displayNameDraft,
					email: emailDraft
				}
			});
			selectedPlayer = normalizeDetail(detail);
			players = players.map((player) =>
				player.id === detail.id
					? {
							...player,
							displayName: detail.displayName ?? '',
							email: detail.email ?? ''
						}
					: player
			);
		} catch (err) {
			profileError = err instanceof Error ? err.message : 'Unable to update profile.';
		} finally {
			profileSaving = false;
		}
	}

	async function resetPassword() {
		if (!selectedPlayer) {
			return;
		}
		passwordSaving = true;
		passwordMessage = null;
		try {
			await AdminService.putApiAdminPlayersPassword({
				playerId: selectedPlayer.id,
				requestBody: { password: passwordDraft }
			});
			passwordDraft = '';
			passwordMessage = 'Password reset successfully.';
		} catch (err) {
			passwordMessage = err instanceof Error ? err.message : 'Unable to reset password.';
		} finally {
			passwordSaving = false;
		}
	}

	async function toggleAdmin(isAdmin: boolean) {
		if (!selectedPlayer) {
			return;
		}
		adminSaving = true;
		adminError = null;
		try {
			const detail = await AdminService.putApiAdminPlayersAdmin({
				playerId: selectedPlayer.id,
				requestBody: { isAdmin }
			});
			selectedPlayer = normalizeDetail(detail);
			players = players.map((player) =>
				player.id === detail.id ? { ...player, isAdmin: detail.isAdmin ?? false } : player
			);
		} catch (err) {
			adminError = err instanceof Error ? err.message : 'Unable to update admin status.';
		} finally {
			adminSaving = false;
		}
	}

	async function saveAttempt(attemptId: string) {
		if (!selectedPlayer) {
			return;
		}
		attemptSaving = true;
		attemptError = null;
		try {
			const updated = await AdminService.putApiAdminAttempts({
				attemptId,
				requestBody: {
					guesses: draftGuesses.map((guess) => guess.trim()),
					playedInHardMode: draftHardMode
				}
			});
			const normalizedAttempt = normalizeAttempt(updated);
			selectedPlayer = {
				...selectedPlayer,
				attempts: selectedPlayer.attempts.map((attempt) =>
					attempt.attemptId === attemptId ? normalizedAttempt : attempt
				)
			};
			stopEditing();
		} catch (err) {
			attemptError = err instanceof Error ? err.message : 'Unable to update guesses.';
		} finally {
			attemptSaving = false;
		}
	}

	async function deleteAttempt(attemptId: string) {
		if (!selectedPlayer) {
			return;
		}
		deletingAttemptId = attemptId;
		attemptError = null;
		try {
			await AdminService.deleteApiAdminAttempts({ attemptId });
			selectedPlayer = {
				...selectedPlayer,
				attempts: selectedPlayer.attempts.filter((attempt) => attempt.attemptId !== attemptId)
			};
			players = players.map((player) =>
				player.id === selectedPlayer?.id
					? { ...player, attemptCount: selectedPlayer.attempts.length }
					: player
			);
			stopEditing();
		} catch (err) {
			attemptError = err instanceof Error ? err.message : 'Unable to reset attempt.';
		} finally {
			deletingAttemptId = null;
		}
	}

	onMount(() => {
		if ($auth.user?.isAdmin) {
			void loadPlayers();
		}
	});

	$effect(() => {
		if ($auth.user?.isAdmin && players.length === 0 && !loading) {
			void loadPlayers();
		}
		if (!$auth.user?.isAdmin) {
			players = [];
			selectedPlayer = null;
		}
	});

	function normalizeSummary(player: ApiAdminPlayerSummaryResponse): AdminPlayerSummary {
		return {
			id: player.id ?? '',
			displayName: player.displayName ?? '',
			email: player.email ?? '',
			createdOn: player.createdOn ?? '',
			isAdmin: player.isAdmin ?? false,
			attemptCount: player.attemptCount ?? 0
		};
	}

	function normalizeDetail(player: ApiAdminPlayerDetailResponse): AdminPlayerDetail {
		return {
			id: player.id ?? '',
			displayName: player.displayName ?? '',
			email: player.email ?? '',
			createdOn: player.createdOn ?? '',
			isAdmin: player.isAdmin ?? false,
			attempts: (player.attempts ?? []).map(normalizeAttempt)
		};
	}

	function normalizeAttempt(attempt: ApiAdminPlayerAttemptResponse): AdminPlayerAttempt {
		const attemptId = attempt.attemptId ?? '';
		return {
			attemptId,
			puzzleDate: attempt.puzzleDate ?? '',
			status: attempt.status ?? 'Unknown',
			playedInHardMode: attempt.playedInHardMode ?? true,
			createdOn: attempt.createdOn ?? '',
			completedOn: attempt.completedOn ?? null,
			guesses: (attempt.guesses ?? []).map((guess, index) =>
				normalizeGuess(guess, attemptId, index)
			)
		};
	}

	function normalizeGuess(guess: GuessResponse, attemptId: string, index: number): AdminGuess {
		return {
			id: guess.guessId ?? `${attemptId}-${index}`,
			guessWord: guess.guessWord ?? '',
			guessNumber: guess.guessNumber ?? index + 1
		};
	}
</script>

{#if !$auth.user}
	<div
		class="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-slate-200/80 shadow-xl"
	>
		<p class="text-lg font-semibold">Sign in to access admin controls.</p>
		<p class="mt-2 text-sm text-slate-200/70">Admin tooling is restricted to approved accounts.</p>
		<a
			href={resolve('/signin')}
			class="mt-6 inline-flex rounded-full bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-900 transition hover:bg-emerald-300"
		>
			Sign in
		</a>
	</div>
{:else if !$auth.user.isAdmin}
	<div
		class="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-slate-200/80 shadow-xl"
	>
		<p class="text-lg font-semibold">Admins only.</p>
		<p class="mt-2 text-sm text-slate-200/70">Contact your lead to request access.</p>
	</div>
{:else}
	<div class="space-y-6" data-testid="admin-page">
		<section class="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-2xl">
			<div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
				<div>
					<p class="text-sm tracking-[0.2em] text-emerald-200/80 uppercase">Admin console</p>
					<h1 class="mt-3 text-3xl font-semibold text-white">Operate the league</h1>
					<p class="mt-2 max-w-2xl text-sm text-slate-200/80">
						Update guesses, reset credentials, and keep the daily puzzle clean. All edits are
						audited in real time.
					</p>
				</div>
				<button
					class="rounded-full border border-white/20 bg-white/10 px-4 py-2 text-xs font-semibold tracking-[0.2em] text-white/80 uppercase transition hover:border-white/40 hover:bg-white/15"
					on:click={loadPlayers}
					disabled={loading}
					data-testid="admin-refresh"
				>
					{loading ? 'Refreshing...' : 'Refresh roster'}
				</button>
			</div>
		</section>

		{#if error}
			<div class="rounded-2xl border border-rose-300/30 bg-rose-500/10 p-4 text-sm text-rose-100">
				{error}
			</div>
		{/if}

		<section class="grid gap-6 lg:grid-cols-[minmax(0,_1fr)_minmax(0,_2fr)]">
			<div class="rounded-3xl border border-white/10 bg-black/30 p-6 shadow-xl">
				<div class="flex items-center justify-between">
					<h2 class="text-sm font-semibold tracking-[0.2em] text-slate-200/70 uppercase">Roster</h2>
					<span class="text-xs text-slate-200/60">{players.length} players</span>
				</div>
				<div class="mt-4">
					<input
						class="w-full rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-sm text-white outline-none focus:border-emerald-400/60"
						placeholder="Search by name or email"
						bind:value={query}
						data-testid="admin-search"
					/>
				</div>
				<div class="mt-5 space-y-2">
					{#if loading && players.length === 0}
						<div class="text-sm text-slate-200/70">Loading roster...</div>
					{:else if filteredPlayers.length === 0}
						<div class="text-sm text-slate-200/70">No players match this search.</div>
					{:else}
						{#each filteredPlayers as player (player.id)}
							<button
								class={`flex w-full items-center justify-between rounded-2xl border px-4 py-3 text-left text-sm transition ${
									selectedPlayer?.id === player.id
										? 'border-emerald-400/60 bg-emerald-500/10 text-white'
										: 'border-white/10 bg-white/5 text-slate-200/80 hover:border-white/30'
								}`}
								on:click={() => selectPlayer(player.id)}
								data-testid="admin-player-row"
							>
								<div>
									<div class="font-semibold text-white">{player.displayName}</div>
									<div class="text-xs text-slate-200/60">{player.email}</div>
								</div>
								<div class="text-right text-xs text-slate-200/60">
									<div>{player.attemptCount} attempts</div>
									{#if player.isAdmin}
										<div class="text-emerald-300">Admin</div>
									{/if}
								</div>
							</button>
						{/each}
					{/if}
				</div>
			</div>

			<div class="rounded-3xl border border-white/10 bg-white/5 p-6 shadow-xl">
				{#if !selectedPlayer}
					<div class="rounded-2xl border border-white/10 bg-black/30 p-6 text-sm text-slate-200/70">
						Select a player to manage their profile and attempt history.
					</div>
				{:else}
					<div class="space-y-6">
						<div
							class="flex flex-col gap-4 border-b border-white/10 pb-6 lg:flex-row lg:items-center lg:justify-between"
						>
							<div>
								<div class="text-sm tracking-[0.2em] text-slate-200/70 uppercase">
									Player profile
								</div>
								<h2 class="mt-2 text-2xl font-semibold text-white">
									{selectedPlayer.displayName}
								</h2>
								<p class="text-xs text-slate-200/60">
									Joined {formatDate(selectedPlayer.createdOn)}
								</p>
							</div>
							<label class="flex items-center gap-3 text-sm text-slate-200/80">
								<input
									type="checkbox"
									checked={selectedPlayer.isAdmin}
									on:change={(event) => toggleAdmin((event.target as HTMLInputElement).checked)}
									disabled={adminSaving}
									data-testid="admin-toggle"
								/>
								<span class="font-semibold">Admin access</span>
							</label>
						</div>

						{#if adminError}
							<div
								class="rounded-2xl border border-rose-300/30 bg-rose-500/10 p-3 text-sm text-rose-100"
							>
								{adminError}
							</div>
						{/if}

						<div class="grid gap-4 lg:grid-cols-2">
							<div class="rounded-2xl border border-white/10 bg-black/30 p-4">
								<h3 class="text-xs font-semibold tracking-[0.2em] text-slate-200/60 uppercase">
									Contact
								</h3>
								<div class="mt-4 space-y-3 text-sm">
									<label class="block">
										<span class="text-xs text-slate-200/70">Display name</span>
										<input
											class="mt-2 w-full rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-white outline-none focus:border-emerald-400/60"
											bind:value={displayNameDraft}
											data-testid="admin-display-name"
										/>
									</label>
									<label class="block">
										<span class="text-xs text-slate-200/70">Email</span>
										<input
											class="mt-2 w-full rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-white outline-none focus:border-emerald-400/60"
											bind:value={emailDraft}
											data-testid="admin-email"
										/>
									</label>
									<button
										class="w-full rounded-full bg-emerald-400 px-4 py-2 text-xs font-semibold tracking-[0.2em] text-slate-900 uppercase transition hover:bg-emerald-300"
										on:click={saveProfile}
										disabled={profileSaving}
										data-testid="admin-profile-save"
									>
										{profileSaving ? 'Saving...' : 'Save profile'}
									</button>
									{#if profileError}
										<div class="text-xs text-rose-200">{profileError}</div>
									{/if}
								</div>
							</div>
							<div class="rounded-2xl border border-white/10 bg-black/30 p-4">
								<h3 class="text-xs font-semibold tracking-[0.2em] text-slate-200/60 uppercase">
									Credentials
								</h3>
								<div class="mt-4 space-y-3 text-sm">
									<label class="block">
										<span class="text-xs text-slate-200/70">Reset password</span>
										<input
											type="password"
											class="mt-2 w-full rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-white outline-none focus:border-emerald-400/60"
											placeholder="New password"
											bind:value={passwordDraft}
											data-testid="admin-password"
										/>
									</label>
									<button
										class="w-full rounded-full border border-emerald-300/50 bg-emerald-500/10 px-4 py-2 text-xs font-semibold tracking-[0.2em] text-emerald-100 uppercase transition hover:border-emerald-200"
										on:click={resetPassword}
										disabled={passwordSaving || !passwordDraft}
										data-testid="admin-password-reset"
									>
										{passwordSaving ? 'Resetting...' : 'Reset password'}
									</button>
									{#if passwordMessage}
										<div class="text-xs text-slate-200/80">{passwordMessage}</div>
									{/if}
								</div>
							</div>
						</div>

						<div class="space-y-4">
							<div class="flex items-center justify-between">
								<h3 class="text-sm font-semibold tracking-[0.2em] text-slate-200/70 uppercase">
									Attempts
								</h3>
								<span class="text-xs text-slate-200/60">
									{selectedPlayer.attempts.length} total
								</span>
							</div>
							{#if selectedPlayer.attempts.length === 0}
								<div
									class="rounded-2xl border border-white/10 bg-black/30 p-6 text-sm text-slate-200/70"
								>
									No attempts recorded yet.
								</div>
							{:else}
								<div class="space-y-4">
									{#each selectedPlayer.attempts as attempt (attempt.attemptId)}
										<div
											class="rounded-2xl border border-white/10 bg-black/30 p-4"
											data-testid="admin-attempt-card"
										>
											<div
												class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between"
											>
												<div>
													<div class="text-sm font-semibold text-white">
														{attempt.puzzleDate}
													</div>
													<div class="text-xs text-slate-200/60">
														Status: {attempt.status} • {attempt.guesses.length} guesses
													</div>
												</div>
												<div
													class="flex flex-wrap gap-2 text-xs font-semibold tracking-[0.2em] uppercase"
												>
													<button
														class="rounded-full border border-white/20 px-3 py-1 text-white/80 transition hover:border-white/40"
														on:click={() => startEditing(attempt)}
														disabled={editingAttemptId === attempt.attemptId}
														data-testid="admin-attempt-edit"
													>
														Edit guesses
													</button>
													<button
														class="rounded-full border border-rose-300/40 px-3 py-1 text-rose-100 transition hover:border-rose-200"
														on:click={() => deleteAttempt(attempt.attemptId)}
														disabled={deletingAttemptId === attempt.attemptId}
														data-testid="admin-attempt-reset"
													>
														{deletingAttemptId === attempt.attemptId
															? 'Resetting...'
															: 'Reset attempt'}
													</button>
												</div>
											</div>

											{#if editingAttemptId === attempt.attemptId}
												<div class="mt-4 space-y-3">
													<div class="flex items-center gap-3 text-xs text-slate-200/70">
														<label class="flex items-center gap-2">
															<input type="checkbox" bind:checked={draftHardMode} />
															Hard mode
														</label>
														<span>{draftGuesses.length} / {maxGuesses} guesses</span>
													</div>
													<div class="grid gap-3 sm:grid-cols-2">
														{#each draftGuesses as guess, index (index)}
															<div class="flex items-center gap-2">
																<input
																	class="w-full rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-sm text-white uppercase outline-none focus:border-emerald-400/60"
																	maxlength={wordLength}
																	value={guess}
																	on:input={(event) =>
																		updateGuess(index, (event.target as HTMLInputElement).value)}
																	data-testid="admin-attempt-guess"
																/>
																<button
																	class="rounded-full border border-white/20 px-2 py-1 text-xs text-white/70"
																	on:click={() => removeGuess(index)}
																	disabled={draftGuesses.length === 0}
																>
																	×
																</button>
															</div>
														{/each}
													</div>
													<div
														class="flex flex-wrap items-center gap-2 text-xs font-semibold tracking-[0.2em] uppercase"
													>
														<button
															class="rounded-full border border-white/20 px-3 py-1 text-white/80 transition hover:border-white/40"
															on:click={addGuess}
															disabled={draftGuesses.length >= maxGuesses}
															data-testid="admin-attempt-add"
														>
															Add guess
														</button>
														<button
															class="rounded-full bg-emerald-400 px-3 py-1 text-slate-900 transition hover:bg-emerald-300"
															on:click={() => saveAttempt(attempt.attemptId)}
															disabled={attemptSaving}
															data-testid="admin-attempt-save"
														>
															{attemptSaving ? 'Saving...' : 'Save changes'}
														</button>
														<button
															class="rounded-full border border-white/20 px-3 py-1 text-white/70"
															on:click={stopEditing}
															disabled={attemptSaving}
														>
															Cancel
														</button>
													</div>
													{#if attemptError}
														<div class="text-xs text-rose-200">{attemptError}</div>
													{/if}
												</div>
											{:else}
												<div class="mt-4 flex flex-wrap gap-2 text-sm text-slate-200/80">
													{#if attempt.guesses.length === 0}
														<span class="text-xs text-slate-200/60">No guesses yet.</span>
													{:else}
														{#each attempt.guesses as guess (guess.id)}
															<span
																class="rounded-full border border-white/10 bg-white/5 px-3 py-1"
															>
																{guess.guessWord}
															</span>
														{/each}
													{/if}
												</div>
											{/if}
										</div>
									{/each}
								</div>
							{/if}
						</div>
					</div>
				{/if}
			</div>
		</section>
	</div>
{/if}
