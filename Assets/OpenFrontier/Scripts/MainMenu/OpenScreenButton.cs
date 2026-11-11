using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Switches to another menu screen when clicked: shows the Target screen
	/// and hides the screen this button lives on. Setup is one drag - point
	/// Target at the other screen's root (the object carrying the
	/// <see cref="MenuScreen"/> marker) and the button is wired.
	///
	/// Like QuitGameButton, this listens to the Button's own onClick from Awake.
	/// Leave the Button's On Click list EMPTY: the listener is added here, and
	/// a manual On Click entry on top would fire the switch twice per press
	/// (same shape as the nested-selectables double-fire trap).
	///
	/// The button's own screen is found by walking up to the NEAREST ancestor
	/// with a MenuScreen marker, so a back button buried a few layers inside a
	/// screen still closes that screen whatever the hierarchy depth. A button
	/// placed on a screen-free branch (organizer object, floating panel) simply
	/// has no screen to hide; it then only shows the target, which is exactly
	/// what a bootstrap-style button wants.
	///
	/// One behaviour per Button, like QuitGameButton: a button that quits gets
	/// QuitGameButton, a button that opens a screen gets this. Both attached to
	/// the same Button would fire both actions on one press.
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class OpenScreenButton : MonoBehaviour
	{
		[Tooltip("MenuScreen shown when clicked: drag the target screen's root object here.")]
		public MenuScreen Target;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(OpenTarget);
		}

		private void OpenTarget()
		{
			if (Target == null)
			{
				return;
			}

			// Hide FIRST: deactivating the own page clears the EventSystem
			// selection, so the target page always opens idle - nothing is
			// highlighted until the first navigation input establishes focus
			// (see MenuScreen.FirstSelection). DeactivateSiblingScreens covers
			// same-parent siblings; solo-spawned screens on other canvases
			// need this own-screen Hide.
			FindOwnScreen()?.Hide();
			Target.Show();
		}

		/// <summary>
		/// Nearest ancestor carrying a MenuScreen marker, or null when the button
		/// sits outside any screen. Tiny transform walk, click-frequency only.
		/// </summary>
		private MenuScreen FindOwnScreen()
		{
			Transform node = transform.parent;

			while (node != null)
			{
				MenuScreen screen = node.GetComponent<MenuScreen>();

				if (screen != null)
				{
					return screen;
				}

				node = node.parent;
			}

			return null;
		}
	}
}
