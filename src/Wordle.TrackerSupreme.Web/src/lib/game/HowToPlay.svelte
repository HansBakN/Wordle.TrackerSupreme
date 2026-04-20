<script lang="ts">
	let { onclose }: { onclose: () => void } = $props();

	function handleBackdropClick(evt: MouseEvent) {
		if (evt.target === evt.currentTarget) {
			onclose();
		}
	}

	function handleKeydown(evt: KeyboardEvent) {
		if (evt.key === 'Escape') {
			onclose();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

<div
	class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4 backdrop-blur-sm"
	onclick={handleBackdropClick}
	role="presentation"
	data-testid="how-to-play-modal"
>
	<div
		class="relative max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-3xl border border-white/10 bg-gradient-to-b from-slate-800 to-slate-900 p-8 shadow-2xl"
		role="dialog"
		aria-modal="true"
		aria-labelledby="how-to-play-title"
		tabindex="-1"
	>
		<button
			class="absolute top-5 right-5 flex h-8 w-8 items-center justify-center rounded-full border border-white/20 bg-white/10 text-white/70 transition hover:border-white/40 hover:bg-white/20 hover:text-white"
			onclick={onclose}
			aria-label="Close how to play"
			data-testid="close-how-to-play"
		>
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-4 w-4">
				<path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
			</svg>
		</button>

		<h2 id="how-to-play-title" class="text-xl font-semibold tracking-tight text-white">
			How to play
		</h2>
		<p class="mt-2 text-sm text-slate-300/80">
			Guess the hidden 5-letter word in 6 tries. Each guess must be a valid word.
		</p>

		<div class="mt-6 space-y-6">
			<!-- Tile colour legend -->
			<section>
				<h3 class="mb-3 text-xs font-semibold tracking-[0.2em] text-slate-200/60 uppercase">
					Tile colours
				</h3>
				<div class="space-y-3">
					<div class="flex items-center gap-4">
						<div
							class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-emerald-400 bg-emerald-400 text-sm font-semibold text-slate-900"
							aria-label="Green tile example"
						>
							C
						</div>
						<p class="text-sm text-slate-200/90">
							<span class="font-semibold text-emerald-300">Green</span> — the letter is in the word and
							in the correct position.
						</p>
					</div>
					<div class="flex items-center gap-4">
						<div
							class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-amber-300/70 bg-amber-300 text-sm font-semibold text-slate-900"
							aria-label="Yellow tile example"
						>
							R
						</div>
						<p class="text-sm text-slate-200/90">
							<span class="font-semibold text-amber-300">Yellow</span> — the letter is in the word but
							in the wrong position.
						</p>
					</div>
					<div class="flex items-center gap-4">
						<div
							class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-white/15 bg-white/5 text-sm font-semibold text-white/60"
							aria-label="Grey tile example"
						>
							A
						</div>
						<p class="text-sm text-slate-200/90">
							<span class="font-semibold text-slate-300">Grey</span> — the letter is not in the word at
							all.
						</p>
					</div>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- Example word -->
			<section>
				<h3 class="mb-3 text-xs font-semibold tracking-[0.2em] text-slate-200/60 uppercase">
					Example
				</h3>
				<div class="flex gap-2" aria-label="Example guess: CRANE">
					{#each ['C', 'R', 'A', 'N', 'E'] as letter, i (i)}
						<div
							class={`flex h-12 w-12 items-center justify-center rounded-xl border text-base font-semibold ${
								i === 0
									? 'border-emerald-400 bg-emerald-400 text-slate-900'
									: i === 1
										? 'border-amber-300/70 bg-amber-300 text-slate-900'
										: 'border-white/15 bg-white/5 text-white/60'
							}`}
						>
							{letter}
						</div>
					{/each}
				</div>
				<p class="mt-3 text-sm text-slate-300/80">
					<strong class="text-white">C</strong> is correct and in position 1.
					<strong class="text-white">R</strong> is in the word but not in position 2.
					<strong class="text-white">A, N, E</strong> are not in the word.
				</p>
			</section>

			<hr class="border-white/10" />

			<!-- Hard mode vs Easy mode -->
			<section>
				<h3 class="mb-3 text-xs font-semibold tracking-[0.2em] text-slate-200/60 uppercase">
					Game modes
				</h3>
				<div class="space-y-3">
					<div class="rounded-xl border border-emerald-300/30 bg-emerald-400/5 p-3">
						<div class="flex items-center gap-2">
							<span
								class="rounded-full border border-emerald-300/60 bg-emerald-400/10 px-2 py-0.5 text-xs font-semibold tracking-[0.15em] text-emerald-100 uppercase"
							>
								Hard mode
							</span>
						</div>
						<p class="mt-2 text-sm text-slate-300/80">
							Every subsequent guess must include all green letters in their exact positions and all
							yellow letters somewhere in the word.
						</p>
					</div>
					<div class="rounded-xl border border-amber-300/30 bg-amber-400/5 p-3">
						<div class="flex items-center gap-2">
							<span
								class="rounded-full border border-amber-300/50 bg-amber-400/10 px-2 py-0.5 text-xs font-semibold tracking-[0.15em] text-amber-50 uppercase"
							>
								Easy mode
							</span>
						</div>
						<p class="mt-2 text-sm text-slate-300/80">
							No letter constraints. You can guess any valid word regardless of previous hints.
						</p>
					</div>
				</div>
				<p class="mt-3 text-xs text-slate-300/60">
					You can switch to Easy mode at any point before completing the puzzle — but only for that
					day's attempt.
				</p>
			</section>

			<hr class="border-white/10" />

			<!-- Daily cutoff -->
			<section>
				<h3 class="mb-3 text-xs font-semibold tracking-[0.2em] text-slate-200/60 uppercase">
					Daily cutoff
				</h3>
				<p class="text-sm text-slate-300/80">
					Solve the puzzle before <strong class="text-white">12:00 PM local time</strong> to count toward
					your streak and leaderboard ranking. Games started after noon are tracked separately as practice
					rounds and won't affect your stats.
				</p>
			</section>
		</div>

		<button
			class="mt-8 w-full rounded-2xl bg-emerald-400 py-3 text-sm font-semibold text-slate-900 transition hover:bg-emerald-300"
			onclick={onclose}
			data-testid="got-it-button"
		>
			Got it — let's play!
		</button>
	</div>
</div>
