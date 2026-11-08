using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Bounty
{
	public class BountyScreen : EngineScreen
	{
		private float lastChangedTime;

		public BountyScreenItemList ItemList;

		public FactionBountyBoard BountyBoard;

		public Button SetWaypointButton;

		public Button ViewButton;

		private static List<FactionBountyItem> itemCache = new List<FactionBountyItem>(100);

		public bool AutoDiscoverLocations = true;

		public Button ViewAllBountiesButton;

		public TextMeshProUGUI CurrentBountyBoardLabel;

		protected override void awake()
		{
			base.awake();
			SetWaypointButton.onClick.AddListener(SetWaypointButtonClick);
			ViewButton.onClick.AddListener(ViewButtonClick);
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
			ViewAllBountiesButton.onClick.AddListener(ViewAllBountiesButtonClick);
		}

		private void ItemList_SelectedItemChanged(ScrollList<FactionBountyItem> sender, FactionBountyItem oldItem, FactionBountyItem newItem)
		{
			if (newItem != null)
			{
				RefreshViewButtonEnabled();
				RefreshWaypointButtonEnabled();
			}
		}

		private void ViewAllBountiesButtonClick()
		{
			ViewAllBountiesButton.gameObject.SetActive(value: false);
			BountyBoard = null;
			Refresh();
		}

		private void SetWaypointButtonClick()
		{
			if (Eng.LocalPlayer != null)
			{
				Eng.LocalPlayer.SetCustomWaypointToSectorPosition(ItemList.FirstSelectedItem.LastKnownSector, ItemList.FirstSelectedItem.LastKnownSectorPosition.Value, autoRemove: true);
			}
		}

		private void ViewButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowSectorMapScreenShowingSectorPosition(ItemList.FirstSelectedItem.LastKnownSector, ItemList.FirstSelectedItem.LastKnownSectorPosition.Value);
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshItems();
			RefreshWaypointButtonEnabled();
			RefreshViewButtonEnabled();
			RefreshCurrentBountyBoardLabel();
			ViewAllBountiesButton.gameObject.SetActive(BountyBoard != null);
			if (!AutoDiscoverLocations || !(EngineASX.Instance.LocalFaction != null) || !(EngineASX.Instance.LocalFaction.Intel != null))
			{
				return;
			}
			foreach (FactionBountyItem activeItem in ItemList.ActiveItems)
			{
				if (activeItem != null && activeItem.LastKnownSector != null)
				{
					EngineASX.Instance.LocalFaction.Intel.DiscoverSector(activeItem.LastKnownSector);
				}
			}
		}

		private void RefreshCurrentBountyBoardLabel()
		{
			if (BountyBoard != null)
			{
				if (BountyBoard.Faction != null)
				{
					CurrentBountyBoardLabel.text = BountyBoard.Faction.GetFriendlyName();
				}
				else
				{
					CurrentBountyBoardLabel.text = "[None]";
				}
			}
			else
			{
				CurrentBountyBoardLabel.text = "All";
			}
		}

		private IEnumerable<FactionBountyItem> GetBountyItemsFromBountyBoard(FactionBountyBoard bountyBoard)
		{
			return bountyBoard.BountyItems.Where((FactionBountyItem e) => e.IsValid);
		}

		private void RefreshItems()
		{
			ItemList.SetItems(GetItems());
		}

		private IEnumerable<FactionBountyItem> GetItems()
		{
			if (BountyBoard != null)
			{
				return (from e in GetBountyItemsFromBountyBoard(BountyBoard)
					orderby e.Bounty descending
					select e).ToList();
			}
			itemCache.Clear();
			Faction localFaction = Eng.LocalFaction;
			if (localFaction != null && localFaction.IsValidInGame)
			{
				foreach (FactionAttitude relation in localFaction.Relations)
				{
					if (relation.TargetFaction != null && relation.TargetFaction.IsValidInGame && relation.TargetFaction != localFaction)
					{
						FactionBountyBoard bountyBoard = relation.TargetFaction.BountyBoard;
						if (bountyBoard != null)
						{
							itemCache.AddRange(GetBountyItemsFromBountyBoard(bountyBoard));
						}
					}
				}
			}
			return itemCache.OrderByDescending((FactionBountyItem e) => e.Bounty).ToList();
		}

		private void RefreshWaypointButtonEnabled()
		{
			SetWaypointButton.interactable = HasItemWithValidPosition();
		}

		private bool HasItemWithValidPosition()
		{
			if (ItemList.FirstSelectedItem != null && ItemList.FirstSelectedItem.LastKnownSector != null)
			{
				return ItemList.FirstSelectedItem.LastKnownSectorPosition.HasValue;
			}
			return false;
		}

		private void RefreshViewButtonEnabled()
		{
			ViewButton.interactable = HasItemWithValidPosition();
		}

		protected override void update()
		{
			base.update();
			if (BountyBoard != null && (lastChangedTime == 0f || BountyBoard.LastChangedTime > lastChangedTime))
			{
				Refresh();
				lastChangedTime = BountyBoard.LastChangedTime;
			}
		}
	}
}
