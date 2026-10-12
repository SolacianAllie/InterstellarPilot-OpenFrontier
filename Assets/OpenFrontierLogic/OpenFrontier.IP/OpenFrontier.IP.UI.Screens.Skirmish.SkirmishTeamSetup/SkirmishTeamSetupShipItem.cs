using System;
using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Skirmish.SkirmishTeamSetup
{
	public class SkirmishTeamSetupShipItem : ScrollListItem<SkirmishShipItem>
	{
		public Text ClassLabel;

		public Text NameLabel;

		public Image ShipImage;

		public override void Refresh()
		{
			base.Refresh();
			if (Item.UnitClass != null)
			{
				string text = Item.GetClassAndSeriesName();
				SkirmishTeamSetupShipList skirmishTeamSetupShipList = (SkirmishTeamSetupShipList)ParentList;
				if (skirmishTeamSetupShipList.TeamIndex == 0 && skirmishTeamSetupShipList.ActiveItems.IndexOf(Item) == 0)
				{
					text += " (You)";
				}
				NameLabel.text = text;
				if (ClassLabel != null)
				{
					ClassLabel.text = Enum.GetName(typeof(ShipHullType), Item.UnitClass.HullType);
				}
				ShipImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(Item.UnitClass);
			}
			else
			{
				NameLabel.text = string.Empty;
			}
		}
	}
}
