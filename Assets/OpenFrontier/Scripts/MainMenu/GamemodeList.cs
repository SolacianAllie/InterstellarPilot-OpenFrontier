using System;
using System.Collections.Generic;
using OpenFrontier.Gamemodes;
using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Populates the NewGame scroll with one row per visible registry
	/// definition and owns the selection. Sits on the Scroll View's Content;
	/// the row prefab instantiates as its children and the scaffold's
	/// VerticalLayoutGroup + ContentSizeFitter stack them - no layout code.
	///
	/// Rebuilds on every enable (docs/gamemode-modding-design.md): the screen
	/// sleeps while hidden, so a mod toggled in Game Extras is picked up the
	/// next time this opens. The cost is a handful of instantiations per
	/// open - nothing runs per-frame, and pooling only earns itself when a
	/// list reaches hundreds of rows (save slots, not this one).
	///
	/// Selection survives a rebuild when the definition's Id is still
	/// registered (compared by Id, not reference - a re-registered mod
	/// returns NEW asset instances) and falls back to the first row when it
	/// is not, so a deleted mod can never leave a phantom selection.
	///
	/// Consumers (GamemodeCard, StartGamemodeButton) both subscribe to
	/// SelectedChanged AND pull Selected in their own OnEnable, which makes
	/// the nondeterministic component enable order irrelevant: the push
	/// covers consumer-enables-first, the pull covers list-enables-first.
	/// </summary>
	[DisallowMultipleComponent]
	public class GamemodeList : MonoBehaviour
	{
		[Tooltip("Row prefab instantiated under this Content, one per visible registry definition.")]
		public GamemodeRow RowPrefab;

		/// <summary>The selected definition, or null before any selection / with nothing registered.</summary>
		public GamemodeDefinition Selected { get; private set; }

		/// <summary>Raised whenever Selected changes, including the preselect on open.</summary>
		public event Action SelectedChanged;

		private void OnEnable()
		{
			Rebuild();
		}

		/// <summary>
		/// Selects a definition. Called by the rows; public so later wiring
		/// (keyboard/gamepad focus, a mod manager's preview) can use it
		/// unchanged. Re-selecting the current definition is a no-op.
		/// </summary>
		public void Select(GamemodeDefinition definition)
		{
			if (definition == null || Selected == definition)
			{
				return;
			}

			Selected = definition;
			RaiseSelectedChanged();
		}

		private void Rebuild()
		{
			// Retire previous rows first. SetActive(false) NOW plus deferred
			// Destroy keeps them out of this open's layout: destroyed-at-
			// frame-end children would otherwise stack under the new rows for
			// one visible frame. Rows this list does not own (a hand-authored
			// block without a GamemodeRow) are left alone.
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				GamemodeRow row = transform.GetChild(i).GetComponent<GamemodeRow>();

				if (row != null)
				{
					row.gameObject.SetActive(false);
					Destroy(row.gameObject);
				}
			}

			if (RowPrefab == null)
			{
				Debug.LogError("[GamemodeList] No row prefab assigned - the gamemode list stays empty.", this);
				return;
			}

			IReadOnlyList<GamemodeDefinition> modes = GamemodeRegistry.Modes;
			GamemodeDefinition firstVisible = null;
			bool selectionStillValid = false;

			for (int i = 0; i < modes.Count; i++)
			{
				GamemodeDefinition mode = modes[i];

				if (firstVisible == null)
				{
					firstVisible = mode;
				}

				if (Selected != null && mode.Id == Selected.Id)
				{
					selectionStillValid = true;
				}

				GamemodeRow row = Instantiate(RowPrefab, transform);
				row.Bind(mode);
			}

			if (!selectionStillValid)
			{
				Selected = firstVisible;
				RaiseSelectedChanged();
			}
		}

		private void RaiseSelectedChanged()
		{
			if (SelectedChanged != null)
			{
				SelectedChanged();
			}
		}
	}
}
