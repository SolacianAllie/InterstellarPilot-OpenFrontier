using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Testing.Spawning;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.BuildMode;
using OpenFrontier.IP.UI.Screens.MessageBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class SpawnUnitTypeOptions : MonoBehaviour
	{
		public Toggle CreateFleetToggle;

		public UnitType UnitTypeFilter = UnitType.Ship;

		public float Spacing = 40f;

		public Toggle SpawnOwnershipPlayerToggle;

		public Toggle SpawnOwnershipBanditsToggle;

		public Toggle SpawnOwnershipAbandonedToggle;

		public Toggle SpawnOwnershipCurrentTargetToggle;

		public Transform ButtonsTransform;

		public Button ButtonPrefab;

		public Slider CountSlider;

		public TextMeshProUGUI CountSliderText;

		public SpawnOwnership SpawnOwnership
		{
			get
			{
				if (SpawnOwnershipAbandonedToggle.isOn)
				{
					return SpawnOwnership.Abandoned;
				}
				if (SpawnOwnershipPlayerToggle.isOn)
				{
					return SpawnOwnership.Player;
				}
				if (SpawnOwnershipCurrentTargetToggle.isOn)
				{
					return SpawnOwnership.CurrentTarget;
				}
				if (SpawnOwnershipBanditsToggle.isOn)
				{
					return SpawnOwnership.Bandits;
				}
				return SpawnOwnership.Unknown;
			}
		}

		public int SpawnUnitCount => (int)CountSlider.value;

		private void Awake()
		{
			CreateButtons();
			CountSliderText.text = ((int)CountSlider.value).ToString();
			CountSlider.onValueChanged.AddListener((float value) =>
			{
				CountSliderText.text = ((int)CountSlider.value).ToString();
			});
		}

		private void CreateButtons()
		{
			foreach (UnitClass unitClass in GetSpawnableUnitClasses())
			{
				CreateButtonForUnitClass(unitClass).onClick.AddListener(() =>
				{
					string text = ValidateSpawnUnitClass(unitClass, SpawnUnitCount);
					if (!string.IsNullOrWhiteSpace(text))
					{
						UIController.Instance.ShowMessageBox(text, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
					}
					else
					{
						List<Unit> list = TrySpawnUnitClass(unitClass, out var spawnFaction);
						if (list.Count > 0)
						{
							string text2 = null;
							text2 = ((!(spawnFaction == null)) ? $"{list.Count}x {unitClass.GetClassAndSeriesName()} spawned in current sector for {spawnFaction.GetShortNameElseLong()} ({spawnFaction.UniqueId})" : $"{list.Count}x abandoned {unitClass.GetClassAndSeriesName()} spawned");
							UIController.Instance.ShowMessageBox(text2, MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
						}
						else
						{
							UIController.Instance.ShowError("Failed to spawn any units");
						}
					}
				});
			}
		}

		private List<Unit> TrySpawnUnitClass(UnitClass unitClass, out Faction spawnFaction)
		{
			spawnFaction = null;
			List<Unit> list = new List<Unit>();
			try
			{
				spawnFaction = GetSpawnFaction();
				Vector3 spawnSectorPositionFromLocalUnit = SpawnUtils.GetSpawnSectorPositionFromLocalUnit(EngineASX.Instance.LocalUnit);
				for (int i = 0; i < SpawnUnitCount; i++)
				{
					Vector3 sectorPosition = spawnSectorPositionFromLocalUnit + Vector3.right * ((float)i * Spacing);
					Unit unit = SpawnUtils.SpawnUnit(unitClass.UnitPrefab, EngineASX.Instance.LocalPlayerSector, sectorPosition, spawnFaction, addCargoLoadout: true, isUnderConstruction: false, unitClass.UnitType != UnitType.Station || unitClass.StationPurpose != StationPurpose.SectorControl);
					if (unit != null)
					{
						list.Add(unit);
					}
				}
				if (CreateFleetToggle != null && CreateFleetToggle.isOn && spawnFaction != null)
				{
					if (spawnFaction.FactionAI != null)
					{
						spawnFaction.FactionAI.CreateFleetAndNpcPilotsForUnits(spawnFaction.FactionAI.GetFleetPrefab(), spawnFaction.FactionAI.GetPilotPrefab(), list);
					}
					else if (spawnFaction.IsPlayerFaction)
					{
						OrdersHelper.CreateFleetForUnits(EngineASX.Instance.LocalPlayerSector, spawnSectorPositionFromLocalUnit, list);
					}
					else
					{
						UIController.Instance.ShowMessageBox("Could not spawn a fleet for this faction", MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
					}
				}
			}
			catch (Exception ex)
			{
				UIController.Instance.ShowMessageBox(ex.Message, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				Debug.LogException(ex);
			}
			return list;
		}

		private string ValidateSpawnUnitClass(UnitClass unitClass, int count)
		{
			if (unitClass.UnitType == UnitType.Station && count > 1 && !BuildModeHelper.CanBuildMultiple(unitClass))
			{
				return "Cannot build multiple of this station type";
			}
			if (EngineASX.Instance.LocalUnit == null || EngineASX.Instance.ActiveSector == null)
			{
				return "Cannot determine location to spawn";
			}
			if (unitClass.StationPurpose == StationPurpose.SectorControl && EngineASX.Instance.ActiveSector.ControllingFaction != null)
			{
				return "Cannot spawn Sector HQ as sector is already controlled";
			}
			if (!EngineASX.Instance.CanSpawnUnitsInSector(EngineASX.Instance.LocalPlayerSector, unitClass.UnitType, count))
			{
				return "Cannot spawn: too many units in sector";
			}
			return null;
		}

		public Faction GetSpawnFaction()
		{
			switch (SpawnOwnership)
			{
			case SpawnOwnership.Abandoned:
				return null;
			case SpawnOwnership.Player:
				if (EngineASX.Instance.LocalFaction == null)
				{
					throw new Exception("No player faction");
				}
				return EngineASX.Instance.LocalFaction;
			case SpawnOwnership.Bandits:
			{
				Faction orCreateHostileWithAllBanditFaction = SpawnUtils.GetOrCreateHostileWithAllBanditFaction();
				if (orCreateHostileWithAllBanditFaction == null)
				{
					throw new Exception("Failed to get bandit faction");
				}
				return orCreateHostileWithAllBanditFaction;
			}
			case SpawnOwnership.CurrentTarget:
				if (EngineASX.Instance.Hud == null || EngineASX.Instance.Hud.CurrentTarget == null || EngineASX.Instance.Hud.CurrentTarget.Faction == null)
				{
					throw new Exception("No current target with faction");
				}
				return EngineASX.Instance.Hud.CurrentTarget.Faction;
			default:
				throw new Exception("Internal error: Unknown spawn ownership type");
			}
		}

		private Button CreateButtonForUnitClass(UnitClass unitClass)
		{
			Button button = UnityEngine.Object.Instantiate(ButtonPrefab);
			button.transform.SetParent(ButtonsTransform);
			button.transform.localScale = Vector3.one;
			button.SetText($"{unitClass.GetClassAndSeriesName()} (ID: {unitClass.UniqueID})");
			return button;
		}

		private IEnumerable<UnitClass> GetSpawnableUnitClasses()
		{
			return from e in EngineASX.Instance.UnitClasses
				where e.IsUsable && !e.ExcludeFromGodModeSpawn && e.UnitType == UnitTypeFilter && !e.IsBarebonesShip
				orderby (e.UnitSeries != null) ? e.UnitSeries.DisplayOrder : 0, e.SaleCost
				select e;
		}
	}
}
