using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.Engine
{
	/// <summary>Which cadence a system rides. Tier assignment changes ONLY on
	/// explicit events (POI enter/exit, system load/unload) — never a
	/// per-tick reclassification scan.</summary>
	public enum SimTier
	{
		/// <summary>30 Hz — the current system's live context: player
		/// kinematics, in-system NPCs, projectiles, active-POI logic,
		/// mass-lock/SafeZone bookkeeping.</summary>
		FullFrame,

		/// <summary>6 Hz (every 5th tick) — current-system off-POI
		/// bookkeeping: traffic director spawn/despawn decisions, in-system
		/// economy ticks, ambient events.</summary>
		SystemTraffic,

		/// <summary>1 Hz (every 30th tick) — POIs in loaded contexts that are
		/// not the player's.</summary>
		PoiAmbient,
	}

	/// <summary>The fixed per-tick phase order. Nothing may reorder or skip a
	/// phase: Commands → Simulate → AI → Housekeeping, every tick. Parallel
	/// work happens WITHIN a phase, never across tiers or phases.</summary>
	public enum SimPhase
	{
		Commands,
		Simulate,
		AI,
		Housekeeping,
	}

	/// <summary>
	/// Read-only per-tick context handed to every system. Blittable by
	/// standing rule — hot sim paths carry no strings, no managed objects,
	/// no registry access inside per-tick math (threading rule 1; this keeps
	/// everything Burst-eligible for the life of the project).
	/// </summary>
	public readonly struct SimContext
	{
		public readonly long TickIndex;

		/// <summary>Accumulated simulated seconds, computed as
		/// tickIndex * deltaTime — exact by construction, no drift.</summary>
		public readonly double SimTime;

		/// <summary>The Fixed Timestep this tick ran at (1/30 s).</summary>
		public readonly float DeltaTime;

		public SimContext(long tickIndex, float deltaTime, double simTime)
		{
			TickIndex = tickIndex;
			DeltaTime = deltaTime;
			SimTime = simTime;
		}
	}

	/// <summary>
	/// A unit of simulation work. Systems register with the scheduler
	/// declaring their tier and phase; the scheduler walks the fixed phase
	/// order every tick and runs each system when its tier is due. Plain
	/// class or component at the implementer's choice — the scheduler holds
	/// references, it does not care.
	/// </summary>
	public interface ISimSystem
	{
		SimTier Tier { get; }
		SimPhase Phase { get; }
		void Tick(in SimContext context);
	}

	/// <summary>
	/// The tick machine's driver. Cadence is integer-only off the tick
	/// counter — FullFrame every tick, SystemTraffic every 5th, PoiAmbient
	/// every 30th — the pinned 30/6/1 Hz shape at the 30 Hz heartbeat.
	/// There is NO SystemSlow tier: unloaded systems are RESOLVED (the
	/// WorldResolver seam), never ticked. Budgets are measured in ticks, not
	/// frame deltas — the legacy engine read Time.deltaTime inside
	/// FixedUpdate, a frame delta in the fixed step, and that class of
	/// mistake is what the integer cadence makes impossible.
	/// </summary>
	public class TickScheduler
	{
		// FullFrame 30 Hz -> SystemTraffic 6 Hz -> PoiAmbient 1 Hz.
		private const int SystemTrafficEvery = 5;
		private const int PoiAmbientEvery = 30;

		private readonly List<ISimSystem> systems = new List<ISimSystem>(64);

		private long tickIndex;

		public long TickIndex => tickIndex;

		public void Register(ISimSystem system)
		{
			// Register-time guard: a double registration would double-tick a
			// system every due tick, and nothing downstream would know why.
			if (system == null || systems.Contains(system))
			{
				return;
			}

			systems.Add(system);
		}

		public void Unregister(ISimSystem system)
		{
			systems.Remove(system);
		}

		/// <summary>One heartbeat: walk the four phases in the pinned order
		/// and run every due system. Called from EngineCore.FixedUpdate —
		/// never from Update, so the machine cannot double-run.</summary>
		public void Tick(float fixedDelta)
		{
			long tick = ++tickIndex;
			var context = new SimContext(tick, fixedDelta, tick * (double)fixedDelta);
			bool trafficDue = tick % SystemTrafficEvery == 0;
			bool ambientDue = tick % PoiAmbientEvery == 0;

			for (SimPhase phase = SimPhase.Commands; phase <= SimPhase.Housekeeping; phase++)
			{
				RunPhase(phase, context, trafficDue, ambientDue);
			}
		}

		private void RunPhase(SimPhase phase, in SimContext context, bool trafficDue, bool ambientDue)
		{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			// Developer-read budget counters — compiled out of release
			// entirely (only a developer reads them). Surfaced through the
			// dev overlay once one exists; no release cost is acceptable.
			long watchStart = System.Diagnostics.Stopwatch.GetTimestamp();
#endif
			int count = systems.Count;
			for (int i = 0; i < count; i++)
			{
				ISimSystem system = systems[i];
				if (system.Phase != phase || !IsDue(system.Tier, trafficDue, ambientDue))
				{
					continue;
				}

				system.Tick(context);
			}
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			RecordDevCost(phase, (System.Diagnostics.Stopwatch.GetTimestamp() - watchStart) * 1000.0
				/ System.Diagnostics.Stopwatch.Frequency);
#endif
		}

		private static bool IsDue(SimTier tier, bool trafficDue, bool ambientDue)
		{
			switch (tier)
			{
				case SimTier.FullFrame: return true;
				case SimTier.SystemTraffic: return trafficDue;
				case SimTier.PoiAmbient: return ambientDue;
				default: return false;
			}
		}

#if DEVELOPMENT_BUILD || UNITY_EDITOR
		private readonly double[] lastPhaseMillis = new double[4];

		private void RecordDevCost(SimPhase phase, double millis)
		{
			lastPhaseMillis[(int)phase] = millis;
		}

		/// <summary>Last tick's per-phase cost in milliseconds. Dev only —
		/// the field does not exist in release builds.</summary>
		public double LastPhaseMillis(SimPhase phase) => lastPhaseMillis[(int)phase];
#endif
	}
}
