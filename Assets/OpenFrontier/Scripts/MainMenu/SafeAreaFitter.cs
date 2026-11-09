using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Shrinks a Rect to the device safe area (notches, punch-holes, home
	/// indicator) by moving its anchors - no pixel positions, no cut-off UI, and
	/// the legacy AdjustSafeAreaScreen (a screen where the player nudged the UI
	/// by hand) never needs to exist.
	///
	/// Attach to ONE panel per Canvas - "SafeAreaRoot", full-stretch, sitting
	/// directly under the Canvas - and author UI inside it. Because anchors are
	/// relative, every child authored to the safe root reflows automatically on
	/// any aspect; nothing else in the tree needs to know.
	///
	/// Re-applies on any mid-play change - rotation, resize, and above all
	/// FOLDING a foldable, which swaps the active display (new resolution, new
	/// insets) while the menu is on screen. Screen.windowSizeChanged is internal
	/// to Unity in 6000.6 (the public event never existed to subscribe to), and
	/// OnRectTransformDimensionsChange only reaches the object whose own rect
	/// changed - the Canvas root, never this child - so the fitter polls. The
	/// poll is a 4-float compare per frame, no allocation: unmeasurable on the
	/// weakest target, and it keeps UI clamped the frame after a fold instead
	/// of ~250 ms late on a timed poll.
	///
	/// Screen.safeArea is simulated by Device Simulator in PLAY mode only - the
	/// fitter clamps nothing in the editor. In the simulator's EDIT view the
	/// yellow Safe Area overlay is drawn over the AUTHORED layout (anchors
	/// 0..1), so a fold device's hinge inset overlaps the UI there and looks
	/// broken - that is the authored state, not a defect; only fold devices
	/// show it because notch-less devices have zero insets either way. Verify
	/// the ladder in play (Device Simulator -> Folded, play mode): the
	/// editor-only log line below prints every applied clamp.
	///
	/// A failed apply never poisons the cache: appliedSafeArea is written
	/// only after the anchors land, so a callback that fires before OnEnable
	/// (play entered with domain reload disabled) retries next poll instead
	/// of silently skipping every clamp for the whole session.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class SafeAreaFitter : MonoBehaviour
	{
		private RectTransform rect;

		private Rect appliedSafeArea;

		private void OnEnable()
		{
			rect = GetComponent<RectTransform>();
			Apply();
		}

		private void OnRectTransformDimensionsChange()
		{
			// Unity magic callback: canvas rect changed - resize, rotate, fold
			// (and our own re-apply, which the guard swallows). Fires on the
			// Canvas root; kept as an early trigger, the poll below is the
			// mechanism that actually catches children.
			Apply();
		}

		private void Update()
		{
			// Fold/rotate/resize swap the active display mid-play. Children
			// never receive OnRectTransformDimensionsChange for that (the
			// canvas root does), so the safe area is polled. The compare is
			// 4 floats, no allocation - unmeasurable even at 120 Hz on the
			// weakest target, and UI stays clamped the frame after a fold.
			Apply();
		}

		private void Apply()
		{
			// Self-healing lookup: the Unity magic callbacks (enable, dimension
			// change with domain-reload disabled) can reach Apply BEFORE
			// OnEnable assigned this - a plain GetComponent here turns that
			// from an exception into a productive apply.
			if (rect == null)
			{
				rect = GetComponent<RectTransform>();
			}

			Rect safeArea = Screen.safeArea;

			// Fold transitions can surface a degenerate screen for a frame;
			// the divisions below would put Infinity in the anchors and eat
			// the UI. Bail WITHOUT caching - the next frame retries.
			if (Screen.width < 1 || Screen.height < 1
				|| safeArea.width < 1 || safeArea.height < 1)
			{
				return;
			}

			if (safeArea == appliedSafeArea)
			{
				return;
			}

			rect.anchorMin = new Vector2(
				safeArea.x / Screen.width,
				safeArea.y / Screen.height);
			rect.anchorMax = new Vector2(
				(safeArea.x + safeArea.width) / Screen.width,
				(safeArea.y + safeArea.height) / Screen.height);
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;

			// Cached ONLY after every write above succeeded. Caching first
			// was the 2026-11-09 fold bug: the callback-before-OnEnable apply
			// poisoned the cache and the equality guard then skipped every
			// later apply for the whole session - silently, no exception.
			appliedSafeArea = safeArea;

#if UNITY_EDITOR
			// Diagnostic only a developer reads - compiled out of builds.
			// Absent in a play session means Apply never fired; a full-rect
			// safe area means the sim device has no notch.
			Debug.Log($"[SafeAreaFitter] {name}: screen={Screen.width}x{Screen.height} "
				+ $"safe={safeArea} -> anchors {rect.anchorMin}..{rect.anchorMax}", this);
#endif
		}
	}
}
