using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// The scroll list of saved multiplayer hosts.
	///
	/// Mirrors <see cref="LoadGameItemsUI"/> - same generic list, same
	/// refresh contract - but sourced from the host store instead of the save
	/// game folder. That is what lets the rest of this screen reuse the existing
	/// selection, scrolling and multi-select machinery unchanged.
	/// </summary>
	public class SavedHostItemsUI : ScrollList<SavedHost>
	{
		protected override void OnRefreshing()
		{
			// Most recently added first, matching the save-game list's ordering
			// so the two screens feel the same.
			SetItems(SavedHostStore.Hosts.OrderByDescending(h => h.AddedUtc).ToList());
		}
	}

	/// <summary>
	/// One row in the host list.
	///
	/// Shows the host's name, and whether a session with it is currently
	/// connected. The status is deliberately not a live ping - there is no
	/// server to ask - so it reports only what is actually known.
	/// </summary>
	public class SavedHostItemUI : ScrollListItem<SavedHost>
	{
		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI StatusLabel;

		/// <summary>Dimmed when the host is not currently connected.</summary>
		public Graphic OfflineIndicator;

		public override void Refresh()
		{
			base.Refresh();

			if (Item == null)
			{
				return;
			}

			NameLabel.text = string.IsNullOrWhiteSpace(Item.Name) ? Item.Code : Item.Name;

			if (Item.IsConnected)
			{
				StatusLabel.text = "Connected";
			}
			else
			{
				// No server exists to ask whether a host is up, so say what is
				// true rather than guessing at liveness.
				StatusLabel.text = "Not connected";
			}

			if (OfflineIndicator != null)
			{
				OfflineIndicator.enabled = !Item.IsConnected;
			}
		}
	}
}