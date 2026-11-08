using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CachedFleetSettings;
using OpenFrontier.IP.UI.Screens.Orders;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FleetSettings
{
	public class FleetSettingsScreen : EngineScreen
	{
		public Button UseDefaultsButton;

		public Button SetAsDefaultsButton;

		public Button ApplyToAllFleetsButton;

		public AdvancedOrdersController AdvancedOrdersController;

		public StanceSettingsController StanceSettingsController;

		public IEnumerable<Fleet> Fleets;

		public TextMeshProUGUI TitleLabel;

		public Fleet SingleFleet
		{
			get
			{
				if (Fleets != null && Fleets.Count() == 1)
				{
					return Fleets.First();
				}
				return null;
			}
		}

		protected override void awake()
		{
			base.awake();
			AdvancedOrdersController.ChooseHomeBaseButton.onClick.AddListener(ChooseHomeUnitButtonClick);
			SetAsDefaultsButton.onClick.AddListener(SetAsDefaultsButtonClick);
			UseDefaultsButton.onClick.AddListener(UseDefaultsButtonClick);
			AdvancedOrdersController.SettingValueChanged += OnSettingValueChanged;
			StanceSettingsController.SettingValueChanged += OnSettingValueChanged;
			ApplyToAllFleetsButton.onClick.AddListener(ApplyToAllFleetsButtonClick);
		}

		private void OnSettingValueChanged()
		{
			RefreshUseDefaultsEnabled();
			RefreshSetAsDefaultsButtonEnabled();
		}

		protected override void refresh()
		{
			base.refresh();
			AdvancedOrdersController.Init(Fleets);
			StanceSettingsController.Init(Fleets);
			TitleLabel.text = GetFleetNameText();
			RefreshUseDefaultsEnabled();
			RefreshSetAsDefaultsButtonEnabled();
		}

		protected override void update()
		{
			base.update();
			ApplyToAllFleetsButton.interactable = SingleFleet != null && EngineASX.Instance.LocalFaction.Fleets.Count > 1;
		}

		public void RefreshSetAsDefaultsButtonEnabled()
		{
			SetAsDefaultsButton.interactable = SingleFleet != null && (EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings == null || AnyFleetHasDifferentSettingsToDefaults());
		}

		public void RefreshUseDefaultsEnabled()
		{
			UseDefaultsButton.interactable = EngineASX.Instance.CachedFleetSettingsController.HasDefaultFleetSettings && AnyFleetHasDifferentSettingsToDefaults();
		}

		private bool AnyFleetHasDifferentSettingsToDefaults()
		{
			if (Fleets == null)
			{
				return true;
			}
			foreach (Fleet fleet in Fleets)
			{
				if (!CachedFleetSettingsController.CreateFromFleet(fleet).IsSameAs(EngineASX.Instance.CachedFleetSettingsController.DefaultFleetSettings))
				{
					return true;
				}
			}
			return false;
		}

		private void ApplyToAllFleetsButtonClick()
		{
			HashSet<Fleet> hashSet = new HashSet<Fleet>(Fleets);
			CachedFleetSettingsItem cachedSettings = CachedFleetSettingsController.CreateFromFleet(SingleFleet);
			foreach (Fleet fleet in EngineASX.Instance.LocalFaction.Fleets)
			{
				if (!hashSet.Contains(fleet))
				{
					CachedFleetSettingsController.ApplyToFleet(cachedSettings, fleet);
					fleet.SetFormationPositions();
				}
			}
			Refresh();
			UIController.Instance.ShowMessageBox("All fleets have been updated");
		}

		private void SetAsDefaultsButtonClick()
		{
			EngineASX.Instance.CachedFleetSettingsController.SetDefaultFleetSettings(SingleFleet);
			RefreshUseDefaultsEnabled();
			RefreshSetAsDefaultsButtonEnabled();
		}

		private void UseDefaultsButtonClick()
		{
			foreach (Fleet fleet in Fleets)
			{
				EngineASX.Instance.CachedFleetSettingsController.RestoreFleetSettingsToDefault(fleet);
				fleet.SetFormationPositions();
			}
			Refresh();
		}

		private string GetFleetNameText()
		{
			if (Fleets.Count() == 1)
			{
				return Fleets.First().GetFriendlyName();
			}
			if (Fleets.Count() < 4)
			{
				return string.Join(", ", Fleets.Select((Fleet e) => e.GetFriendlyName()));
			}
			return "[Multiple fleets]";
		}

		private void ChooseHomeUnitButtonClick()
		{
			ChangeFleetHomeBase.Change(Fleets, () =>
			{
				UIController.Instance.ScreenNavigator.NavigateBackTo(this);
				Refresh();
			});
		}
	}
}
