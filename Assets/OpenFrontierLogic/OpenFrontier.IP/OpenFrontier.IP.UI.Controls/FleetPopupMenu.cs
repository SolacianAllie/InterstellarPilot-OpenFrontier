using OpenFrontier.IP.Assets.Scripts.Engine.Core;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Fleets;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using OpenFrontier.IP.UI.Screens.SectorMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class FleetPopupMenu : MonoBehaviour
	{
		private PopupMenu popupMenu;

		public Button FleetSettingsButton;

		public Button ViewFleetButton;

		public Button OrdersButton;

		public Button RenameFleetButton;

		public Button DisbandFleetButton;

		public Button SplitFleetButton;

		public Button NewOrderButton;

		public Button ClearOrdersButton;

		public Button EnterFleetButton;

		public Button FleetCargoButton;

		public TextMeshProUGUI FleetNameLabel;

		private double lastSetNameTime;

		public Fleet Fleet { get; set; }

		private void Awake()
		{
			popupMenu = GetComponent<PopupMenu>();
			popupMenu.Opening += PopupMenu_Opening;
			if (DisbandFleetButton != null)
			{
				DisbandFleetButton.onClick.AddListener(DisbandFleetButtonClick);
			}
			if (RenameFleetButton != null)
			{
				RenameFleetButton.onClick.AddListener(RenameFleetButtonClick);
			}
			if (NewOrderButton != null)
			{
				NewOrderButton.onClick.AddListener(NewOrderButtonClick);
			}
			if (ClearOrdersButton != null)
			{
				ClearOrdersButton.onClick.AddListener(ClearOrdersButtonClick);
			}
			if (SplitFleetButton != null)
			{
				SplitFleetButton.onClick.AddListener(SplitFleetButtonClick);
			}
			if (FleetCargoButton != null)
			{
				FleetCargoButton.onClick.AddListener(FleetCargoButtonClick);
			}
			if (ViewFleetButton != null)
			{
				ViewFleetButton.onClick.AddListener(ViewFleetButtonClick);
			}
			if (FleetSettingsButton != null)
			{
				FleetSettingsButton.onClick.AddListener(FleetSettingsButtonClick);
			}
			if (EnterFleetButton != null)
			{
				EnterFleetButton.onClick.AddListener(EnterFleetButtonClick);
			}
			if (OrdersButton != null)
			{
				OrdersButton.onClick.AddListener(OrdersButtonClick);
			}
		}

		private void OrdersButtonClick()
		{
			if (ShowOrdersButton())
			{
				UIController.Instance.ScreenNavigator.ShowOrdersScreen(Fleet);
			}
		}

		private void EnterFleetButtonClick()
		{
			if (ShowEnterFleetButton())
			{
				EngineASX.Instance.ChangePlayerUnit(Fleet.LeaderUnit);
			}
		}

		private void FleetSettingsButtonClick()
		{
			if (ShowFleetSettingsButton())
			{
				UIController.Instance.ScreenNavigator.ShowFleetSettingsScreen(Fleet);
			}
		}

		private void ViewFleetButtonClick()
		{
			if (ShowViewFleetButton())
			{
				FleetsHelper.ViewFleet(Fleet);
			}
		}

		private void FleetCargoButtonClick()
		{
			if (ShowFleetCargoButton())
			{
				UIController.Instance.ScreenNavigator.ShowFleetCargoScreen(Fleet);
			}
		}

		private void SplitFleetButtonClick()
		{
			if (ShowSplitFleetButton())
			{
				FleetUtils.SplitFleet(Fleet, EngineASX.Instance.PlayerFleetPrefab);
			}
		}

		private void ClearOrdersButtonClick()
		{
			if (ShowClearOrdersButton())
			{
				Fleet.ClearOrders();
			}
		}

		private void NewOrderButtonClick()
		{
			if (!ShowNewOrderButton())
			{
				return;
			}
			NewOrderTarget newOrderTarget = NewOrderTarget.CreateFromFleets(Fleet);
			ScreenBase currentScreen = UIController.Instance.ScreenNavigator.CurrentScreen;
			UIController.Instance.ScreenNavigator.ShowNewFleetOrderScreen(newOrderTarget, () =>
			{
				if (currentScreen != null)
				{
					UIController.Instance.ScreenNavigator.NavigateBackTo(currentScreen);
				}
			});
		}

		private bool ShowNewOrderButton()
		{
			return Fleet != null;
		}

		private void RenameFleetButtonClick()
		{
			if (!ShowRenameFleetButton())
			{
				return;
			}
			UIController.Instance.ScreenNavigator.ShowRenameScreen(Fleet.Name, GameController.Instance.GameSettings.GeneralFleetSettings.MinNameLength, GameController.Instance.GameSettings.GeneralFleetSettings.MaxNameLength, (RenameUnitScreen handler, bool rename, string newName) =>
			{
				if (rename)
				{
					Fleet.Name = newName;
				}
			});
		}

		private bool ShowRenameFleetButton()
		{
			return Fleet != null;
		}

		private void DisbandFleetButtonClick()
		{
			if (ShowDisbandFleetButton())
			{
				UIController.Instance.ShowMessageBox("Are you sure?", MessageBoxButtons.OkCancel, DisbandFleetButtonClickConfirm, MessageBoxIcon.Warning, "Disband fleet");
			}
		}

		private bool ShowDisbandFleetButton()
		{
			return Fleet != null;
		}

		private void DisbandFleetButtonClickConfirm(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				OrdersHelper.DisbandPlayerFleet(Fleet);
			}
		}

		private bool PopupMenu_Opening(PopupMenu sender)
		{
			RefreshButtons();
			return true;
		}

		private void RefreshButtons()
		{
			if (OrdersButton != null)
			{
				OrdersButton.gameObject.SetActive(ShowOrdersButton());
			}
			if (ViewFleetButton != null)
			{
				ViewFleetButton.gameObject.SetActive(ShowViewFleetButton());
			}
			if (SplitFleetButton != null)
			{
				SplitFleetButton.gameObject.SetActive(ShowSplitFleetButton());
			}
			if (ClearOrdersButton != null)
			{
				ClearOrdersButton.gameObject.SetActive(ShowClearOrdersButton());
			}
			if (EnterFleetButton != null)
			{
				EnterFleetButton.gameObject.SetActive(ShowEnterFleetButton());
			}
		}

		private bool ShowOrdersButton()
		{
			return Fleet != null;
		}

		private bool ShowViewFleetButton()
		{
			if (Fleet != null)
			{
				return !(UIController.Instance.ScreenNavigator.CurrentScreen is SectorMapScreen);
			}
			return false;
		}

		private bool ShowSplitFleetButton()
		{
			if (Fleet != null)
			{
				return FleetUtils.CanSplitFleet(Fleet);
			}
			return false;
		}

		private bool ShowClearOrdersButton()
		{
			if (Fleet != null)
			{
				return Fleet.HasAnyOrders;
			}
			return false;
		}

		private bool ShowEnterFleetButton()
		{
			if (Fleet != null)
			{
				return WorldHelper.CanEnterUnitFromCurrentUnit(Fleet.LeaderUnit);
			}
			return false;
		}

		private bool ShowFleetSettingsButton()
		{
			return Fleet != null;
		}

		private bool ShowFleetCargoButton()
		{
			return Fleet != null;
		}

		private void Update()
		{
			if (Fleet != null && Time.realtimeSinceStartupAsDouble > lastSetNameTime + 1.0)
			{
				Refresh();
				lastSetNameTime = Time.realtimeSinceStartupAsDouble;
			}
		}

		public void Refresh()
		{
			if (Fleet != null)
			{
				FleetNameLabel.text = Fleet.GetFriendlyName();
			}
		}
	}
}
