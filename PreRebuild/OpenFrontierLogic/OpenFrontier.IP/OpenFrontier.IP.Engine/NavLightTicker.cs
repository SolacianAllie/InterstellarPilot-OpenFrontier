using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	// Shared gate driver for every NavLight in play. One MonoBehaviour whose
	// Update runs ONCE per frame, polls on the 2s cadence (the same rhythm as
	// ActiveUnit.UpdateVisibility and FlareScaler's distance check), and asks
	// each chip one thing: is your hull still visible enough to burn for?
	//
	// Why a self-spawning static and not a per-chip Update: a sector can hold
	// a few hundred chips, and AGENTS.md's first law is that per-frame work is
	// a per-frame tax on a phone. N Update no-ops per frame for a boolean that
	// only changes every 2s is exactly the shape of the thing the project
	// keeps deleting. One ticker call beats N editors' Update calls at any N.
	//
	// Registration is pooling-safe: OnEnable registers, OnDisable
	// unregisters (destroy runs OnDisable first, so scene teardown and
	// pool recycling clean themselves up).
	public sealed class NavLightTicker : MonoBehaviour
	{
		private const float PollInterval = 2f;

		private static NavLightTicker instance;

		// Backwards iteration in Update because a chip's OnDisable may fire
		// mid-poll (unit destroyed between frames) and the removal must not
		// reorder the visitors the frame is walking.
		private static readonly List<NavLight> lights = new List<NavLight>();

		private float nextPoll;

		public static void Register(NavLight navLight)
		{
			if (navLight == null)
			{
				return;
			}
			if (instance == null)
			{
				GameObject host = new GameObject("NavLightTicker");
				// Persists across sector loads: the ticker owns no scene
				// state, only the poll clock.
				Object.DontDestroyOnLoad(host);
				instance = host.AddComponent<NavLightTicker>();
			}
			if (!lights.Contains(navLight))
			{
				lights.Add(navLight);
			}
		}

		public static void Unregister(NavLight navLight)
		{
			lights.Remove(navLight);
		}

		// Domain reload can be OFF (Project Settings Enter Play Mode
		// options); statics then outlive a play session holding destroyed
		// chips. Rebuild from nothing on the next play's start and let every
		// chip's OnEnable re-register.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticsForNoDomainReload()
		{
			lights.Clear();
			instance = null;
		}

		private void Update()
		{
			// The delay uses unscaled realtime: behind a pause screen
			// (timeScale 0) the gate state moves as little as the sim it
			// mirrors does, and startup frames before EngineASX loads keep
			// the cadence steady.
			float now = Time.realtimeSinceStartup;
			if (now < nextPoll)
			{
				return;
			}
			nextPoll = now + PollInterval;

			for (int i = lights.Count - 1; i >= 0; i--)
			{
				NavLight navLight = lights[i];
				if (navLight == null)
				{
					// Destroyed without a registered OnDisable (object was
					// disabled at destroy time or torn down mid-poll) - prune
					// here rather than holding a seat forever.
					lights.RemoveAt(i);
					continue;
				}
				navLight.PollGate();
			}
		}
	}
}
