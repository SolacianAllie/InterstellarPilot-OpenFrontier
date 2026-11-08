using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud.TargetInfo;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class UnitInfoUI : MonoBehaviour
	{
		private float lastTimeUpdatedSpeedLabel;

		private float lastTimeRefreshStatic;

		public SectorDisplayIcons SectorDisplayIcons;

		public UnitConstructionControllerUI UnitConstructionController;

		public UnitPathIconsDisplay UnitPathIconsDisplay;

		private int cargoLastCount;

		public TextMeshProUGUI ClassLabel;

		public TextMeshProUGUI DistanceLabel;

		private EngineASX engine;

		private Unit foreignUnit;

		public GameObject InfoRoot;

		private bool isAwake;

		private float lastUnitSpeedValue;

		public Unit LocalUnit;

		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI JumpGateNameLabel;

		private bool showingShipNameInLabel;

		public TextMeshProUGUI SpeedLabel;

		public GameObject SpeedRoot;

		private float speedValueLerpFaction = 10f;

		public string UnknownText = "N/a";

		public TargetMissileLockController TargetMissileLockController;

		public Unit ForeignUnit
		{
			get
			{
				return foreignUnit;
			}
			set
			{
				if (!(foreignUnit != value))
				{
					return;
				}
				foreignUnit = value;
				lastUnitSpeedValue = -1f;
				cargoLastCount = -1;
				Refresh();
				if (foreignUnit != null)
				{
					if (foreignUnit.CargoComponent != null)
					{
						cargoLastCount = foreignUnit.CargoComponent.Quantity;
					}
					ClassLabel.gameObject.SetActive(ShouldShowClassLabel());
				}
				TargetMissileLockController.TargetUnit = foreignUnit;
			}
		}

		public void Refresh()
		{
			if (!isAwake)
			{
				Awake();
			}
			RefreshVisibility();
			if (foreignUnit != null)
			{
				RefreshStatic();
				RefreshVolatile();
			}
		}

		private void Awake()
		{
			engine = EngineASX.Instance;
			isAwake = true;
		}

		private void Start()
		{
			Refresh();
		}

		private void Update()
		{
			RefreshVisibility();
			if (foreignUnit != null)
			{
				RefreshVolatile();
			}
		}

		private float GetSpeedValue()
		{
			return Mathf.RoundToInt(foreignUnit.CurrentSpeed);
		}

		private void RefreshVisibility()
		{
			bool flag = foreignUnit != null;
			if (flag != InfoRoot.activeSelf)
			{
				InfoRoot.SetActive(flag);
			}
		}

		private bool ShouldShowClassLabel()
		{
			if (foreignUnit == null)
			{
				return false;
			}
			if (foreignUnit.UnitClass.UnitType == UnitType.Ship)
			{
				return foreignUnit.UnitClass.ShipType == ShipType.Normal;
			}
			// Stations get their service shown here too. An unnamed station used
			// to read "Outpost QQ3", so the type came free with the designation.
			// Now that every station carries a generated name, "Northwind
			// Laboratory" alone says nothing about what the place does, and the
			// ClassText slot below the name is already there and already laid
			// out - it was just hidden for anything that was not a ship.
			//
			// Bars are excluded because their name is already prefixed "Bar",
			// and StationPurpose.None covers waypoints and other non-stations
			// that borrow the station prefab.
			return foreignUnit.UnitType == UnitType.Station
				&& foreignUnit.UnitClass.StationPurpose != StationPurpose.None
				&& foreignUnit.UnitClass.StationPurpose != StationPurpose.Bar;
		}

		private void RefreshStatic()
		{
			lastTimeRefreshStatic = Time.time;
			if (!(foreignUnit != null))
			{
				return;
			}
			showingShipNameInLabel = false;
			ShowHideNameLabels(foreignUnit);
			UpdateNameLabel();
			RefreshSectorDisplayIcons();
			switch (foreignUnit.UnitClass.UnitType)
			{
			case UnitType.Ship:
				if (foreignUnit.Components != null && ClassLabel != null)
				{
					ClassLabel.text = foreignUnit.GetClassAndSeriesName();
					if (!showingShipNameInLabel && !string.IsNullOrEmpty(foreignUnit.Components.ShipName))
					{
						ClassLabel.text += $" \"{foreignUnit.Components.ShipName}\"";
					}
				}
				break;
			case UnitType.Station:
				// The service, e.g. "Laboratory", "Shipyard", "Trade Station".
				// Same source the old designation used, so the wording matches
				// what an unnamed station used to say.
				if (ClassLabel != null)
				{
					ClassLabel.text = foreignUnit.GetClassAndSeriesName();
				}
				break;
			case UnitType.Cargo:
				cargoLastCount = foreignUnit.CargoComponent.Quantity;
				break;
			}
			RefreshSpeedRootVisible();
			RefreshSpeedLabel();
		}

		private void RefreshSpeedRootVisible()
		{
			SpeedRoot.gameObject.SetActive(!foreignUnit.IsStatic && !foreignUnit.IsUnderConstructionOrDismantling);
		}

		private void RefreshSectorDisplayIcons()
		{
			Sector foreignUnitDiscoveredTargetSector = UnitInfoHelper.GetForeignUnitDiscoveredTargetSector(foreignUnit);
			SectorDisplayIcons.gameObject.SetActive(foreignUnitDiscoveredTargetSector != null);
			if (foreignUnitDiscoveredTargetSector != null)
			{
				SectorDisplayIcons.Sector = foreignUnitDiscoveredTargetSector;
				SectorDisplayIcons.Refresh();
			}
		}

		public void Invalidate()
		{
			lastTimeRefreshStatic = -1f;
		}

		private bool NeedsToRefreshStatic()
		{
			if (foreignUnit == null)
			{
				return false;
			}
			if (Time.time - lastTimeRefreshStatic > 1.5f)
			{
				return true;
			}
			if (foreignUnit.CargoComponent != null && foreignUnit.CargoComponent.Quantity != cargoLastCount)
			{
				return true;
			}
			return false;
		}

		private void RefreshVolatile()
		{
			if (LocalUnit != null && foreignUnit != null && foreignUnit.ActiveUnit != null)
			{
				UnitPathIconsDisplay.Unit = foreignUnit;
				UnitConstructionController.Unit = foreignUnit;
				UnitConstructionController.Refresh();
				RefreshSpeedRootVisible();
				if (NeedsToRefreshStatic())
				{
					RefreshStatic();
				}
				SetNameLabelColor();
				if (DistanceLabel != null)
				{
					float dist = Vector3.Distance(LocalUnit.transform.position, foreignUnit.transform.position);
					DistanceLabel.SetText(TextFormattingHelper.FormatDistanceNonAlloc(dist));
				}
				RefreshSpeedLabelPeriodically();
			}
		}

		private void RefreshSpeedLabelPeriodically()
		{
			if (SpeedLabel != null && Time.time > lastTimeUpdatedSpeedLabel + 0.3f)
			{
				RefreshSpeedLabel();
			}
		}

		private void RefreshSpeedLabel()
		{
			SpeedLabel.SetText(TextFormattingHelper.FormatSpeedNonAlloc(GetSpeedValue()));
			lastTimeUpdatedSpeedLabel = Time.time;
		}

		private void ShowHideNameLabels(Unit foreignUnit)
		{
			JumpGateNameLabel.gameObject.SetActive(foreignUnit.UnitType == UnitType.Wormhole);
			NameLabel.gameObject.SetActive(foreignUnit.UnitType != UnitType.Wormhole);
		}

		private void SetNameLabelColor()
		{
			GetNameLabelForUnit(foreignUnit).color = UnitInfoHelper.GetUnitDisplayColor(foreignUnit, LocalUnit.Faction);
		}

		private void UpdateNameLabel()
		{
			TextMeshProUGUI nameLabelForUnit = GetNameLabelForUnit(foreignUnit);
			bool preferShortFactionName = foreignUnit.Faction == null || foreignUnit.Faction.FactionType != FactionType.Bar;
			bool preferShortUnitName = false;
			if (foreignUnit.ClassName.Length > 16)
			{
				preferShortUnitName = true;
			}
			nameLabelForUnit.text = UnitNamer.GetNameWithFactionAndFleet(EngineASX.Instance.LocalFaction, foreignUnit, engine.World.UsePilotNamesAsDesignations, preferShortUnitName, preferShortFactionName, out showingShipNameInLabel);
		}

		public TextMeshProUGUI GetNameLabelForUnit(Unit unit)
		{
			if (unit.UnitType == UnitType.Wormhole)
			{
				return JumpGateNameLabel;
			}
			return NameLabel;
		}
	}
}
