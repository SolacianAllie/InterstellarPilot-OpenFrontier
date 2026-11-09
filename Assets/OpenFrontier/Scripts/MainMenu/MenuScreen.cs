using System.Collections;
using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Marks the root of a menu screen and owns its open state.
	///
	/// The recipe for a new screen: author its prefab from the shared blocks,
	/// place an instance in the menu scene under a "Screens" organizer, add this
	/// component to the screen's root, deactivate it, and leave the screen play
	/// should start on VISIBLE when the scene is saved (that visible state is
	/// the entry screen). Buttons switch screens via <see cref="OpenScreenButton"/>;
	/// there is no navigator object, screen list, or registry to fill in.
	///
	/// Show() also enforces "one screen visible" among the direct children of
	/// this object's parent: any other sibling that carries a MenuScreen is
	/// deactivated once this one activates. That covers the common case of
	/// forgetting a subscreen when saving the scene. Enforcement is sibling-scoped
	/// by design - screens kept on separate canvases still close through their
	/// OpenScreenButton's own-screen Hide, just not through this walk.
	///
	/// Fade: set FadeDuration above zero and keep a CanvasGroup on the same
	/// object; transitions fade in unscaled time. Without a CanvasGroup the
	/// screen snaps on, which is fine. The coroutine only lives for the fade,
	/// so there is no per-frame cost while a screen rests.
	/// </summary>
	[DisallowMultipleComponent]
	public class MenuScreen : MonoBehaviour
	{
		[Tooltip("Checked: at play start every sibling menu screen is closed so this one is the only one active. Mark the screen the scene opens on; the scene's saved visible state supplies the entry screen itself.")]
		public bool EntryScreen;

		[Tooltip("Fade-in duration when shown, in seconds, unscaled time. 0 snaps on. Needs a CanvasGroup on this object; without one the screen always snaps on.")]
		[Min(0f)]
		public float FadeDuration = 0.15f;

		private CanvasGroup group;

		private void Awake()
		{
			group = GetComponent<CanvasGroup>();
		}

		/// <summary>
		/// Opens the screen and deactivates every sibling that also carries a
		/// MenuScreen. Called by OpenScreenButton; public so future wiring (spawn
		/// navigators, launch-origin redirects) can use it unchanged.
		/// </summary>
		public void Show()
		{
			gameObject.SetActive(true);
			DeactivateSiblingScreens();
			StopAllCoroutines();

			if (group != null)
			{
				if (FadeDuration > 0f)
				{
					StartCoroutine(FadeIn());
				}
				else
				{
					group.alpha = 1f;
				}
			}
		}

		/// <summary>
		/// Closes the screen. Cheap and total: an inactive GameObject's canvases
		/// build nothing and every child script inside sleeps.
		/// </summary>
		public void Hide()
		{
			gameObject.SetActive(false);
		}

		private void DeactivateSiblingScreens()
		{
			Transform parent = transform.parent;

			if (parent == null)
			{
				return;
			}

			int count = parent.childCount;

			for (int i = 0; i < count; i++)
			{
				Transform sibling = parent.GetChild(i);

				if (sibling == transform)
				{
					continue;
				}

				if (sibling.GetComponent<MenuScreen>() != null)
				{
					sibling.gameObject.SetActive(false);
				}
			}
		}

		private IEnumerator FadeIn()
		{
			float duration = FadeDuration;

			for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
			{
				group.alpha = elapsed / duration;
				yield return null;
			}

			group.alpha = 1f;
		}
	}
}
