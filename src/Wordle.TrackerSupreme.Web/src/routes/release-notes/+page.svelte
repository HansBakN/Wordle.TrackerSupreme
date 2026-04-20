<script lang="ts">
	import { releaseNotes } from '$lib/release-notes/releases';
</script>

<svelte:head>
	<title>Release notes | Wordle Tracker Supreme</title>
</svelte:head>

<div class="space-y-8">
	<section class="border-b border-white/10 pb-8">
		<p class="text-sm tracking-[0.2em] text-emerald-200/80 uppercase">Product updates</p>
		<h1 class="mt-3 text-4xl font-semibold text-white">Release notes</h1>
		<p class="mt-4 max-w-3xl text-sm leading-6 text-slate-200/80">
			Changes are collected on a release branch before deployment, then summarized here before the
			branch is promoted to main.
		</p>
	</section>

	<section class="grid gap-6 lg:grid-cols-[minmax(0,_1.8fr)_minmax(280px,_0.9fr)]">
		<div class="space-y-5">
			{#each releaseNotes as release (release.slug)}
				<article
					id={release.slug}
					class="rounded-2xl border border-white/10 bg-white/5 p-6 shadow-xl"
					aria-labelledby={`${release.slug}-title`}
				>
					<div class="flex flex-wrap items-center gap-3">
						<h2 id={`${release.slug}-title`} class="text-2xl font-semibold text-white">
							{release.title}
						</h2>
						<span
							class="rounded-full border border-cyan-300/30 bg-cyan-400/10 px-3 py-1 text-xs font-semibold tracking-[0.16em] text-cyan-100 uppercase"
						>
							{release.status}
						</span>
					</div>
					<p class="mt-1 text-xs tracking-[0.16em] text-slate-300/70 uppercase">{release.date}</p>
					<p class="mt-4 text-sm leading-6 text-slate-100/80">{release.summary}</p>

					<div class="mt-6 grid gap-5 md:grid-cols-2">
						<div>
							<h3 class="text-xs font-semibold tracking-[0.2em] text-emerald-200/80 uppercase">
								Player changes
							</h3>
							<ul class="mt-3 space-y-2 text-sm leading-6 text-slate-100/80">
								{#each release.userFacing as item (item)}
									<li>{item}</li>
								{/each}
							</ul>
						</div>
						<div>
							<h3 class="text-xs font-semibold tracking-[0.2em] text-cyan-200/80 uppercase">
								Operational notes
							</h3>
							<ul class="mt-3 space-y-2 text-sm leading-6 text-slate-100/80">
								{#each release.operations as item (item)}
									<li>{item}</li>
								{/each}
							</ul>
						</div>
					</div>
				</article>
			{/each}
		</div>

		<aside class="rounded-2xl border border-white/10 bg-black/25 p-6 shadow-xl">
			<h2 class="text-lg font-semibold text-white">Release branch workflow</h2>
			<ol class="mt-4 space-y-3 text-sm leading-6 text-slate-100/80">
				<li>Merge completed feature and fix branches into `release/&lt;version-or-date&gt;`.</li>
				<li>Update the release-note data with a short player-facing summary.</li>
				<li>Run the release branch verification before merging to `main` for deployment.</li>
			</ol>
		</aside>
	</section>
</div>
