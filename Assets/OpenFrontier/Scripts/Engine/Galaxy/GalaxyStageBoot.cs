using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// The galaxy stage's boot conductor — the Galaxy scene carries one on
	/// the "Galaxy" root. The stage exists ONLY to be photographed (the
	/// pinned lifecycle): this boot scans the scene contract, activates the
	/// launched gamemode's dressing subtree, proves the stage by log, and
	/// hands over to the payload's TargetSceneName — whose Single-mode load
	/// unloads the galaxy. In M2a the hand-over is a pass-through; the M2b
	/// capture rig slots into exactly this position (scan → CAPTURE →
	/// hand-over), so the seam this file fixes is the one the rig lands in.
	///
	/// Runs at timeScale 0 (the launcher dropped the protocol before the
	/// galaxy loaded) — correct by design: Start and coroutines run while
	/// the sim machine is stopped, and only the system world's WorldBoot
	/// lifts the protocol back to 1. Nothing here touches timeScale.
	/// </summary>
	public class GalaxyStageBoot : MonoBehaviour
	{
		[Tooltip("The 'systems' container — SystemAnchor objects, hand-authored and runtime-created.")]
		public Transform SystemsRoot;

		[Tooltip("The 'modes' container — per-gamemode dressing subtrees named by gamemode Id (modes/<gamemode-id>/).")]
		public Transform ModesRoot;

		[Tooltip("The legal-placement volume generated systems are forced inside.")]
		public GenerationTrigger Trigger;

		[Tooltip("The capture eye. Wired now so the M2b rig lands against a real reference.")]
		public Camera GalaxyCamera;

		private void Start()
		{
			// Defensive: the galaxy scene may be opened directly in the
			// editor (no landing boot, no launch). Boot is idempotent.
			EngineCore core = EngineCore.Boot();

			// Peek, do not consume: the payload belongs to the SYSTEM
			// scene's WorldBoot — the galaxy stage reads what it needs and
			// passes the carrier through untouched.
			LaunchPayload payload = EngineLauncher.PeekPending();

			if (!ValidateStructure(payload))
			{
				// Validation already logged the specifics. Stay in the
				// galaxy on a failed boot — loading onward would bury the
				// broken stage under a world that pretends it was fine
				// (the legacy try/finally-ready-world trap, not repeated).
				return;
			}

			ActivateModeSubtree(payload);

			if (payload == null)
			{
				Debug.Log("[GalaxyStageBoot] Bare boot (galaxy opened directly) - stage scanned, staying in the galaxy.");
				return;
			}

			// Hand over: the Single-mode load unloads the galaxy (the stage
			// exists only to be photographed). M2b inserts the cubemap
			// capture between this line and the load.
			Debug.Log($"[GalaxyStageBoot] Stage ready - handing over to '{payload.TargetSceneName}'.");
			SceneManager.LoadScene(payload.TargetSceneName, LoadSceneMode.Single);
		}

		private bool ValidateStructure(LaunchPayload payload)
		{
			bool ok = true;

			int anchorCount = SystemsRoot != null
				? SystemsRoot.GetComponentsInChildren<SystemAnchor>(true).Length
				: 0;
			if (SystemsRoot == null)
			{
				Debug.LogError("[GalaxyStageBoot] SystemsRoot is not wired - the stage cannot find system anchors.");
				ok = false;
			}
			else
			{
				int available = CountAnchorsAvailableTo(payload);
				Debug.Log($"[GalaxyStageBoot] Anchors: {anchorCount} under 'systems/', {available} available to '{(payload?.Gamemode != null ? payload.Gamemode.Id : "<bare>")}'.");
			}

			if (Trigger == null)
			{
				Debug.LogError("[GalaxyStageBoot] Trigger is not wired - generated systems would have no legal region.");
				ok = false;
			}
			else if (!Trigger.HasLegalRegion)
			{
				// The trigger's own RefreshLegalShapes logged the empty
				// region; a boot-stage echo keeps the failure next to the
				// chain proof instead of only at the component.
				Debug.LogError($"[GalaxyStageBoot] Trigger '{Trigger.name}' has no legal region - generation would refuse.", Trigger);
				ok = false;
			}

			if (GalaxyCamera == null)
			{
				Debug.LogError("[GalaxyStageBoot] GalaxyCamera is not wired - the M2b capture rig has no eye.");
				ok = false;
			}

			return ok;
		}

		private int CountAnchorsAvailableTo(LaunchPayload payload)
		{
			if (SystemsRoot == null)
			{
				return 0;
			}

			string gamemodeId = payload?.Gamemode != null ? payload.Gamemode.Id : null;
			SystemAnchor[] anchors = SystemsRoot.GetComponentsInChildren<SystemAnchor>(true);
			int count = 0;
			foreach (SystemAnchor anchor in anchors)
			{
				if (anchor.IsAvailableTo(gamemodeId))
				{
					count++;
				}
			}

			return count;
		}

		/// <summary>The data-overlay activation: exactly the launched
		/// gamemode's dressing subtree goes live, sibling subtrees sleep,
		/// and the universal "hand" content (outside this container) was
		/// never gated. A gamemode with no subtree gets no dressing — a new
		/// gamemode is a new subfolder, no code.</summary>
		private void ActivateModeSubtree(LaunchPayload payload)
		{
			if (ModesRoot == null)
			{
				Debug.LogWarning("[GalaxyStageBoot] ModesRoot is not wired - no per-gamemode dressing can activate (universal content is unaffected).");
				return;
			}

			string gamemodeId = payload?.Gamemode != null ? payload.Gamemode.Id : null;
			if (gamemodeId == null)
			{
				return; // Bare boot: leave every subtree as authored.
			}

			foreach (Transform child in ModesRoot)
			{
				bool active = child.name == gamemodeId;
				if (child.gameObject.activeSelf != active)
				{
					child.gameObject.SetActive(active);
				}
			}

			Transform mine = ModesRoot.Find(gamemodeId);
			Debug.Log(mine != null
				? $"[GalaxyStageBoot] Mode subtree 'modes/{gamemodeId}/' activated ({mine.childCount} direct child(ren))."
				: $"[GalaxyStageBoot] No 'modes/{gamemodeId}/' subtree - the gamemode carries no galaxy dressing.");
		}
	}
}
