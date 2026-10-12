using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Assets.Scripts.Engine.Core;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Fleets
{
	public class FleetsScreen : EngineScreen
	{
		public FleetsListSorter ListSorter;

		public ModalWindow ViewFilterModal;

		public SectorFilter SectorFilter;

		public PopupMenu SelectedFleetPopupMenu;

		public Button FleetSettingsButton;

		public Toggle ShowSingleShipsToggle;

		public FleetsList FleetsList;

		public Button ViewFleetButton;

		public Button OrdersButton;

		public Button RenameFleetButton;

		public Button DisbandFleetButton;

		public Button SplitFleetButton;

		public Button NewOrderButton;

		public Button ClearOrdersButton;

		public Button EnterFleetButton;

		public Button FleetCargoButton;

		private double lastTimeRefreshedList;

		private double nextCheckFleetsListStale;

		public double MaxTimeBeforeFullRefresh = 15.0;

		private double nextTimeRefreshVisible;

		public double RefreshVisibleItemsInterval = 1.0;

		public double CheckFleetsListStaleInterval = 2.0;

		public Fleet SingleSelectedFleet => FleetsList.SingleSelectedItem;

		protected override void awake()
		{
			base.awake();
			RenameFleetButton.onClick.AddListener(RenameFleetButtonClick);
			OrdersButton.onClick.AddListener(OrdersButtonClick);
			ViewFleetButton.onClick.AddListener(ViewFleetButtonClick);
			DisbandFleetButton.onClick.AddListener(DisbandFleetButtonClick);
			SplitFleetButton.onClick.AddListener(SplitFleetButtonClick);
			ShowSingleShipsToggle.onValueChanged.AddListener(ShowSingleShipsToggleValueChanged);
			NewOrderButton.onClick.AddListener(NewOrderButtonClick);
			ClearOrdersButton.onClick.AddListener(ClearOrdersButtonClick);
			EnterFleetButton.onClick.AddListener(EnterFleetButtonClick);
			FleetCargoButton.onClick.AddListener(FleetCargoButtonClick);
			FleetSettingsButton.onClick.AddListener(FleetSettingsButtonClick);
			FleetsList.SelectedItemsChanged += FleetsList_SelectedItemsChanged;
			RenameFleetButton.gameObject.SetActive(value: false);
			OrdersButton.gameObject.SetActive(value: false);
			ViewFleetButton.gameObject.SetActive(value: false);
			DisbandFleetButton.gameObject.SetActive(value: false);
			SplitFleetButton.gameObject.SetActive(value: false);
			NewOrderButton.gameObject.SetActive(value: false);
			ClearOrdersButton.gameObject.SetActive(value: false);
			FleetSettingsButton.gameObject.SetActive(value: false);
			EnterFleetButton.gameObject.SetActive(value: false);
			FleetCargoButton.gameObject.SetActive(value: false);
			SectorFilter.Changed += SectorFilter_Changed;
			SelectedFleetPopupMenu.Opening += SelectedFleetPopupMenu_Opening;
			ListSorter.Applying += ListSorter_Applying;
		}

		private void ListSorter_Applying(ListSorter sender)
		{
			Refresh();
		}

		private bool SelectedFleetPopupMenu_Opening(PopupMenu sender)
		{
			RenameFleetButton.gameObject.SetActive(ShowRenameFleetButton());
			ViewFleetButton.gameObject.SetActive(ShowViewFleetButton());
			DisbandFleetButton.gameObject.SetActive(ShowDisbandFleetButton());
			SplitFleetButton.gameObject.SetActive(ShowSplitFleetButton());
			ClearOrdersButton.gameObject.SetActive(ShowClearOrdersButton());
			NewOrderButton.gameObject.SetActive(ShowNewOrderButton());
			FleetSettingsButton.gameObject.SetActive(ShowFleetSettingsButton());
			EnterFleetButton.gameObject.SetActive(ShowEnterFleetButton());
			FleetCargoButton.gameObject.SetActive(ShowFleetCargoButton());
			return true;
		}

		private bool ShowFleetCargoButton()
		{
			return SingleSelectedFleet != null;
		}

		private bool ShowEnterFleetButton()
		{
			if (SingleSelectedFleet != null)
			{
				return WorldHelper.CanEnterUnitFromCurrentUnit(SingleSelectedFleet.LeaderUnit);
			}
			return false;
		}

		private bool ShowFleetSettingsButton()
		{
			return FleetsList.SelectedItems.Count > 0;
		}

		private bool ShowNewOrderButton()
		{
			return FleetsList.SelectedItems.Count > 0;
		}

		private bool ShowClearOrdersButton()
		{
			if (FleetsList.SelectedItems.Count > 0)
			{
				return AnySelectedFleetHasOrders();
			}
			return false;
		}

		private bool ShowSplitFleetButton()
		{
			if (FleetsList.SelectedItems.Count > 0)
			{
				return CanSplitSelectedFleets();
			}
			return false;
		}

		private bool ShowDisbandFleetButton()
		{
			return FleetsList.SelectedItems.Count > 0;
		}

		private bool ShowOrdersButton()
		{
			return SingleSelectedFleet != null;
		}

		private bool ShowViewFleetButton()
		{
			if (SingleSelectedFleet != null)
			{
				return SingleSelectedFleet.Sector != null;
			}
			return false;
		}

		private bool ShowRenameFleetButton()
		{
			return SingleSelectedFleet != null;
		}

		private void SectorFilter_Changed(SectorFilter sender)
		{
			Refresh();
			FleetsList.ResetScrollPosition();
		}

		private void FleetsList_SelectedItemsChanged(ScrollListBase sender)
		{
			SelectedFleetPopupMenu.gameObject.SetActive(FleetsList.SelectedItems.Count > 0);
			RefreshOrdersButtonVisible();
		}

		private void RefreshOrdersButtonVisible()
		{
			OrdersButton.gameObject.SetActive(ShowOrdersButton());
		}

		private void ShowSingleShipsToggleValueChanged(bool value)
		{
			Refresh();
		}

		private void EnterFleetButtonClick()
		{
			if (ShowEnterFleetButton())
			{
				EngineASX.Instance.ChangePlayerUnit(FleetsList.FirstSelectedItem.LeaderUnit);
			}
		}

		private void FleetCargoButtonClick()
		{
			if (ShowFleetCargoButton())
			{
				UIController.Instance.ScreenNavigator.ShowFleetCargoScreen(FleetsList.FirstSelectedItem);
			}
		}

		private void NewOrderButtonClick()
		{
			if (!ShowNewOrderButton() || FleetsList.SelectedItems.Count <= 0)
			{
				return;
			}
			NewOrderTarget newOrderTarget = NewOrderTarget.CreateFromFleets(FleetsList.SelectedItems);
			UIController.Instance.ScreenNavigator.ShowNewFleetOrderScreen(newOrderTarget, () =>
			{
				if ((bool)this)
				{
					UIController.Instance.ScreenNavigator.NavigateBackTo(this);
					Refresh();
				}
			});
		}

		private void ClearOrdersButtonClick()
		{
			if (!ShowClearOrdersButton())
			{
				return;
			}
			foreach (Fleet selectedItem in FleetsList.SelectedItems)
			{
				selectedItem.ClearOrders();
			}
			Refresh();
		}

		private void FleetSettingsButtonClick()
		{
			if (ShowFleetSettingsButton())
			{
				UIController.Instance.ScreenNavigator.ShowFleetSettingsScreen(FleetsList.SelectedItems);
			}
		}

		private void ViewFleetButtonClick()
		{
			if (ShowViewFleetButton())
			{
				FleetsHelper.ViewFleet(SingleSelectedFleet);
			}
		}

		private void OrdersButtonClick()
		{
			if (ShowOrdersButton())
			{
				UIController.Instance.ScreenNavigator.ShowOrdersScreen(SingleSelectedFleet);
			}
		}

		private void RenameFleetButtonClick()
		{
			if (ShowRenameFleetButton())
			{
				UIController.Instance.ScreenNavigator.ShowRenameScreen(SingleSelectedFleet.Name, GameController.Instance.GameSettings.GeneralFleetSettings.MinNameLength, GameController.Instance.GameSettings.GeneralFleetSettings.MaxNameLength, OnFleetRenamed);
			}
		}

		private void SplitFleetButtonClick()
		{
			if (!ShowSplitFleetButton())
			{
				return;
			}
			List<Fleet> list = FleetsList.SelectedItems.ToList();
			foreach (Fleet selectedItem in FleetsList.SelectedItems)
			{
				Fleet fleet = FleetUtils.SplitFleet(selectedItem, EngineASX.Instance.PlayerFleetPrefab);
				if (fleet != null)
				{
					list.Add(fleet);
				}
			}
			Refresh();
			if (FleetsList.IsMultiSelectEnabled)
			{
				FleetsList.AddToSelection(list);
			}
			else
			{
				FleetsList.FirstSelectedItem = list.Last();
			}
		}

		public void DisbandFleetButtonClick()
		{
			if (ShowDisbandFleetButton() && FleetsList.SelectedItems.Count > 0)
			{
				UIController.Instance.ShowMessageBox("Are you sure?", MessageBoxButtons.OkCancel, DisbandFleetButtonClickConfirm, MessageBoxIcon.Warning, "Disband fleet");
			}
		}

		private void DisbandFleetButtonClickConfirm(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				Fleet[] array = FleetsList.SelectedItems.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					OrdersHelper.DisbandPlayerFleet(array[i]);
				}
				Refresh();
			}
		}

		private void OnFleetRenamed(RenameUnitScreen handler, bool rename, string newName)
		{
			if (rename)
			{
				SingleSelectedFleet.Name = newName;
				FleetsList.SelectedUIItem.Refresh();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			SectorFilter.Refresh();
			RefreshFleetsList();
			SelectedFleetPopupMenu.gameObject.SetActive(FleetsList.SelectedItems.Count > 0);
			RefreshOrdersButtonVisible();
		}

		private void RefreshFleetsList(IEnumerable<Fleet> fleets = null)
		{
			if (EngineASX.Instance.LocalFaction != null)
			{
				if (fleets == null)
				{
					fleets = GetFilteredOrderedFleets();
				}
				FleetsList.SetItems(fleets);
			}
			lastTimeRefreshedList = Time.realtimeSinceStartupAsDouble;
			SetNextCheckFleetsListStaleTime();
			SetNextRefreshVisibleItemsTime();
		}

		private void SetNextRefreshVisibleItemsTime()
		{
			nextTimeRefreshVisible = Time.realtimeSinceStartupAsDouble + RefreshVisibleItemsInterval;
		}

		private IEnumerable<Fleet> GetFilteredFleets()
		{
			return from e in FleetsHelper.GetPlayerFleets(ShowSingleShipsToggle.isOn)
				where ShouldShowFleet(e)
				select e;
		}

		private IOrderedEnumerable<Fleet> GetFilteredOrderedFleets()
		{
			_ = EngineASX.Instance.ActiveSector;
			_ = EngineASX.Instance.LocalUnit.SectorPosition;
			return GetFilteredFleets().OrderBy((Fleet e) => e, ListSorter);
		}

		private bool ShouldShowFleet(Fleet fleet)
		{
			if (SectorFilter.Sector != null)
			{
				return fleet.Sector == SectorFilter.Sector;
			}
			return true;
		}

		protected override void update()
		{
			base.update();
			if (Time.realtimeSinceStartupAsDouble > nextCheckFleetsListStale)
			{
				IEnumerable<Fleet> filteredFleets = GetFilteredFleets();
				if (Time.realtimeSinceStartupAsDouble > lastTimeRefreshedList + MaxTimeBeforeFullRefresh || FleetsList.ActiveItems.Any((Fleet e) => !FleetsHelper.ShouldShowFleet(e) || !ShouldShowFleet(e)) || filteredFleets.Any((Fleet e) => !FleetsList.ActiveItems.Contains(e)))
				{
					RefreshFleetsList(filteredFleets.OrderBy((Fleet e) => e, ListSorter));
				}
			}
			if (Time.realtimeSinceStartupAsDouble > nextTimeRefreshVisible)
			{
				FleetsList.Refresh();
				SetNextRefreshVisibleItemsTime();
			}
		}

		public void SetNextCheckFleetsListStaleTime()
		{
			nextCheckFleetsListStale = Time.realtimeSinceStartupAsDouble + CheckFleetsListStaleInterval;
		}

		private bool AnySelectedFleetHasOrders()
		{
			foreach (Fleet selectedItem in FleetsList.SelectedItems)
			{
				if (selectedItem.HasAnyOrders)
				{
					return true;
				}
			}
			return false;
		}

		private bool CanSplitSelectedFleets()
		{
			if (FleetsList.SelectedItems.Count == 0)
			{
				return false;
			}
			foreach (Fleet selectedItem in FleetsList.SelectedItems)
			{
				if (!FleetUtils.CanSplitFleet(selectedItem))
				{
					return false;
				}
			}
			return true;
		}

		protected override void onDisable()
		{
			base.onDisable();
			ViewFilterModal.ToggleActive(active: false);
		}

		public void ResetFiltersWhenShown()
		{
			Sector sector = EngineASX.Instance.ActiveSector;
			if (sector == null)
			{
				sector = EngineASX.Instance.LocalPlayerSector;
			}
			SectorFilter.Sector = sector;
			FleetsList.ResetScrollPosition();
		}
	}
}
