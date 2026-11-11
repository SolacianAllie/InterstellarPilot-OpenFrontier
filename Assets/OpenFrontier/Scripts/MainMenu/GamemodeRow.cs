using OpenFrontier.Gamemodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// One row in the NewGame gamemode scroll: binds a single definition and
	/// reports clicks to the owning GamemodeList. Exists only as instances
	/// of the row prefab (Resources/Prefabs/UIX/) - the list instantiates it,
	/// never author it by hand in the scene.
	///
	/// The card is the selection indicator: a row's tap shows up as the
	/// Gamemode Card refreshing (title, description, image). Rows keep no
	/// selected state of their own - one source of truth, in the list.
	///
	/// Like OpenScreenButton and QuitGameButton, the click listener is added
	/// here in Awake: leave the Button's On Click list EMPTY, or every press
	/// fires twice per press (the double-fire trap).
	///
	/// State tint comes from SelectableTextColor on the prefab's label
	/// (authored, not code) - rows read Normal/Pressed/Highlighted like any
	/// button.
	/// </summary>
	[RequireComponent(typeof(Button))]
	[DisallowMultipleComponent]
	public class GamemodeRow : MonoBehaviour
	{
		[Tooltip("Row label. Leave unassigned to auto-find the first TMP child of the row.")]
		public TMP_Text Title;

		private GamemodeList list;

		private GamemodeDefinition definition;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(SelectSelf);

			if (Title == null)
			{
				Title = GetComponentInChildren<TMP_Text>();
			}
		}

		/// <summary>Called by the list right after instantiation.</summary>
		public void Bind(GamemodeDefinition definition)
		{
			this.definition = definition;
			list = GetComponentInParent<GamemodeList>();

			if (Title != null && definition != null)
			{
				Title.text = definition.Title;
			}
		}

		private void SelectSelf()
		{
			if (definition != null && list != null)
			{
				list.Select(definition);
			}
		}
	}
}
