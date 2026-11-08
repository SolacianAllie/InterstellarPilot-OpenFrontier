using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Skirmish.SkirmishTeamSetup
{
	public class SkirmishTeamSetupScreen : ScreenBase
	{
		public Text MaxShipsText;

		public Button SetAsPlayerButton;

		public Button AddShipButton;

		public SkirmishTeamSetupShipList AvailableShipList;

		public Button ClearButton;

		private int editedTeamIndex = -1;

		public Button RemoveShipButton;

		public SkirmishTeamSetupShipList SelectedShipList;

		public Button ShipInfoButton;

		private SkirmishSetupScreen skirmishSetupUI;

		public Text TeamLabel;

		public bool IncludeCustomVariants { get; set; } = true;

		public List<CustomUnitVariant> LoadedCustomUnitVariants { get; set; } = new List<CustomUnitVariant>();

		public SkirmishShipItem CurrentShipType
		{
			get
			{
				if (SelectedShipList.FirstSelectedItem != null)
				{
					return SelectedShipList.FirstSelectedItem;
				}
				return AvailableShipList.FirstSelectedItem;
			}
		}

		public SkirmishSetupScreen SkirmishSetupUI
		{
			get
			{
				return skirmishSetupUI;
			}
			set
			{
				skirmishSetupUI = value;
				if (skirmishSetupUI != null)
				{
					editedTeamIndex = skirmishSetupUI.TeamList.ActiveItems.IndexOf(skirmishSetupUI.EditedTeam);
					TeamLabel.text = skirmishSetupUI.EditedTeam.TeamName;
					SelectedShipList.SetItems(from e in skirmishSetupUI.GetTeamShips(editedTeamIndex)
						select e.Clone());
					SelectedShipList.TeamIndex = editedTeamIndex;
				}
			}
		}

		public void LoadAvailableShipData()
		{
			List<SkirmishShipItem> items = (from e in SkirmishHelper.GetAvailableShips(IncludeCustomVariants, LoadedCustomUnitVariants)
				orderby e.UnitClass.UnitSeries.DisplayOrder, e.SaleCost
				select SkirmishShipItem.FromSkirmishTeamParamsItem(e)).ToList();
			AvailableShipList.SetItems(items);
			AvailableShipList.ResetScrollPosition();
		}

		public void ClearShipSelection()
		{
			SelectedShipList.ClearActiveItems();
		}

		protected override void awake()
		{
			base.awake();
			SetAsPlayerButton.onClick.AddListener(SetAsPlayerButtonClick);
		}

		protected override void onEnable()
		{
			base.onEnable();
			if (AvailableShipList.FirstSelectedItem == null && AvailableShipList.ActiveItems.Count > 0)
			{
				AvailableShipList.SelectFirstItem();
			}
		}

		protected override void start()
		{
			base.start();
			if (ClearButton != null)
			{
				ClearButton.onClick.AddListener(ClearShipSelection);
			}
			if (ShipInfoButton != null)
			{
				ShipInfoButton.onClick.AddListener(ShowShipInfo);
			}
			AvailableShipList.SelectedItemChanged += AvailableShipList_SelectedItemChanged;
			SelectedShipList.SelectedItemChanged += SelectedShipList_SelectedItemChanged;
			AddShipButton.onClick.AddListener(AddShip);
			RemoveShipButton.onClick.AddListener(RemoveShip);
			LoadAvailableShipData();
			MaxShipsText.text = $"(Max {GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeamShips} ships)";
		}

		protected override void update()
		{
			base.update();
			SetAsPlayerButton.interactable = SelectedShipList.SelectedIndex > 0;
		}

		protected override void refresh()
		{
			base.refresh();
			AvailableShipList.Refresh();
			SelectedShipList.Refresh();
			RefreshButtons();
		}

		protected override bool onNavigatingBack()
		{
			if (skirmishSetupUI != null && editedTeamIndex > -1)
			{
				skirmishSetupUI.SetTeamShips(editedTeamIndex, SelectedShipList.ActiveItems);
			}
			return base.onNavigatingBack();
		}

		private void RefreshShipInfoButton()
		{
			ShipInfoButton.interactable = CurrentShipType != null;
		}

		private void RemoveShip()
		{
			int num = SelectedShipList.ActiveItems.IndexOf(SelectedShipList.FirstSelectedItem);
			SelectedShipList.Remove(SelectedShipList.FirstSelectedItem);
			if (num > SelectedShipList.ActiveItems.Count - 1)
			{
				num = SelectedShipList.ActiveItems.Count - 1;
			}
			if (num > -1 && num < SelectedShipList.ActiveItems.Count)
			{
				SelectedShipList.FirstSelectedItem = SelectedShipList.ActiveItems[num];
			}
			else
			{
				AvailableShipList.SelectFirstItem();
			}
		}

		private void AddShip()
		{
			SkirmishShipItem firstSelectedItem = AvailableShipList.FirstSelectedItem;
			SelectedShipList.Add(firstSelectedItem.Clone());
			RefreshButtons();
			SelectedShipList.Refresh();
			AvailableShipList.FirstSelectedItem = firstSelectedItem;
		}

		private void SelectedShipList_SelectedItemChanged(ScrollList<SkirmishShipItem> sender, SkirmishShipItem oldItem, SkirmishShipItem newItem)
		{
			RefreshButtons();
		}

		private void AvailableShipList_SelectedItemChanged(ScrollList<SkirmishShipItem> sender, SkirmishShipItem oldItem, SkirmishShipItem newItem)
		{
			RefreshButtons();
		}

		private void RefreshButtons()
		{
			AddShipButton.interactable = AvailableShipList.FirstSelectedItem != null && SelectedShipList.ActiveItems.Count < GameController.Instance.GameSettings.SkirmishSettings.MaxSkirmishTeamShips;
			RemoveShipButton.interactable = SelectedShipList.FirstSelectedItem != null;
			RefreshShipInfoButton();
			if (ClearButton != null)
			{
				ClearButton.interactable = SelectedShipList.ActiveItems.Count > 0;
			}
		}

		private void ShowShipInfo()
		{
			UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(CurrentShipType.UnitClass.UnitPrefab);
		}

		private void SetAsPlayerButtonClick()
		{
			SkirmishShipItem[] array = SelectedShipList.ActiveItems.ToArray();
			int selectedIndex = SelectedShipList.SelectedIndex;
			if (selectedIndex > 0 && selectedIndex < SelectedShipList.ActiveItems.Count)
			{
				SkirmishShipItem skirmishShipItem = array[0];
				array[0] = SelectedShipList.FirstSelectedItem;
				array[selectedIndex] = skirmishShipItem;
				SelectedShipList.SetItems(array);
			}
			SelectedShipList.Refresh();
		}
	}
}
