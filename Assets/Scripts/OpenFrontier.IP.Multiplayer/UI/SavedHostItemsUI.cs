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
	/// Shows the host's name and what the last liveness probe found: online,
	/// offline, on a different build, or up but not accepting players.
	/// "Checking..." while a probe is in flight - claiming a host is offline
	/// before asking it would be a lie the player acts on.
	/// </summary>
	public class SavedHostItemUI : ScrollListItem<SavedHost>
	{
		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI StatusLabel;

		/// <summary>Dimmed when the host could not be reached.</summary>
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
				// Being in the session is the most useful thing to say, so it
				// takes precedence over the probe result.
				StatusLabel.text = "Connected";
			}
			else
			{
				StatusLabel.text = Item.ProbeResult.Describe();
			}

			if (OfflineIndicator != null)
			{
				// Dim only on a confirmed negative. While a probe is still in
				// flight there is nothing to dim for.
				OfflineIndicator.enabled = !Item.IsConnected
					&& Item.ProbeResult.IsKnown
					&& Item.ProbeResult.Reachability == HostReachability.Unreachable;
			}
		}
	}
}