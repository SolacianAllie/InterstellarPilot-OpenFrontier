using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

		[Tooltip("Optional defacto default: the button focused when this page opens while the controller is the scheme actively in use (the player's last input was a gamepad's), or when a first controller input takes over an unlit page. Empty = the page's first enabled, interactable button in hierarchy order. Set explicitly to choose a different anchor; runtime-created rows cannot be referenced, so pick a static one (e.g. Back). Touch, mouse and keyboard use clear focus instead of lighting it.")]
		public Selectable FirstSelection;

		private CanvasGroup group;

		private void Awake()
		{
			group = GetComponent<CanvasGroup>();
		}

		private void OnEnable()
		{
			ActiveInput.SchemeChanged += OnSchemeChanged;
		}

		private void OnDisable()
		{
			ActiveInput.SchemeChanged -= OnSchemeChanged;
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
			SelectFirstIfIdle();

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

		private void Start()
		{
			// Entry screen only: boot straight into the menu, and the default
			// is already lit before the player's first input - when the
			// controller is the scheme in use.
			if (EntryScreen)
			{
				SelectFirstIfIdle();
			}
		}

		/// <summary>
		/// The controller taking over lights the current page's default right
		/// away: ANY controller input counts, not just navigation. Safe from
		/// accidental activation - Input System action callbacks (the module's
		/// Submit/Cancel) run before MonoBehaviour Update, so by the time this
		/// flip lands, the frame's Submit already resolved against an empty
		/// selection; a single A press lights the default without clicking it.
		/// </summary>
		private void OnSchemeChanged(InputScheme scheme)
		{
			if (scheme != InputScheme.Gamepad)
			{
				return;
			}

			SelectFirstIfIdle();
		}

		/// <summary>
		/// The defacto default: when the CONTROLLER is the scheme actively in
		/// use - the player's last observed input was a gamepad's, not merely
		/// that a gamepad is connected - an opening page selects its anchor
		/// (or first button) immediately, so a single stick push navigates
		/// from a known place. The sibling walk just before this in Show() may
		/// have cleared the selection (deactivated pages lose it), which is
		/// exactly the state this repairs - it must run AFTER that walk, never
		/// before. Every other scheme (and the boot-time Unknown) leaves pages
		/// unlit: touch, mouse and keyboard never auto-select.
		/// </summary>
		private void SelectFirstIfIdle()
		{
			EventSystem eventSystem = EventSystem.current;

			if (eventSystem == null || eventSystem.currentSelectedGameObject != null)
			{
				return;
			}

			if (ActiveInput.Scheme != InputScheme.Gamepad)
			{
				return;
			}

			Selectable target = FirstSelection != null ? FirstSelection : FindFirstSelectable();

			if (target != null)
			{
				eventSystem.SetSelectedGameObject(target.gameObject);
			}
		}

		private void Update()
		{
			// Establisher for held NAVIGATION input (stick/d-pad, arrows/WASD)
			// on an unlit page - the scheme-flip event (OnSchemeChanged)
			// covers every other first controller input. Runs only while this
			// page is active, and the selection gate comes first: when
			// anything is focused this is a single-null-test no-op, and on
			// devices without those controls the input check is two more.
			EventSystem eventSystem = EventSystem.current;

			if (eventSystem == null || eventSystem.currentSelectedGameObject != null)
			{
				return;
			}

			if (!NavigationInputHeld())
			{
				return;
			}

			Selectable target = FirstSelection != null ? FirstSelection : FindFirstSelectable();

			if (target != null)
			{
				eventSystem.SetSelectedGameObject(target.gameObject);
			}
		}

		/// <summary>
		/// Is a navigation control held right now: gamepad stick/d-pad, or
		/// keyboard arrows/WASD - the controls the UI module's Navigate action
		/// drives. WASD is included defensively; if the module's default asset
		/// ignores it, a stray press merely establishes the highlight. The
		/// stick uses a small deadzone so resting drift never wakes focus.
		/// </summary>
		private static bool NavigationInputHeld()
		{
			Gamepad gamepad = Gamepad.current;

			if (gamepad != null
				&& (gamepad.dpad.ReadValue().sqrMagnitude > 0.04f
					|| gamepad.leftStick.ReadValue().sqrMagnitude > 0.04f))
			{
				return true;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null)
			{
				return false;
			}

			return keyboard.upArrowKey.isPressed
				|| keyboard.downArrowKey.isPressed
				|| keyboard.leftArrowKey.isPressed
				|| keyboard.rightArrowKey.isPressed
				|| keyboard.wKey.isPressed
				|| keyboard.aKey.isPressed
				|| keyboard.sKey.isPressed
				|| keyboard.dKey.isPressed;
		}

		/// <summary>
		/// Auto-anchor when no FirstSelection was authored: the first active,
		/// interactable Selectable in the page's hierarchy order. Runs once per
		/// establishment - on open, on a scheme flip, or on a held-navigation
		/// frame - always behind the scheme/selection gates, so the single
		/// allocation of the child scan never touches a phone.
		/// </summary>
		private Selectable FindFirstSelectable()
		{
			Selectable[] candidates = GetComponentsInChildren<Selectable>(false);

			for (int i = 0; i < candidates.Length; i++)
			{
				Selectable candidate = candidates[i];

				if (candidate.isActiveAndEnabled && candidate.interactable)
				{
					return candidate;
				}
			}

			return null;
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
