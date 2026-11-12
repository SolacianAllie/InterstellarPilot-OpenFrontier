using Unity.Jobs.LowLevel.Unsafe;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// The ONE parallelism knob (pinned policy): a single object every
	/// parallel call site consults — no #if forks at call sites, so a beefy
	/// tablet earns PC-ish behaviour purely by boot-time configuration.
	/// Mobile-first defaults: serial until the work EARNS parallelism,
	/// because below the threshold the scheduling overhead can exceed the
	/// work on a phone. M1 has no parallel call sites; the knob exists so
	/// the first one lands through it, not around it.
	/// </summary>
	public sealed class ParallelismPolicy
	{
		/// <summary>PC 8–12, mobile 1–2 (pinned).</summary>
		public int MaxWorkers { get; set; } = 2;

		/// <summary>PC ~64 items, mobile ~1024 (pinned). Below the
		/// threshold: run inline, serially — never schedule.</summary>
		public int MinWorkToParallelize { get; set; } = 1024;

		/// <summary>
		/// Push the budget into Unity's job worker count. Call at boot, and
		/// again whenever the budget changes — under thermal pressure this
		/// is the ONE knob that shrinks (workers, escalation caps, render
		/// resolution).
		/// </summary>
		public void Apply()
		{
			JobsUtility.JobWorkerCount = MaxWorkers;
		}

		/// <summary>
		/// The only gate a call site needs: true → ParallelFor the batch;
		/// false → plain inline loop.
		/// </summary>
		public bool EarnsParallel(int workItemCount)
		{
			return workItemCount >= MinWorkToParallelize;
		}
	}
}
