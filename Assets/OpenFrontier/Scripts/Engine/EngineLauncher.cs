using OpenFrontier.Gamemodes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// The launch seam: GamemodeDefinition → payload → GALAXY stage →
	/// system boot. The port of legacy ScenarioUI.PlayScenario/
	/// ScenarioLoaderUI minus everything sector-specific. Deliberately
	/// small: validate, drop the load/pause protocol to 0 through the
	/// single authority, park the payload on a STATIC carrier (the pinned
	/// scene-boundary pattern — a Single-mode load destroys the menu
	/// scene, and only statics and persistent objects survive it), then
	/// load the GALAXY scene. The galaxy stage proves itself and hands
	/// over to the payload's TargetSceneName (M2b inserts the cubemap
	/// capture at that seam), whose Single-mode load unloads the galaxy
	/// and whose WorldBoot adopts the payload and lifts the protocol.
	///
	/// Every validation happens BEFORE the protocol drop: a launch that
	/// cannot happen must leave the world exactly as it was. A LoadScene
	/// for a scene not in the build profile logs an error and loads
	/// nothing — but a protocol dropped first would freeze the menu at
	/// timeScale 0 behind a load that never finishes (the failed first
	/// playtest: unlisted stub scene, menu died silently after the click).
	/// BOTH scenes are validated — the galaxy stage AND the final target.
	/// </summary>
	public static class EngineLauncher
	{
		private static LaunchPayload pending;

		/// <summary>A launch is queued but its scene has not adopted it yet.</summary>
		public static bool HasPending => pending != null;

		/// <summary>
		/// Read the pending payload WITHOUT consuming it — the galaxy stage
		/// needs the gamemode Id (dressing subtree) and the target scene
		/// name, but the payload itself belongs to the system scene's
		/// WorldBoot, whose ConsumePending is still the only consume.
		/// </summary>
		public static LaunchPayload PeekPending() => pending;

		/// <summary>
		/// The boot conductor's one consume: adopt-and-clear, so a payload
		/// can never be applied twice (the legacy static carrier was
		/// consumed exactly once for the same reason).
		/// </summary>
		public static LaunchPayload ConsumePending()
		{
			LaunchPayload payload = pending;
			pending = null;
			return payload;
		}

		/// <summary>
		/// Launch a gamemode. Errors loudly and does nothing on a
		/// definition that cannot name a scene (the legacy loader's empty
		/// TargetSceneName check, kept) or whose scene is not in the active
		/// build profile's scene list.
		/// </summary>
		public static void Launch(GamemodeDefinition gamemode)
		{
			if (gamemode == null)
			{
				Debug.LogError("[EngineLauncher] Launch called with no gamemode.");
				return;
			}

			if (string.IsNullOrEmpty(gamemode.TargetSceneName))
			{
				Debug.LogError($"[EngineLauncher] Gamemode '{gamemode.Id}' has no TargetSceneName - author it before launching.");
				return;
			}

			if (!IsSceneInBuildSettings(gamemode.TargetSceneName))
			{
				Debug.LogError($"[EngineLauncher] Gamemode '{gamemode.Id}' targets scene '{gamemode.TargetSceneName}', which is not in the active build profile's scene list (File > Build Profiles > Scene List). Launch refused - nothing changed.");
				return;
			}

			if (!IsSceneInBuildSettings(SceneNames.Galaxy))
			{
				Debug.LogError($"[EngineLauncher] The galaxy stage '{SceneNames.Galaxy}' is not in the active build profile's scene list (File > Build Profiles > Scene List) - every launch routes through it. Launch refused - nothing changed.");
				return;
			}

			// Defensive Boot: a menu opened directly in the editor never ran
			// the landing boot, and the engine must exist before its world
			// boots. Idempotent — the normal path is unaffected.
			EngineCore.Boot();

			// The load/pause protocol drops to 0 BEFORE the swap: the
			// machine stops natively behind the load, and ONLY WorldBoot
			// lifts it. Single authority — the legacy game had four
			// unrelated timeScale writers and relied on ordering luck.
			EngineCore.NotifyLoadStarted();

			pending = ComposePayload(gamemode);

			// Route through the galaxy stage: the Single-mode load destroys
			// the menu; the stage scans, proves itself and hands to
			// TargetSceneName (the pinned lifecycle — the stage exists only
			// to be photographed).
			SceneManager.LoadScene(SceneNames.Galaxy, LoadSceneMode.Single);
		}

		/// <summary>Build-profile scene-list check, by NAME (the
	/// serialized-string contract: definitions carry names, not paths).
	/// Runs once per launch — never a per-frame path. A name that matches
	/// no build-profile scene fails here loudly instead of freezing the
	/// world at timeScale 0.</summary>
	private static bool IsSceneInBuildSettings(string sceneName)
	{
		int count = SceneManager.sceneCountInBuildSettings;
		for (int i = 0; i < count; i++)
		{
			string path = SceneUtility.GetScenePathByBuildIndex(i);
			if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
			{
				return true;
			}
		}

		return false;
	}

	private static LaunchPayload ComposePayload(GamemodeDefinition gamemode)
		{
			var payload = new LaunchPayload
			{
				Gamemode = gamemode,
				TargetSceneName = gamemode.TargetSceneName,
			};

			payload.ResolvedSeed = payload.CustomSeed < 0
				? Random.Range(int.MinValue, int.MaxValue)
				: payload.CustomSeed;

			return payload;
		}
	}
}
