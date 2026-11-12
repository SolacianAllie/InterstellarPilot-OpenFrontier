using System.Collections.Generic;

namespace OpenFrontier.Engine
{
	/// <summary>Computes one unloaded system's coarse state at a sim time —
	/// pure math (microseconds), triggered on system load and galaxy-map
	/// refresh. Exercised by M6/M7 content; the seam exists at M1.</summary>
	public interface IWorldResolver
	{
		void Resolve(long systemId, double atSimTime);
	}

	/// <summary>
	/// The unloaded-world resolver's registry/entry-point seam (pinned:
	/// it exists at M1, it is exercised by M6/M7). Unloaded systems store
	/// nothing and tick never — their coarse state at sim-time t is
	/// COMPUTED from three inputs:
	///   1. Seeds — everything procedural;
	///   2. Schedules — seed-derived timelines walked from last-observed
	///      time to now (wormhole windows, phenomena spawns, conflict rolls);
	///   3. Player delta records — the ONLY mutable memory; a delta
	///      OVERRIDES the schedule for the thing it touches.
	/// A 30-system galaxy and a 1000-system galaxy cost the same while
	/// unloaded. Seed-derived math is INTEGER-ONLY (threading rule 2).
	/// </summary>
	public sealed class WorldResolver
	{
		private readonly List<IWorldResolver> resolvers = new List<IWorldResolver>(16);

		public void Register(IWorldResolver resolver)
		{
			// Double-register guard — a double registration double-resolves.
			if (resolver == null || resolvers.Contains(resolver))
			{
				return;
			}

			resolvers.Add(resolver);
		}

		public void Unregister(IWorldResolver resolver)
		{
			resolvers.Remove(resolver);
		}

		/// <summary>Resolve a system's coarse state as of a sim time. Pure
		/// math, no side effects on unloaded state — callers integrate the
		/// result where it becomes live.</summary>
		public void ResolveAll(long systemId, double atSimTime)
		{
			int count = resolvers.Count;
			for (int i = 0; i < count; i++)
			{
				resolvers[i].Resolve(systemId, atSimTime);
			}
		}
	}
}
