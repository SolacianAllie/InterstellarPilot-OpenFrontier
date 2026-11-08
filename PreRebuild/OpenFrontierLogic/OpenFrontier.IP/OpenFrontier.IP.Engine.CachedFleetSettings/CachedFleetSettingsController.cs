using System.Collections.Generic;
using OpenFrontier.IP.Engine.Fleets.FleetFormations;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CachedFleetSettings
{
	public class CachedFleetSettingsController
	{
		private Dictionary<int, CachedFleetSettingsItem> cachedSettingsByUnit = new Dictionary<int, CachedFleetSettingsItem>(8);

		private CachedFleetSettingsItem defaultFleetSettings;

		public bool HasDefaultFleetSettings => defaultFleetSettings != null;

		public CachedFleetSettingsItem DefaultFleetSettings
		{
			get
			{
				return defaultFleetSettings;
			}
			set
			{
				defaultFleetSettings = value;
			}
		}

		public IEnumerable<KeyValuePair<int, CachedFleetSettingsItem>> Items => cachedSettingsByUnit;

		public void SetDefaultFleetSettings(Fleet fleet)
		{
			defaultFleetSettings = CreateFromFleet(fleet);
			if (EngineASX.Instance.LocalFaction != null)
			{
				FleetFormation fleetFormationById = EngineASX.Instance.GetFleetFormationById(defaultFleetSettings.FormationId);
				if (fleetFormationById != null)
				{
					EngineASX.Instance.LocalFaction.PreferredFormationStyle = fleetFormationById.FormationStyle;
				}
			}
		}

		public void RestoreFleetSettingsToDefault(Fleet fleet)
		{
			if (HasDefaultFleetSettings)
			{
				ApplyToFleet(defaultFleetSettings, fleet);
			}
		}

		public void CacheForUnit(Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet != null)
			{
				CachedFleetSettingsItem value = CreateFromFleet(fleet);
				cachedSettingsByUnit[unit.UniqueId] = value;
			}
		}

		public static CachedFleetSettingsItem CreateFromFleet(Fleet fleet)
		{
			CachedFleetSettingsItem cachedFleetSettingsItem = new CachedFleetSettingsItem();
			cachedFleetSettingsItem.FleetSettings = SaveGameModelExporter.ExportFleetSettings(fleet.Settings);
			cachedFleetSettingsItem.FormationId = SaveGameModelExporter.ExportFleetFormationId(fleet);
			cachedFleetSettingsItem.FleetSettings.PlayerFleetSettings = SaveGameModelExporter.ExportPlayerFleetSettings(fleet);
			if (fleet.IsHomeBaseValid)
			{
				cachedFleetSettingsItem.HomeBase = fleet.HomeBase.Copy();
			}
			return cachedFleetSettingsItem;
		}

		public void SetItem(Unit unit, CachedFleetSettingsItem item)
		{
			cachedSettingsByUnit[unit.UniqueId] = item;
		}

		public void ForgetForUnit(Unit unit)
		{
			cachedSettingsByUnit.Remove(unit.UniqueId);
		}

		public CachedFleetSettingsItem GetForUnit(Unit unit)
		{
			if (cachedSettingsByUnit.TryGetValue(unit.UniqueId, out var value))
			{
				return value;
			}
			return null;
		}

		public static void ApplyToFleet(CachedFleetSettingsItem cachedSettings, Fleet fleet)
		{
			SaveGameModelImporter170.ImportFleetSettings(fleet.Settings, cachedSettings.FleetSettings);
			SaveGameModelImporter170.ImportPlayerFleetSettings(cachedSettings.FleetSettings.PlayerFleetSettings, fleet.Settings);
			fleet.FleetFormation = EngineASX.Instance.GetFleetFormationById(cachedSettings.FormationId);
			if (cachedSettings.HomeBase != null)
			{
				fleet.SetHomeBase(cachedSettings.HomeBase.Copy());
			}
			else
			{
				fleet.SetHomeBase(null);
			}
		}

		public void ApplyDefaultSettingsToFleet(Fleet fleet)
		{
			if (defaultFleetSettings != null)
			{
				ApplyToFleet(defaultFleetSettings, fleet);
			}
		}

		public void ApplyCachedUnitFleetSettings(Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet != null)
			{
				CachedFleetSettingsItem forUnit = GetForUnit(unit);
				if (forUnit != null)
				{
					ApplyToFleet(forUnit, fleet);
				}
			}
			else
			{
				Debug.LogError("Expecting unit " + unit.GetFriendlyName() + " to have a fleet");
			}
		}
	}
}
