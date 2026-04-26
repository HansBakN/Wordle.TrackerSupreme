<script lang="ts">
	import { onMount } from 'svelte';
	import { ApiError } from '$lib/api-client';
	import { AdminService } from '$lib/api-client/services/AdminService';
	import type { AdminPuzzleResponse } from '$lib/api-client/models/AdminPuzzleResponse';

	type Puzzle = {
		id: string;
		puzzleDate: string;
		solution: string;
		attemptCount: number;
	};

	let loading = $state(false);
	let error = $state<string | null>(null);
	let puzzles = $state<Puzzle[]>([]);

	let showForm = $state(false);
	let editingId = $state<string | null>(null);
	let formDate = $state('');
	let formSolution = $state('');
	let formSaving = $state(false);
	let formError = $state<string | null>(null);

	let deletingId = $state<string | null>(null);

	function getErrorMessage(err: unknown, fallback: string): string {
		if (err instanceof ApiError) {
			const body = err.body as { detail?: string; message?: string } | undefined;
			return body?.detail ?? body?.message ?? fallback;
		}
		return err instanceof Error ? err.message : fallback;
	}

	function normalizePuzzle(p: AdminPuzzleResponse): Puzzle {
		return {
			id: p.id ?? '',
			puzzleDate: p.puzzleDate ?? '',
			solution: p.solution ?? '',
			attemptCount: p.attemptCount ?? 0
		};
	}

	async function loadPuzzles() {
		loading = true;
		error = null;
		try {
			const response = await AdminService.getApiAdminPuzzles();
			puzzles = response.map(normalizePuzzle);
		} catch (err) {
			error = getErrorMessage(err, 'Unable to load puzzles.');
		} finally {
			loading = false;
		}
	}

	function openCreateForm() {
		editingId = null;
		formDate = '';
		formSolution = '';
		formError = null;
		showForm = true;
	}

	function openEditForm(puzzle: Puzzle) {
		editingId = puzzle.id;
		formDate = puzzle.puzzleDate;
		formSolution = puzzle.solution;
		formError = null;
		showForm = true;
	}

	function closeForm() {
		showForm = false;
		editingId = null;
		formError = null;
	}

	async function saveForm() {
		if (!formDate || !formSolution.trim()) {
			formError = 'Date and solution are required.';
			return;
		}
		formSaving = true;
		formError = null;
		try {
			if (editingId) {
				const updated = await AdminService.putApiAdminPuzzles({
					puzzleId: editingId,
					requestBody: { puzzleDate: formDate, solution: formSolution.trim().toUpperCase() }
				});
				puzzles = puzzles.map((p) => (p.id === editingId ? normalizePuzzle(updated) : p));
			} else {
				const created = await AdminService.postApiAdminPuzzles({
					requestBody: { puzzleDate: formDate, solution: formSolution.trim().toUpperCase() }
				});
				puzzles = [normalizePuzzle(created), ...puzzles];
			}
			closeForm();
		} catch (err) {
			formError = getErrorMessage(err, 'Unable to save puzzle.');
		} finally {
			formSaving = false;
		}
	}

	onMount(() => {
		void loadPuzzles();
	});

	async function deletePuzzle(puzzleId: string) {
		deletingId = puzzleId;
		try {
			await AdminService.deleteApiAdminPuzzles({ puzzleId });
			puzzles = puzzles.filter((p) => p.id !== puzzleId);
		} catch (err) {
			error = getErrorMessage(err, 'Unable to delete puzzle.');
		} finally {
			deletingId = null;
		}
	}
</script>

