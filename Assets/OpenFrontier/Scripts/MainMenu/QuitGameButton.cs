using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Quits the game when the button is clicked.
	///
	/// The smallest possible menu button script: Application.Quit cannot be
	/// called from a persistent scene listener without some component owning the
	/// method, so until the engine screens are restored and their MainMenuScreen
	/// quits in code (the original wires QuitButton.onClick in Awake), this is
	/// the whole mechanism. Attach to each exit button; it registers itself.
	///
	/// Leave the Button's On Click list empty. The click already gets a listener
	/// here - adding an On Click entry on top would call Quit twice per press,
	/// same shape as the nested-selectables double-fire trap.
	///
	/// Quitting in the editor's play mode looks like nothing happening (Unity
	/// does not stop play mode from Quit); verify on a built player or device.
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class QuitGameButton : MonoBehaviour
	{
		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(QuitGame);
		}

		private void QuitGame()
		{
			Application.Quit();
		}
	}
}
