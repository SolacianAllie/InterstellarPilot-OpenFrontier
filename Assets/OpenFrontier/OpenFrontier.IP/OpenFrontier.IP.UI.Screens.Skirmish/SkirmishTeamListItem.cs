using System.Linq;
using OpenFrontier.IP.UI.Components;
using OpenFrontier.IP.UI.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Skirmish
{
	public class SkirmishTeamListItem : ScrollListItem<SkirmishTeam>
	{
		public ShipIconsController ShipIconsController;

		public TextMeshProUGUI NameLabel;

		public Transform ShipsContainer;

		public Image ShipImagePrefab;

		public override void Refresh()
		{
			NameLabel.text = Item.TeamName;
			NameLabel.SetAlpha((Item.Ships.Count > 0) ? 1f : 0.4f);
			RefreshShipIcons();
			base.Refresh();
		}

		private void RefreshShipIcons()
		{
			ShipIconsController.RefreshShipImages(Item.Ships.Select((SkirmishShipItem e) => new ShipIconsController.ShipIconItem(e.UnitClass, 1f)).ToList());
		}
	}
}
