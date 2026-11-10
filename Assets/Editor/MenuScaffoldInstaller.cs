using System.Collections.Generic;
using OpenFrontier.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// One-click installer for the fullscreen scaffold's auto-layout spine
	/// (docs/new-ui-guide.md, "The auto-layout spine").
	///
	/// Builds inside the `Fullscreen Menu` prefab asset:
	///
	///   SafeAreaRoot
	///    └─ Scroll      ScrollRect (window; slides the stack)
	///        └─ Viewport   RectMask2D (frame; only shows what fits)
	///            └─ Content   VerticalLayoutGroup + ContentSizeFitter (stack)
	///                └─ (SafeAreaRoot's authored children move in here)
	///
	/// The placeholder `Text (TMP)` currently under SafeAreaRoot is re-homed
	/// under Content; rows dropped into Content from then on stack themselves.
	///
	/// Also bakes the two identity parts every screen needs but the scaffold
	/// lacked: MenuScreen on the root (so no screen instance needs the "mark
	/// it" step) and a CanvasGroup so FadeDuration has something to drive.
	///
	/// Idempotent: running it twice re-applies the settings and reports what
	/// changed; nothing is duplicated. Git is the undo - the prefab diff lands
	/// on Dev like any other save. Run this from the project or scene view,
	/// NOT while the prefab is open in prefab mode (this writes the asset file
	/// an open prefab-mode session could then clobber). Delete this script
	/// once the spine is verified - it is one-time tooling.
	///
	/// Editor-only by folder (Assets/Editor → Assembly-CSharp-Editor).
	/// </summary>
	public static class MenuScaffoldInstaller
	{
		// Design values - tweak and re-run; every run re-applies them, so a
		// hand-tuned value in the inspector gets reset by a later run. Once the
		// spine is in and verified, the installer is deleted and the numbers
		// live only in the prefab.
		const float RowSpacing = 8f;
		const int PaddingLeft = 24;
		const int PaddingRight = 24;
		const int PaddingTop = 16;
		const int PaddingBottom = 16;

		const string ScaffoldPath = "Assets/OpenFrontier/Resources/Prefabs/UIX/Fullscreen Menu.prefab";

		[MenuItem("Open Frontier/UIX/Install Fullscreen Scaffold Spine")]
		public static void Install()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(ScaffoldPath);

			if (root == null)
			{
				Debug.LogError($"[ScaffoldInstaller] No prefab at '{ScaffoldPath}' - moved or renamed? Stopping.");
				return;
			}

			if (root.GetComponent<RectTransform>() == null)
			{
				Debug.LogError("[ScaffoldInstaller] Prefab root has no RectTransform - is this the right asset? Stopping.");
				return;
			}

			Transform safeAreaRoot = FindByName(root.transform, "SafeAreaRoot");

			if (safeAreaRoot == null)
			{
				Debug.LogError("[ScaffoldInstaller] No 'SafeAreaRoot' child found - was it renamed? Stopping.");
				return;
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
			// why RectMask2D beats Mask here: cheaper on phones and it skips the
			// stencil-variant material relay the saturation shader needs under Mask.
			if (viewport.GetComponent<RectMask2D>() == null)
			{
				viewport.gameObject.AddComponent<RectMask2D>();
			}

			Stretch(viewport);

			// The stack: width follows the viewport (anchor stretch), height is
			// the sum of its rows. That pairing also avoids layout-loop warnings.
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
			fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // width comes from the anchors
			fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // height is driven by the rows

			// The window's wiring. No scrollbars on purpose: mobile menus do not
			// show them, and touch drag / mouse wheel slide without one.
			scrollRect.viewport = viewport;
			scrollRect.content = content;
			scrollRect.horizontal = false;
			scrollRect.vertical = true;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.inertia = true;

			// Re-home SafeAreaRoot's authored children (today: the placeholder
			// 'Text (TMP)') into the stack, layout-owned from now on.
			List<Transform> existing = new List<Transform>();

			for (int i = 0; i < safeAreaRoot.childCount; i++)
			{
				existing.Add(safeAreaRoot.GetChild(i));
			}

			List<string> rehomed = new List<string>();

			for (int i = 0; i < existing.Count; i++)
			{
				if (existing[i] == (Transform)scroll)
				{
					continue;
				}

				existing[i].SetParent(content, false);
				rehomed.Add(existing[i].name);
			}

			EditorUtility.SetDirty(root);
			AssetDatabase.SaveAssets();

			Debug.Log(
				"[ScaffoldInstaller] Done.\n" +
				$"  root: MenuScreen {(screen != null ? "baked" : "present")}, CanvasGroup {(root.GetComponent<CanvasGroup>() != null ? "baked" : "present")}\n" +
				$"  spine: Scroll{(scrollCreated ? " created" : " re-applied")}, Viewport{(viewportCreated ? " created" : " re-applied")}, Content{(contentCreated ? " created" : " re-applied")}\n" +
				$"  spacing {RowSpacing}, padding {PaddingTop}/{PaddingRight}/{PaddingBottom}/{PaddingLeft} (T/R/B/L)\n" +
				$"  re-homed under Content: {(rehomed.Count > 0 ? string.Join(", ", rehomed) : "(none - Stack already owns them)")}");

			EditorGUIUtility.PingObject(root);
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
