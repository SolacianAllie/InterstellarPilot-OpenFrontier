using UnityEngine;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// The world's boot conductor — the target scene carries one. The port
	/// shape of legacy WorldBase minus everything the persistent EngineCore
	/// obsoletes: no additive engine-scene load (the engine is already
	/// alive — it spawns AT BOOT now), no save-vs-new branch (saves are a
	/// later milestone; the payload field exists as the seam), no
	/// NewGameRoot handback (no authored starter kit at M1).
	///
	/// Order of business: adopt the static launch payload, then lift the
	/// load/pause protocol through the single authority and prove the chain
	/// by log. FAILS LOUDLY — the legacy init poll wrapped everything in a
	/// try/finally that set hasInitialised regardless, so a failed init
	/// read exactly like a ready world; nothing here is swallowed.
	/// </summary>
	public sealed class WorldBoot : MonoBehaviour
	{
		private void Start()
		{
			// Defensive: the stub scene may have been opened directly in the
			// editor (no landing boot, no launch) — Boot is idempotent.
			EngineCore core = EngineCore.Boot();

			LaunchPayload payload = EngineLauncher.ConsumePending();
			if (payload == null)
			{
				Debug.Log("[WorldBoot] No launch payload - bare boot (scene opened directly).");
			}
			else
			{
				string gamemodeId = payload.Gamemode != null ? payload.Gamemode.Id : "<null>";
				Debug.Log($"[WorldBoot] Adopted launch payload: gamemode '{gamemodeId}', seed {payload.ResolvedSeed}, scene '{payload.TargetSceneName}'.");
			}

			// M1 world state lives nowhere yet — no systems registered, the
			// stub world simulates nothing. Later milestones adopt the
			// payload HERE: seed settings, starting conditions, registries.
			// (The seed is already recorded on the payload for them.)

			EngineCore.NotifyWorldLoadedAndReady();
			Debug.Log($"[WorldBoot] World ready - timeScale lifted to 1, engine ticking from tick {core.Scheduler.TickIndex}.");

#if DEVELOPMENT_BUILD || UNITY_EDITOR
			// The unscaled proof marker: a dev-only system logging the tick
			// machine's heartbeat at 1 Hz. Compiled out of release — only a
			// developer reads it (the diagnostics-do-not-ship rule).
			core.Scheduler.Register(new EngineProveSystem());
#endif
		}
	}

#if DEVELOPMENT_BUILD || UNITY_EDITOR
	/// <summary>
	/// Dev-only heartbeat proof for the M1 boot chain: one log line per
	/// second of simulated time (30 ticks at the 30 Hz heartbeat), carrying
	/// tick index and sim time. If these lines stop while nothing is
	/// paused, the machine stopped — the symptom this marker exists to
	/// surface. Not a MonoBehaviour: one MonoScript per file is untouched.
	/// </summary>
	internal sealed class EngineProveSystem : ISimSystem
	{
		public SimTier Tier => SimTier.FullFrame;
		public SimPhase Phase => SimPhase.Housekeeping;

		private long lastLoggedTick = -1;

		public void Tick(in SimContext context)
		{
			if (lastLoggedTick >= 0 && context.TickIndex - lastLoggedTick < 30)
			{
				return;
			}

			lastLoggedTick = context.TickIndex;
			Debug.Log($"[EngineCore] tick {context.TickIndex}, simTime {context.SimTime:F2}s");
		}
	}
#endif
}
