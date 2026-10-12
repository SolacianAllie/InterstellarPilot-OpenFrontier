using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SectorDisplayIcons : MonoBehaviour
	{
		public Image UnitUnderAttackImage;

		public Image RestrictedNavigationImage;

		public Image PlanetImage;

		public Image AsteroidIconImage;

		public Image OwnedShipsImage;

		public Image OwnedStationsImage;

		public Image HostileShipsImage;

		public Image HostilesStationsImage;

		private Sector sector;

		public bool ShowSectorTypeImages = true;

		public bool ShowImages = true;

		public Sector Sector
		{
			get
			{
				return sector;
			}
			set
			{
				sector = value;
			}
		}

		public void Refresh()
		{
			RefreshStatic();
			RefreshVolatile();
		}

		public void RefreshStatic()
		{
			OwnedShipsImage.color = EngineASX.Instance.OwnedColor;
			OwnedStationsImage.color = EngineASX.Instance.OwnedColor;
			HostileShipsImage.color = EngineASX.Instance.HostilityColor;
			HostilesStationsImage.color = EngineASX.Instance.HostilityColor;
			RefreshAsteroidImage();
			RefreshPlanetImage();
		}

		private void RefreshRestrictedNavigationImage()
		{
			if (RestrictedNavigationImage == null)
			{
				Debug.LogError("Missing navigation image", this);
			}
			else if (sector == null)
			{
				RestrictedNavigationImage.gameObject.SetActive(value: false);
			}
			else
			{
				RestrictedNavigationImage.gameObject.SetActive(EngineASX.Instance.LocalFaction != null && EngineASX.Instance.LocalFaction.AutopilotExcludedSectors.Contains(sector.UniqueId));
			}
		}

		public void RefreshVolatile()
		{
			RefreshShipsInSectorImages();
			RefreshRestrictedNavigationImage();
		}

		private void RefreshPlanetImage()
		{
			if (!(PlanetImage != null))
			{
				return;
			}
			bool flag = ShowImages && ShowSectorTypeImages && sector != null && Sector.HasPlanets;
			PlanetImage.gameObject.SetActive(flag);
			if (!flag)
			{
				return;
			}
			List<Unit> unitsByType = Sector.GetUnitsByType(UnitType.Planet);
			if (unitsByType != null)
			{
				Unit unit = unitsByType.FirstOrDefault((Unit e) => e.UnitClass.PlanetClass != null);
				if (unit != null && unit.UnitClass.PlanetClass.PlanetSprite != null)
				{
					PlanetImage.sprite = unit.UnitClass.PlanetClass.PlanetSprite;
				}
			}
		}

		private void RefreshAsteroidImage()
		{
			if (ShowImages && sector != null && AsteroidIconImage != null && ShowSectorTypeImages)
			{
				List<Unit> unitsByType = Sector.GetUnitsByType(UnitType.AsteroidCluster);
				bool flag = unitsByType != null && unitsByType.Count > 0;
				AsteroidIconImage.gameObject.SetActive(flag);
				if (flag)
				{
					AsteroidType asteroidType = unitsByType[0].GetComponent<AsteroidCluster>().AsteroidType;
					if ((bool)asteroidType)
					{
						AsteroidIconImage.sprite = asteroidType.UniverseMapSectorTypeSprite;
					}
				}
			}
			else
			{
				AsteroidIconImage.gameObject.SetActive(value: false);
			}
		}

		private void RefreshShipsInSectorImages()
		{
			bool active = false;
			if (ShowImages && sector != null && ContainsPlayerShipsAndOutputHighestCombatRatingShip(Sector, out var unitClass, out var isPlayerShipUnderAttack))
			{
				OwnedShipsImage.sprite = unitClass.GetThumbnailIconSprite();
				OwnedShipsImage.gameObject.SetActive(value: true);
				if (isPlayerShipUnderAttack)
				{
					active = true;
				}
			}
			else
			{
				OwnedShipsImage.gameObject.SetActive(value: false);
			}
			if (ShowImages && sector != null && ContainsPlayerStationsAndOutputHighestCombatRatingShip(Sector, out var unitClass2, out var isPlayerStationUnderAttack))
			{
				OwnedStationsImage.sprite = unitClass2.GetThumbnailIconSprite();
				OwnedStationsImage.gameObject.SetActive(value: true);
				if (isPlayerStationUnderAttack)
				{
					active = true;
				}
			}
			else
			{
				OwnedStationsImage.gameObject.SetActive(value: false);
			}
			if (UnitUnderAttackImage != null)
			{
				UnitUnderAttackImage.gameObject.SetActive(active);
			}
			if (ShowImages && sector != null && ContainsDiscoveredShipsHostileToPlayer(sector, out var unitClass3))
			{
				HostileShipsImage.sprite = unitClass3.GetThumbnailIconSprite();
				HostileShipsImage.gameObject.SetActive(value: true);
			}
			else
			{
				HostileShipsImage.gameObject.SetActive(value: false);
			}
			if (ShowImages && sector != null && ContainsDiscoveredStationsHostileToPlayer(sector, out var unitClass4))
			{
				HostilesStationsImage.sprite = unitClass4.GetThumbnailIconSprite();
				HostilesStationsImage.gameObject.SetActive(value: true);
			}
			else
			{
				HostilesStationsImage.gameObject.SetActive(value: false);
			}
		}

		public static bool ContainsDiscoveredStationsHostileToPlayer(Sector sector, out UnitClass unitClass)
		{
			unitClass = null;
			Faction localFaction = EngineASX.Instance.LocalFaction;
			if (localFaction != null && localFaction.Intel != null)
			{
				if (!localFaction.Intel.SectorHasRecentHostileStations(sector, double.MaxValue))
				{
					return false;
				}
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && localFaction.Intel.IsUnitDiscovered(item) && item.IsHostileToOrAlwaysHostileToTwoWay(EngineASX.Instance.LocalFaction) && (unitClass == null || item.UnitClass.SaleCost > unitClass.SaleCost))
						{
							unitClass = item.UnitClass;
						}
					}
				}
			}
			return unitClass != null;
		}

		public static bool ContainsDiscoveredShipsHostileToPlayer(Sector sector, out UnitClass unitClass)
		{
			unitClass = null;
			Faction localFaction = EngineASX.Instance.LocalFaction;
			if (localFaction != null && localFaction.Intel != null)
			{
				if (!localFaction.Intel.SectorHasRecentHostileShips(sector, double.MaxValue))
				{
					return false;
				}
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				float value = (sector.IsActive ? GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector : GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && localFaction.Intel.IsUnitDiscovered(item, value) && item.IsHostileToOrAlwaysHostileToTwoWay(EngineASX.Instance.LocalFaction) && (unitClass == null || item.UnitClass.CombatRating > unitClass.CombatRating))
						{
							unitClass = item.UnitClass;
						}
					}
				}
			}
			return unitClass != null;
		}

		public static bool ContainsPlayerStationsAndOutputHighestCombatRatingShip(Sector sector, out UnitClass unitClass, out bool isPlayerStationUnderAttack)
		{
			unitClass = null;
			isPlayerStationUnderAttack = false;
			if (sector.Engine.LocalFaction != null)
			{
				List<Unit> unitsByType = sector.Engine.LocalFaction.GetUnitsByType(UnitType.Station);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && item.Sector == sector)
						{
							if (unitClass == null || item.UnitClass.SaleCost > unitClass.SaleCost)
							{
								unitClass = item.UnitClass;
							}
							if (!isPlayerStationUnderAttack && item.IsUnderAttack())
							{
								isPlayerStationUnderAttack = true;
							}
						}
					}
				}
			}
			return unitClass != null;
		}

		public static bool ContainsPlayerShipsAndOutputHighestCombatRatingShip(Sector sector, out UnitClass unitClass, out bool isPlayerShipUnderAttack)
		{
			unitClass = null;
			isPlayerShipUnderAttack = false;
			if (sector.Engine.LocalFaction != null)
			{
				List<Unit> unitsByType = sector.Engine.LocalFaction.GetUnitsByType(UnitType.Ship);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.IsValidAndNotDestroyed && item.Sector == sector)
						{
							if (unitClass == null || item.UnitClass.CombatRating > unitClass.CombatRating)
							{
								unitClass = item.UnitClass;
							}
							if (!isPlayerShipUnderAttack && item.IsUnderAttack())
							{
								isPlayerShipUnderAttack = true;
							}
						}
					}
				}
			}
			return unitClass != null;
		}
	}
}