<section class="space-y-4" data-testid="admin-puzzles">
	<div class="flex items-center justify-between">
		<h2 class="text-sm font-semibold tracking-[0.2em] text-slate-200/70 uppercase">
			Puzzle schedule
		</h2>
		<div class="flex gap-2">
			<button
				class="rounded-full border border-white/20 bg-white/10 px-3 py-1 text-xs font-semibold tracking-[0.2em] text-white/80 uppercase transition hover:border-white/40"
				onclick={loadPuzzles}
				disabled={loading}
				data-testid="admin-puzzles-refresh"
			>
				{loading ? 'Loading...' : 'Refresh'}
			</button>
			<button
				class="rounded-full bg-emerald-400 px-3 py-1 text-xs font-semibold tracking-[0.2em] text-slate-900 uppercase transition hover:bg-emerald-300"
				onclick={openCreateForm}
				data-testid="admin-puzzles-create"
			>
				New puzzle
			</button>
		</div>
	</div>

	{#if error}
		<div class="rounded-2xl border border-rose-300/30 bg-rose-500/10 p-3 text-sm text-rose-100">
			{error}
		</div>
	{/if}

	{#if showForm}
		<div
			class="space-y-3 rounded-2xl border border-emerald-400/30 bg-emerald-500/5 p-4"
			data-testid="admin-puzzle-form"
		>
			<h3 class="text-xs font-semibold tracking-[0.2em] text-emerald-200/80 uppercase">
				{editingId ? 'Edit puzzle' : 'Schedule new puzzle'}
			</h3>
			<div class="grid gap-3 sm:grid-cols-2">
				<label class="block">
					<span class="text-xs text-slate-200/70">Date</span>
					<input
						type="date"
						class="mt-1 w-full rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-sm text-white outline-none focus:border-emerald-400/60"
						bind:value={formDate}
						data-testid="admin-puzzle-date"
					/>
				</label>
				<label class="block">
					<span class="text-xs text-slate-200/70">Solution</span>
					<input
						class="mt-1 w-full rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-sm text-white uppercase outline-none focus:border-emerald-400/60"
						maxlength={10}
						placeholder="e.g. CRANE"
						bind:value={formSolution}
						data-testid="admin-puzzle-solution"
					/>
				</label>
			</div>
			{#if formError}
				<div class="text-xs text-rose-200">{formError}</div>
			{/if}
			<div class="flex gap-2 text-xs font-semibold tracking-[0.2em] uppercase">
				<button
					class="rounded-full bg-emerald-400 px-3 py-1 text-slate-900 transition hover:bg-emerald-300"
					onclick={saveForm}
					disabled={formSaving}
					data-testid="admin-puzzle-save"
				>
					{formSaving ? 'Saving...' : 'Save'}
				</button>
				<button
					class="rounded-full border border-white/20 px-3 py-1 text-white/70"
					onclick={closeForm}
					disabled={formSaving}
				>
					Cancel
				</button>
			</div>
		</div>
	{/if}

	{#if loading && puzzles.length === 0}
		<div class="text-sm text-slate-200/70">Loading puzzles...</div>
	{:else if puzzles.length === 0}
		<div class="rounded-2xl border border-white/10 bg-black/30 p-6 text-sm text-slate-200/70">
			No puzzles scheduled yet.
		</div>
	{:else}
		<div class="max-h-96 space-y-2 overflow-y-auto">
			{#each puzzles as puzzle (puzzle.id)}
				<div
					class="flex items-center justify-between rounded-2xl border border-white/10 bg-black/30 px-4 py-3"
					data-testid="admin-puzzle-row"
				>
					<div>
						<div class="text-sm font-semibold text-white">{puzzle.puzzleDate}</div>
						<div class="text-xs text-slate-200/60">
							{puzzle.solution} • {puzzle.attemptCount} attempts
						</div>
					</div>
					<div class="flex gap-2 text-xs font-semibold tracking-[0.2em] uppercase">
						{#if puzzle.attemptCount === 0}
							<button
								class="rounded-full border border-white/20 px-3 py-1 text-white/80 transition hover:border-white/40"
								onclick={() => openEditForm(puzzle)}
								data-testid="admin-puzzle-edit"
							>
								Edit
							</button>
							<button
								class="rounded-full border border-rose-300/40 px-3 py-1 text-rose-100 transition hover:border-rose-200"
								onclick={() => deletePuzzle(puzzle.id)}
								disabled={deletingId === puzzle.id}
								data-testid="admin-puzzle-delete"
							>
								{deletingId === puzzle.id ? '...' : 'Delete'}
							</button>
						{:else}
							<span class="px-3 py-1 text-slate-200/40">Locked</span>
						{/if}
					</div>
				</div>
			{/each}
		</div>
	{/if}
</section>
