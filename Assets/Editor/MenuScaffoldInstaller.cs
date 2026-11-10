using System.Collections.Generic;
using OpenFrontier.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// One-click installer for the fullscreen scaffold's auto-layout spine
	/// (docs/new-ui-guide.md, "The auto-layout spine").
	///
	///   SafeAreaRoot
	///    ├─ Scroll      ScrollRect (window; slides the stack)
	///    │   └─ Viewport   RectMask2D (frame; only shows what fits)
	///    │       └─ Content   VerticalLayoutGroup + ContentSizeFitter (stack)
	///    ├─ Background  full-stretch surface - NOT a row, stays beside the spine
	///    └─ Quit        chrome, stays beside the spine
	///
	/// v2 internals (v1 is dead): Loaded asset roots refuse transform
	/// manipulation in Unity 6000.6 ("Transform resides in a Prefab asset and
	/// cannot be set to prevent data corruption" - v1's SetParent calls), so
	/// all editing goes through PrefabUtility.LoadPrefabContents /
	/// SaveAsPrefabAsset: existing nodes keep their fileIDs, the heavily
	/// overridden SidebarMenu scene instance stays linked, and only the three
	/// new objects get fresh IDs. v1 also left a phantom `Scroll` branch
	/// floating in the open scene; the cleanup pass at the top removes any
	/// such stray so the second run washes the scene back to clean.
	///
	/// SafeAreaRoot children are OBSERVED, never moved - only the placeholder
	/// row the user drags into Content becomes a row.
	///
	/// Idempotent: a second run re-applies settings and reports what changed.
	/// Git is the undo. Run from the project or scene view, NOT while the
	/// prefab is open in prefab mode (that owns an editing session of its
	/// own). Delete this script once the spine is verified.
	///
	/// Editor-only by folder (Assets/Editor → Assembly-CSharp-Editor).
	/// </summary>
	public static class MenuScaffoldInstaller
	{
		// Design values - tweak and re-run; every run re-applies them. Once
		// verified, the installer is deleted and the number live only in the
		// prefab.
		const float RowSpacing = 8f;
		const int PaddingLeft = 24;
		const int PaddingRight = 24;
		const int PaddingTop = 16;
		const int PaddingBottom = 16;

		const string ScaffoldPath = "Assets/OpenFrontier/Resources/Prefabs/UIX/Fullscreen Menu.prefab";

		[MenuItem("Open Frontier/UIX/Install Fullscreen Scaffold Spine")]
		public static void Install()
		{
			if (AssetDatabase.LoadAssetAtPath<GameObject>(ScaffoldPath) == null)
			{
				Debug.LogError($"[ScaffoldInstaller] No prefab at '{ScaffoldPath}' - moved or renamed? Stopping.");
				return;
			}

			int strays = CleanupSceneStrays();

			GameObject contents = PrefabUtility.LoadPrefabContents(ScaffoldPath);
			string summary;

			try
			{
				summary = EditContents(contents);
				PrefabUtility.SaveAsPrefabAsset(contents, ScaffoldPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}

			// Post-check against what actually landed on disk, not what the
			// editor intended - v1 promised without verifying.
			GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(ScaffoldPath);
			string error = PostCheck(saved);

			if (error != null)
			{
				Debug.LogError($"[ScaffoldInstaller] Saved, but post-check FAILED: {error}");
				return;
			}

			Debug.Log($"[ScaffoldInstaller] Done.\n{summary}\n  scene strays removed: {strays}");
			EditorGUIUtility.PingObject(saved);
		}

		/// <summary>
		/// All editing happens on the loaded prefab contents (freely transform-
		/// manipulable). Returns the summary the caller logs. Existing nodes
		/// keep their fileIDs through this round-trip, so the scene instance's
		/// override table survives unmapped-intact.
		/// </summary>
		static string EditContents(GameObject root)
		{
			Transform safeAreaRoot = FindByName(root.transform, "SafeAreaRoot");

			if (safeAreaRoot == null)
			{
				return null; // caller's finally unloads; PostCheck reports the go/missing
			}

			// --- identity: every instance born marked and fade-ready ---
			MenuScreen screen = root.GetComponent<MenuScreen>();

			if (screen == null)
			{
				screen = root.AddComponent<MenuScreen>();
				screen.EntryScreen = false; // entry status is a per-instance tick
			}

			if (root.GetComponent<CanvasGroup>() == null)
			{
				root.AddComponent<CanvasGroup>();
			}

			// --- the spine: window > frame > stack ---
			RectTransform scroll = EnsureRect(safeAreaRoot, "Scroll", out bool scrollCreated);
			ScrollRect scrollRect = OrAdd<ScrollRect>(scroll.gameObject);
			RectTransform viewport = EnsureRect(scroll, "Viewport", out bool viewportCreated);
			RectTransform content = EnsureRect(viewport, "Content", out bool contentCreated);

			// The window fills the safe area entirely; nothing else pushes it.
			Stretch(scroll);

			// The frame clips by culling, no stencil material variants - that is
			// why RectMask2D beats Mask here: cheaper on phones, and it skips the
			// stencil-variant material relay the saturation shader needs under Mask.
			if (viewport.GetComponent<RectMask2D>() == null)
			{
				viewport.gameObject.AddComponent<RectMask2D>();
			}

			Stretch(viewport);

			// The stack: width follows the viewport (anchor stretch), height is
			// the sum of its rows - the pairing that avoids layout-loop warnings.
			content.anchorMin = new Vector2(0f, 1f);
			content.anchorMax = new Vector2(1f, 1f);
			content.pivot = new Vector2(0.5f, 1f);
			content.anchoredPosition = Vector2.zero;

			VerticalLayoutGroup stack = OrAdd<VerticalLayoutGroup>(content.gameObject);
			stack.padding = new RectOffset(PaddingLeft, PaddingRight, PaddingTop, PaddingBottom);
			stack.spacing = RowSpacing;
			stack.childAlignment = TextAnchor.UpperCenter;
			stack.childControlWidth = true; // width: rows fill the stack
			stack.childControlHeight = false; // height: each row keeps its own
			stack.childForceExpandWidth = true;
			stack.childForceExpandHeight = false;

			ContentSizeFitter fit = OrAdd<ContentSizeFitter>(content.gameObject);
			fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // width from the anchors
			fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // height from the rows

			// The window's wiring. No scrollbars on purpose: mobile menus do not
			// show them; touch drag / mouse wheel slide without one.
			scrollRect.viewport = viewport;
			scrollRect.content = content;
			scrollRect.horizontal = false;
			scrollRect.vertical = true;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.inertia = true;

			// Report, don't move: SafeAreaRoot's authored children (Background,
			// Quit) stay exactly where the user put them. Only a row dropped
			// into Content by hand becomes part of the stack.
			List<string> observed = new List<string>();

			for (int i = 0; i < safeAreaRoot.childCount; i++)
			{
				observed.Add(safeAreaRoot.GetChild(i).name);
			}

			return
				"  root: MenuScreen present, CanvasGroup present\n" +
				$"  spine under SafeAreaRoot: Scroll{(scrollCreated ? "" : " (re-applied)")} -> " +
				$"Viewport{(viewportCreated ? "" : " (re-applied)")} -> " +
				$"Content{(contentCreated ? "" : " (re-applied)")}\n" +
				"  components: ScrollRect(clamped) + RectMask2D + VerticalLayoutGroup + ContentSizeFitter\n" +
				$"  spacing {RowSpacing}, padding {PaddingTop}/{PaddingRight}/{PaddingBottom}/{PaddingLeft} (T/R/B/L)\n" +
				$"  SafeAreaRoot children left in place: {string.Join(", ", observed)}";
		}

		/// <summary>
		/// Reads the saved asset back and confirms the spine actually exists
		/// there - the gate between "the editor meant it" and "the file has it".
		/// </summary>
		static string PostCheck(GameObject saved)
		{
			if (saved == null)
			{
				return "prefab could not be reloaded";
			}

			Transform safeAreaRoot = FindByName(saved.transform, "SafeAreaRoot");

			if (safeAreaRoot == null)
			{
				return "SafeAreaRoot missing";
			}

			Transform scroll = safeAreaRoot.Find("Scroll");

			if (scroll == null || scroll.GetComponent<ScrollRect>() == null)
			{
				return "Scroll/ScrollRect missing";
			}

			Transform viewport = scroll.Find("Viewport");

			if (viewport == null || viewport.GetComponent<RectMask2D>() == null)
			{
				return "Viewport/RectMask2D missing";
			}

			Transform content = viewport.Find("Content");

			if (content == null || content.GetComponent<VerticalLayoutGroup>() == null ||
				content.GetComponent<ContentSizeFitter>() == null)
			{
				return "Content layout components missing";
			}

			if (saved.GetComponent<MenuScreen>() == null || saved.GetComponent<CanvasGroup>() == null)
			{
				return "root identity components missing";
			}

			return null;
		}

		/// <summary>
		/// v1's failure left a dangling `Scroll` branch (scene object, not part
		/// of the asset). This pass removes any scene-root Scroll/Viewport/
		/// Content that carries a ScrollRect and is NOT a prefab instance -
		/// the scaffolding's own children keep those names INSIDE prefabs, and
		/// the destroyed ones match the exact shape this script created.
		/// </summary>
		static int CleanupSceneStrays()
		{
			// Prefab contents are being edited on the SIDE; scene roots only.
			Scene active = SceneManager.GetActiveScene();
			int removed = 0;

			foreach (GameObject go in active.GetRootGameObjects())
			{
				// shape gate first: a scene-root Foo built from this script
				// must hold the ScrollRect + complete spine children.
				if (go.name != "Scroll" || go.GetComponent<ScrollRect>() == null ||
					go.transform.Find("Viewport/Content") == null ||
					PrefabUtility.IsPartOfPrefabInstance(go))
				{
					continue;
				}

				Object.DestroyImmediate(go);
				removed++;
			}

			return removed;
		}

		/// <summary>
		/// Gets an existing child RectTransform by name, or creates one.
		/// `created` lets the report distinguish first-run from re-run.
		/// </summary>
		static RectTransform EnsureRect(Transform parent, string name, out bool created)
		{
			Transform found = parent.Find(name);

			if (found != null)
			{
				created = false;
				return (RectTransform)found;
			}

			GameObject go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			created = true;
			return (RectTransform)go.transform;
		}

		/// <summary>
		/// Full-stretch anchors with zero offsets: the rect IS its parent's rect.
		/// LocalScale pinned to 1 because that is a classic silent drift spot.
		/// </summary>
		static void Stretch(RectTransform rt)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			rt.localScale = Vector3.one;
		}

		static T OrAdd<T>(GameObject go) where T : Component
		{
			T component = go.GetComponent<T>();
			return component != null ? component : go.AddComponent<T>();
		}

		/// <summary>
		/// Depth-first name lookup; a direct Find only searches its own children.
		/// First match wins and the log names what it acted on, so a collided
		/// name is visible rather than silently wrong.
		/// </summary>
		static Transform FindByName(Transform node, string name)
		{
			if (node.name == name)
			{
				return node;
			}

			Transform direct = node.Find(name);

			if (direct != null)
			{
				return direct;
			}

			for (int i = 0; i < node.childCount; i++)
			{
				Transform found = FindByName(node.GetChild(i), name);

				if (found != null)
				{
					return found;
				}
			}

			return null;
		}
	}
}
