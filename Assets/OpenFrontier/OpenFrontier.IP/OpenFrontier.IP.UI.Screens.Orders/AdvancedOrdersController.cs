using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.UI.Screens.FleetFormations;
using OpenFrontier.IP.UI.Screens.FleetSettings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Orders
{
	public class AdvancedOrdersController : MonoBehaviour
	{
		public Button ClearHomeBaseButton;

		public Button FormationStyleButton;

		public Image FormationStyleImage;

		public TextMeshProUGUI FleetFormationStyleText;

		public Slider FormationTightnessSlider;

		public Toggle PreferToDockDontCareToggle;

		public Toggle PreferToDockAlwaysDockToggle;

		public Toggle PreferToDockAlwaysUndockToggle;

		public Toggle CollectCargo_AmmoToggle;

		public Toggle CollectCargo_TradableToggle;

		public Toggle CollectCargo_IncompatibleAmmoToggle;

		public Toggle PreferToCloakToggle;

		public Toggle NotifyWhenOrderCompleteToggle;

		public Toggle NotifyHostileTargetScannedToggle;

		public Toggle NotifyAbandonedUnitScannedToggle;

		public Toggle NotifyWhenAbandonedCargoFoundToggle;

		public TextMeshProUGUI NotifyWhenOrderCompleteLabel;

		public TextMeshProUGUI NotifyHostileTargetScannedLabel;

		public TextMeshProUGUI NotifyAbandonedUnitScannedLabel;

		public TextMeshProUGUI NotifyWhenAbandonedCargoFoundLabel;

		public Button ChooseHomeBaseButton;

		private IEnumerable<Fleet> fleets;

		public Slider MaxJumpsSlider;

		public Text MaxJumpsText;

		public Text HomeBaseText;

		public GameObject MaxJumpsRoot;

		private float lastTimeAdjustFormationTightness;

		private bool adjustFormationTightness;

		public TextMeshProUGUI PreferToCloakLabel;

		public TextMeshProUGUI FormationTightnessLabel;

		private bool hasAddedListeners;

		public Fleet SingleFleet
		{
			get
			{
				if (fleets.Count() == 1)
				{
					return fleets.First();
				}
				return null;
			}
		}

		public IEnumerable<Fleet> Fleets
		{
			get
			{
				return fleets;
			}
			set
			{
				fleets = value;
			}
		}

		public event SettingValueChangedHandler SettingValueChanged;

		public void Init(IEnumerable<Fleet> fleets)
		{
			RemoveListeners();
			Fleets = fleets;
			MaxJumpsSlider.wholeNumbers = true;
			MaxJumpsSlider.minValue = 0f;
			MaxJumpsSlider.maxValue = 10f;
			RefreshControls();
			AddListeners();
			Refresh();
		}

		private void RaiseValueChanged()
		{
			if (SettingValueChanged != null)
			{
				SettingValueChanged();
			}
		}

		private void RemoveListeners()
		{
			MaxJumpsSlider.onValueChanged.RemoveAllListeners();
			PreferToDockDontCareToggle.onValueChanged.RemoveAllListeners();
			PreferToDockAlwaysDockToggle.onValueChanged.RemoveAllListeners();
			PreferToDockAlwaysUndockToggle.onValueChanged.RemoveAllListeners();
			CollectCargo_AmmoToggle.onValueChanged.RemoveAllListeners();
			CollectCargo_TradableToggle.onValueChanged.RemoveAllListeners();
			CollectCargo_IncompatibleAmmoToggle.onValueChanged.RemoveAllListeners();
			PreferToCloakToggle.onValueChanged.RemoveAllListeners();
			NotifyWhenOrderCompleteToggle.onValueChanged.RemoveAllListeners();
			NotifyHostileTargetScannedToggle.onValueChanged.RemoveAllListeners();
			NotifyAbandonedUnitScannedToggle.onValueChanged.RemoveAllListeners();
			NotifyWhenAbandonedCargoFoundToggle.onValueChanged.RemoveAllListeners();
			FormationTightnessSlider.onValueChanged.RemoveAllListeners();
			FormationStyleButton.onClick.RemoveAllListeners();
		}

		private void AddListeners()
		{
			ClearHomeBaseButton.onClick.AddListener(ClearHomeBaseButtonClick);
			MaxJumpsSlider.onValueChanged.AddListener(MaxJumpsSliderValueChanged);
			PreferToDockDontCareToggle.onValueChanged.AddListener((bool value) =>
			{
				OnPreferToDockToggleChanged(value, DockedPreference.DontCare);
			});
			PreferToDockAlwaysDockToggle.onValueChanged.AddListener((bool value) =>
			{
				OnPreferToDockToggleChanged(value, DockedPreference.Dock);
			});
			PreferToDockAlwaysUndockToggle.onValueChanged.AddListener((bool value) =>
			{
				OnPreferToDockToggleChanged(value, DockedPreference.Undock);
			});
			CollectCargo_AmmoToggle.onValueChanged.AddListener((bool value) =>
			{
				OnCollectCargoToggleChanged(value, FleetCargoCollectionPreference.CompatibleEquipment);
			});
			CollectCargo_TradableToggle.onValueChanged.AddListener((bool value) =>
			{
				OnCollectCargoToggleChanged(value, FleetCargoCollectionPreference.TradableCargo);
			});
			CollectCargo_IncompatibleAmmoToggle.onValueChanged.AddListener((bool value) =>
			{
				OnCollectCargoToggleChanged(value, FleetCargoCollectionPreference.IncompatibleEquipment);
			});
			PreferToCloakToggle.onValueChanged.AddListener(PreferToCloakToggleValueChanged);
			NotifyWhenOrderCompleteToggle.onValueChanged.AddListener(NotifyWhenOrderCompleteToggleValueChanged);
			NotifyHostileTargetScannedToggle.onValueChanged.AddListener(NotifyHostileTargetScannedToggleValueChanged);
			NotifyAbandonedUnitScannedToggle.onValueChanged.AddListener(NotifyAbandonedUnitScannedToggleValueChanged);
			NotifyWhenAbandonedCargoFoundToggle.onValueChanged.AddListener(NotifyWhenAbandonedCargoFoundToggleValueChanged);
			FormationTightnessSlider.onValueChanged.AddListener(FormationTightnessSliderValueChanged);
			FormationStyleButton.onClick.AddListener(FormationStyleButtonClick);
		}

		private void NotifyWhenOrderCompleteToggleValueChanged(bool value)
		{
			foreach (Fleet fleet in fleets)
			{
				fleet.Settings.NotifyWhenOrderComplete = value;
			}
			RaiseValueChanged();
			Refresh();
		}

		private void NotifyHostileTargetScannedToggleValueChanged(bool value)
		{
			foreach (Fleet fleet in fleets)
			{
				fleet.Settings.NotifyWhenScannedHostile = value;
			}
			RaiseValueChanged();
			Refresh();
		}

		private void NotifyAbandonedUnitScannedToggleValueChanged(bool value)
		{
			foreach (Fleet fleet in fleets)
			{
				fleet.Settings.NotifyWhenAbandonedUnitFound = value;
			}
			RaiseValueChanged();
			Refresh();
		}

		private void NotifyWhenAbandonedCargoFoundToggleValueChanged(bool value)
		{
			foreach (Fleet fleet in fleets)
			{
				fleet.Settings.NotifyWhenAbandonedCargoFound = value;
			}
			RaiseValueChanged();
			Refresh();
		}

		private void PreferToCloakToggleValueChanged(bool value)
		{
			foreach (Fleet fleet in fleets)
			{
				fleet.Settings.PreferCloak = value;
			}
			RaiseValueChanged();
			Refresh();
		}

		private void OnPreferToDockToggleChanged(bool value, DockedPreference dockedPreference)
		{
			if ((fleets != null) & value)
			{
				foreach (Fleet fleet in fleets)
				{
					if (fleet != null)
					{
						fleet.Settings.PreferToDock = dockedPreference;
						if (fleet.ActiveOrder != null)
						{
							fleet.ActiveOrder.ResetTargetPosition();
						}
					}
				}
				RefreshPreferToDockToggles();
				Refresh();
			}
			RaiseValueChanged();
		}

		private void OnCollectCargoToggleChanged(bool value, FleetCargoCollectionPreference fleetCargoCollectionPreference)
		{
			FleetCargoCollectionPreference fleetCargoCollectionPreference2 = FleetCargoCollectionPreference.Nothing;
			if (CollectCargo_AmmoToggle.isOn)
			{
				fleetCargoCollectionPreference2 |= FleetCargoCollectionPreference.CompatibleEquipment;
			}
			if (CollectCargo_IncompatibleAmmoToggle.isOn)
			{
				fleetCargoCollectionPreference2 |= FleetCargoCollectionPreference.IncompatibleEquipment;
			}
			if (CollectCargo_TradableToggle.isOn)
			{
				fleetCargoCollectionPreference2 |= FleetCargoCollectionPreference.TradableCargo;
			}
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.Settings.CargoCollectionPreference = fleetCargoCollectionPreference2;
				}
			}
			RaiseValueChanged();
		}

		private void FormationStyleButtonClick()
		{
			Fleet singleFleet = SingleFleet;
			UIController.Instance.ScreenNavigator.ShowFleetFormationStylePickerScreen((singleFleet != null && singleFleet.FleetFormation != null) ? singleFleet.FleetFormation.FormationStyle : null, (FleetFormationStylePickerScreen sender, FleetFormationStyle formationStyle) =>
			{
				if (formationStyle != null)
				{
					FleetFormation fleetFormationById = EngineASX.Instance.GetFleetFormationById(formationStyle.UniqueId);
					foreach (Fleet fleet in fleets)
					{
						if (fleetFormationById != fleet.FleetFormation)
						{
							fleet.FleetFormation = fleetFormationById;
							fleet.SetFormationPositions();
						}
					}
					RaiseValueChanged();
					Refresh();
				}
				sender.NavigateBack();
			});
		}

		private void FormationTightnessSliderValueChanged(float value)
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.Settings.FormationTightness = value;
					fleet.SetFormationPositions();
					lastTimeAdjustFormationTightness = Time.time;
					adjustFormationTightness = true;
				}
			}
			RaiseValueChanged();
			RefreshFormationTightnessLabel();
		}

		private void ClearHomeBaseButtonClick()
		{
			foreach (Fleet fleet in fleets)
			{
				fleet.SetHomeBase(null);
			}
			RefreshVisibility();
			RaiseValueChanged();
		}

		public void Update()
		{
			if (fleets == null)
			{
				return;
			}
			RefreshVisibility();
			RefreshHomeUnitText();
			RefreshMaxJumpsText();
			if (!adjustFormationTightness || !(Time.time > lastTimeAdjustFormationTightness + 1f))
			{
				return;
			}
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.SetFormationPositions();
					adjustFormationTightness = false;
				}
			}
		}

		private void RefreshVisibility()
		{
			MaxJumpsRoot.SetActive(SingleFleet == null || SingleFleet.IsHomeBaseValid);
			MaxJumpsSlider.gameObject.SetActive(SingleFleet == null || SingleFleet.IsHomeBaseValid);
			ClearHomeBaseButton.interactable = CanClearHomeBase();
		}

		private bool CanClearHomeBase()
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet.IsHomeBaseValid)
				{
					return true;
				}
			}
			return false;
		}

		private void MaxJumpsSliderValueChanged(float value)
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					fleet.Settings.MaxJumpDistance = (int)value;
				}
			}
			RaiseValueChanged();
		}

		private void RefreshMaxJumpsText()
		{
			MaxJumpsText.text = GetMaxJumpsText();
		}

		private SectorTarget GetSingleHomeBase()
		{
			SectorTarget sectorTarget = null;
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null && fleet.IsHomeBaseValid)
				{
					if (sectorTarget == null)
					{
						sectorTarget = fleet.HomeBase;
					}
					else if (!sectorTarget.IsSameTargetAs(fleet.HomeBase))
					{
						return null;
					}
				}
			}
			return sectorTarget;
		}

		private int? GetSingleMaxJumpDistanceValue()
		{
			int? num = null;
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null)
				{
					if (!num.HasValue)
					{
						num = fleet.Settings.MaxJumpDistance;
					}
					else if (num != fleet.Settings.MaxJumpDistance)
					{
						return null;
					}
				}
			}
			return num;
		}

		private FleetFormationStyle GetSingleFleetFormationStyle()
		{
			FleetFormationStyle fleetFormationStyle = null;
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null && fleet.FleetFormation != null)
				{
					if (fleetFormationStyle == null)
					{
						fleetFormationStyle = fleet.FleetFormation.FormationStyle;
					}
					else if (fleetFormationStyle != fleet.FleetFormation.FormationStyle)
					{
						return null;
					}
				}
			}
			return fleetFormationStyle;
		}

		private string GetMaxJumpsText()
		{
			Fleet singleFleet = SingleFleet;
			if (singleFleet != null)
			{
				return singleFleet.Settings.MaxJumpDistance.ToString();
			}
			Fleet firstFleet = fleets.First();
			if (fleets.All((Fleet e) => e.Settings.MaxJumpDistance == firstFleet.Settings.MaxJumpDistance))
			{
				return firstFleet.Settings.MaxJumpDistance.ToString();
			}
			return "[Multiple]";
		}

		public void Refresh()
		{
			if (fleets != null && fleets.Any())
			{
				Fleet fleet = fleets.First();
				RefreshMaxJumpsText();
				RefreshHomeUnitText();
				RefreshVisibility();
				if (fleet.FleetFormation != null)
				{
					FormationStyleImage.sprite = fleet.FleetFormation.FormationStyle.Sprite;
				}
				FleetFormationStyleText.text = GetFleetFormationStyleText();
				RefreshPreferToCloakLabel();
				RefreshFormationTightnessLabel();
				RefreshNotifyWhenOrderCompleteLabel();
				RefreshNotifyHostileTargetScannedLabel();
				RefreshNotifyAbandonedUnitScannedLabel();
				RefreshNotifyWhenAbandonedCargoFoundLabel();
			}
		}

		private void RefreshPreferToDockToggles()
		{
			Fleet singleFleet = SingleFleet;
			Fleet firstFleet = fleets.First();
			if (singleFleet != null || fleets.All((Fleet e) => e.Settings.PreferToDock == firstFleet.Settings.PreferToDock))
			{
				PreferToDockDontCareToggle.isOn = firstFleet.Settings.PreferToDock == DockedPreference.DontCare;
				PreferToDockAlwaysDockToggle.isOn = firstFleet.Settings.PreferToDock == DockedPreference.Dock;
				PreferToDockAlwaysUndockToggle.isOn = firstFleet.Settings.PreferToDock == DockedPreference.Undock;
			}
		}

		private void RefreshControls()
		{
			if (fleets != null && fleets.Any())
			{
				Fleet singleFleet = SingleFleet;
				Fleet firstFleet = fleets.First();
				MaxJumpsSlider.value = firstFleet.Settings.MaxJumpDistance;
				PreferToCloakToggle.isOn = firstFleet.Settings.PreferCloak;
				RefreshPreferToDockToggles();
				if (singleFleet != null || fleets.All((Fleet e) => e.Settings.CargoCollectionPreference == firstFleet.Settings.CargoCollectionPreference))
				{
					CollectCargo_AmmoToggle.isOn = (firstFleet.Settings.CargoCollectionPreference & FleetCargoCollectionPreference.CompatibleEquipment) != 0;
					CollectCargo_TradableToggle.isOn = (firstFleet.Settings.CargoCollectionPreference & FleetCargoCollectionPreference.TradableCargo) != 0;
					CollectCargo_IncompatibleAmmoToggle.isOn = (firstFleet.Settings.CargoCollectionPreference & FleetCargoCollectionPreference.IncompatibleEquipment) != 0;
				}
				NotifyWhenOrderCompleteToggle.isOn = firstFleet.Settings.NotifyWhenOrderComplete;
				NotifyHostileTargetScannedToggle.isOn = firstFleet.Settings.NotifyWhenScannedHostile;
				NotifyAbandonedUnitScannedToggle.isOn = firstFleet.Settings.NotifyWhenAbandonedUnitFound;
				NotifyWhenAbandonedCargoFoundToggle.isOn = firstFleet.Settings.NotifyWhenAbandonedCargoFound;
				FormationTightnessSlider.value = firstFleet.Settings.FormationTightness;
				RefreshVisibility();
			}
		}

		private void RefreshFormationTightnessLabel()
		{
			FormationTightnessLabel.text = GetFormationTightnessLabel();
		}

		private string GetFormationTightnessLabel()
		{
			float firstValue = fleets.First().Settings.FormationTightness;
			if (fleets.Where((Fleet e) => e != null).All((Fleet e) => e.Settings.FormationTightness == firstValue))
			{
				return string.Empty;
			}
			return "[Multiple]";
		}

		private void RefreshPreferToCloakLabel()
		{
			PreferToCloakLabel.text = GetPreferToCloakLabel();
		}

		private string GetPreferToCloakLabel()
		{
			bool firstValue = fleets.First().Settings.PreferCloak;
			if (fleets.Where((Fleet e) => e != null).All((Fleet e) => e.Settings.PreferCloak == firstValue))
			{
				return string.Empty;
			}
			return "[Multiple]";
		}

		private bool AnyFleetHasFormationStyle()
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null && fleet.FleetFormation != null)
				{
					return true;
				}
			}
			return false;
		}

		private string GetFleetFormationStyleText()
		{
			if (fleets.Count() > 1)
			{
				if (AnyFleetHasFormationStyle())
				{
					FleetFormationStyle singleFleetFormationStyle = GetSingleFleetFormationStyle();
					if (!(singleFleetFormationStyle != null))
					{
						return "[Multiple]";
					}
					return singleFleetFormationStyle.Name;
				}
				return "[None]";
			}
			Fleet fleet = fleets.FirstOrDefault();
			if (fleet != null)
			{
				if (!(fleet != null) || fleet.FleetFormation == null)
				{
					return "[None]";
				}
				return fleet.FleetFormation.FormationStyle.Name;
			}
			return null;
		}

		private void RefreshHomeUnitText()
		{
			HomeBaseText.text = GetHomeBaseText();
		}

		private string GetHomeBaseText()
		{
			Fleet singleFleet = SingleFleet;
			if (singleFleet != null && singleFleet.IsHomeBaseValid)
			{
				return GetHomeBaseText(singleFleet.HomeBase);
			}
			if (fleets != null && AnyFleetHasHomeBase())
			{
				SectorTarget singleHomeBase = GetSingleHomeBase();
				if (singleHomeBase != null)
				{
					return GetHomeBaseText(singleHomeBase);
				}
				return "[Multiple]";
			}
			return "[None]";
		}

		private void RefreshNotifyWhenOrderCompleteLabel()
		{
			NotifyWhenOrderCompleteLabel.text = GetNotifyWhenOrderCompleteLabel();
		}

		private string GetNotifyWhenOrderCompleteLabel()
		{
			bool firstValue = fleets.First().Settings.NotifyWhenOrderComplete;
			if (!fleets.All((Fleet e) => e.Settings.NotifyWhenOrderComplete == firstValue))
			{
				return "[Multiple]";
			}
			return string.Empty;
		}

		private void RefreshNotifyHostileTargetScannedLabel()
		{
			NotifyHostileTargetScannedLabel.text = GetNotifyHostileTargetScannedLabel();
		}

		private string GetNotifyHostileTargetScannedLabel()
		{
			bool firstValue = fleets.First().Settings.NotifyWhenScannedHostile;
			if (!fleets.All((Fleet e) => e.Settings.NotifyWhenScannedHostile == firstValue))
			{
				return "[Multiple]";
			}
			return string.Empty;
		}

		private void RefreshNotifyAbandonedUnitScannedLabel()
		{
			NotifyAbandonedUnitScannedLabel.text = GetNotifyAbandonedUnitScannedLabel();
		}

		private string GetNotifyAbandonedUnitScannedLabel()
		{
			bool firstValue = fleets.First().Settings.NotifyWhenAbandonedUnitFound;
			if (!fleets.All((Fleet e) => e.Settings.NotifyWhenAbandonedUnitFound == firstValue))
			{
				return "[Multiple]";
			}
			return string.Empty;
		}

		private void RefreshNotifyWhenAbandonedCargoFoundLabel()
		{
			NotifyWhenAbandonedCargoFoundLabel.text = GetNotifyWhenAbandonedCargoFoundLabel();
		}

		private string GetNotifyWhenAbandonedCargoFoundLabel()
		{
			bool firstValue = fleets.First().Settings.NotifyWhenAbandonedCargoFound;
			if (!fleets.All((Fleet e) => e.Settings.NotifyWhenAbandonedCargoFound == firstValue))
			{
				return "[Multiple]";
			}
			return string.Empty;
		}

		private bool AnyFleetHasHomeBase()
		{
			foreach (Fleet fleet in fleets)
			{
				if (fleet != null && fleet.IsHomeBaseValid)
				{
					return true;
				}
			}
			return false;
		}

		private string GetHomeBaseText(SectorTarget sectorTarget)
		{
			if (sectorTarget.TargetUnit != null)
			{
				return $"{sectorTarget.TargetUnit.GetFriendlyNameAndFactionShortName()}, {sectorTarget.TargetUnit.Sector.Name}";
			}
			return TextFormattingHelper.FormatSectorAndPosition(sectorTarget.GetTargetSector(), sectorTarget.GetTargetSectorPosition());
		}
	}
}
