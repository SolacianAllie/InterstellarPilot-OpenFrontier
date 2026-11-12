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
	/// The list is the single source of selection truth. Both indicators read
	/// it: the Gamemode Card mirrors the info, and the row itself mirrors the
	/// lit state - the row bound to the list's current choice holds the hover
	/// look (black label through SelectableTextColor, lit bar through
	/// SelectableImageColor, both via their code-owned SetChosen) until the
	/// choice moves. Rows keep no selection state of their own.
	///
	/// Like OpenScreenButton and QuitGameButton, the click listener is added
	/// here in Awake: leave the Button's On Click list EMPTY, or every press
	/// fires twice per press (the double-fire trap).
	///
	/// Transient tint still comes from the state machines on the prefab
	/// (authored, not code) - hover/press/focus behave exactly like any
	/// button; SetChosen only pins the persistent lit look on top.
	/// </summary>
	[RequireComponent(typeof(Button))]
	[DisallowMultipleComponent]
	public class GamemodeRow : MonoBehaviour
	{
		[Tooltip("Row label. Leave unassigned to auto-find the first TMP child of the row.")]
		public TMP_Text Title;

		private GamemodeList list;

		private GamemodeDefinition definition;

		private SelectableTextColor labelText;

		private SelectableImageColor backgroundImage;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(SelectSelf);

			if (Title == null)
			{
				Title = GetComponentInChildren<TMP_Text>();
			}

			// Both optional: a row prefab without the image twin tints label-only.
			labelText = GetComponent<SelectableTextColor>();
			backgroundImage = GetComponent<SelectableImageColor>();
		}

		private void OnEnable()
		{
			// Instantiate sets the parent before this runs, so the owning list
			// is findable here - earlier than Bind, which the list calls after.
			list = GetComponentInParent<GamemodeList>();

			if (list != null)
			{
				list.SelectedChanged += UpdateChosen;
			}

			// Pull before the push: a selection surviving a rebuild lights the
			// fresh rows with no event arriving.
			UpdateChosen();
		}

		private void OnDisable()
		{
			// Rows die on every rebuild; without this the destroyed row's
			// handler would still be invoked by the list's next change.
			if (list != null)
			{
				list.SelectedChanged -= UpdateChosen;
			}
		}

		/// <summary>Called by the list right after instantiation.</summary>
		public void Bind(GamemodeDefinition definition)
		{
			this.definition = definition;

			if (Title != null && definition != null)
			{
				Title.text = definition.Title;
			}

			UpdateChosen();
		}

		private void SelectSelf()
		{
			if (definition != null && list != null)
			{
				list.Select(definition);
			}
		}

		private void UpdateChosen()
		{
			// Reference compare is right: rows bind the very instances the
			// registry handed out, and Selected holds one of them.
			bool isChosen = definition != null && list != null && list.Selected == definition;

			if (labelText != null)
			{
				labelText.SetChosen(isChosen);
			}

			if (backgroundImage != null)
			{
				backgroundImage.SetChosen(isChosen);
			}
		}
	}
}
