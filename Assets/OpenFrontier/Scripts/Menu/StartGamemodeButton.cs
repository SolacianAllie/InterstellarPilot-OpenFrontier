using OpenFrontier.Gamemodes;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// The launch seam: on click, hands the selected definition to whatever
	/// boots a session. Today that is a logged stub - the engine's world-boot
	/// is not ported yet, and the dev gamemode's destination is the recorded
	/// open question in docs/gamemode-modding-design.md. When it exists, this
	/// is the port of legacy ScenarioUI.PlayScenario: compose the launch data
	/// from the definition and hand it to the scenario loader.
	///
	/// The definition travels, not the row or the asset name - everything
	/// downstream keys off Id.
	///
	/// Keeps itself unclickable while nothing is selected, re-evaluated on
	/// enable and on every selection change - the same pull+push pattern as
	/// GamemodeCard, so component enable order is irrelevant. The disabled
	/// state also tints the label through SelectableTextColor, whose Update
	/// poll exists precisely to catch script-driven interactable flips.
	///
	/// Like every menu button, the click listener is added here: leave the
	/// On Click list EMPTY.
	/// </summary>
	[RequireComponent(typeof(Button))]
	[DisallowMultipleComponent]
	public class StartGamemodeButton : MonoBehaviour
	{
		[Tooltip("The NewGame list - drag the Content object carrying GamemodeList.")]
		public GamemodeList List;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(StartSelected);
		}

		private void OnEnable()
		{
			if (List != null)
			{
				List.SelectedChanged += Refresh;
			}

			Refresh();
		}

		private void OnDisable()
		{
			if (List != null)
			{
				List.SelectedChanged -= Refresh;
			}
		}

		private void Refresh()
		{
			GetComponent<Button>().interactable = List != null && List.Selected != null;
		}

		private void StartSelected()
		{
			GamemodeDefinition selected = List != null ? List.Selected : null;

			if (selected == null)
			{
				return;
			}

			Debug.Log($"[StartGamemodeButton] Launch requested: '{selected.Id}' (stub - engine world-boot not ported yet).");
		}
	}
}
