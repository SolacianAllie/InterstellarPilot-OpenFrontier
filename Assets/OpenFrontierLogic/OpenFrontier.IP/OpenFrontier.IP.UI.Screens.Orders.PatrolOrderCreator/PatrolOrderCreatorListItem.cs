using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders.PatrolOrderCreator
{
	public class PatrolOrderCreatorListItem : ScrollListItem<SectorTarget>
	{
		public TextMeshProUGUI PositionText;

		public Button DeleteButton;

		public int Index => ParentList.ActiveItems.IndexOf(Item);

		protected override void awake()
		{
			base.awake();
			DeleteButton.onClick.AddListener(DeleteButtonClick);
		}

		private void DeleteButtonClick()
		{
			ParentPatrolOrderCreatorList().RequestDelete(this, Item);
		}

		private PatrolOrderCreatorList ParentPatrolOrderCreatorList()
		{
			return (PatrolOrderCreatorList)ParentList;
		}

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				RefreshText();
			}
		}

		private void RefreshText()
		{
			PositionText.text = $"{Index + 1}. {Item.Sector.Name} - {TextFormattingHelper.FormatSectorPosition(Item.SectorPosition)}";
			PositionText.color = ((ParentPatrolOrderCreatorList().CurrentSector == Item.Sector) ? Color.white : ParentPatrolOrderCreatorList().InactiveSectorListItemColor);
		}
	}
}
