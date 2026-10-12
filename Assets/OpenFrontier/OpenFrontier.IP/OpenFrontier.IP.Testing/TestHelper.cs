using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Testing
{
	public static class TestHelper
	{
		public static void DeclareWarWithTargetUnitTwoWay()
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalFaction != null && instance.Hud != null && instance.Hud.CurrentTarget != null && instance.Hud.CurrentTarget.Faction != null)
			{
				instance.LocalFaction.SetAsHostileToTwoWay(instance.Hud.CurrentTarget.Faction);
			}
		}

		public static void UnstableWormholes_RepointAll()
		{
			if (!EngineASX.LoadedAndReady)
			{
				return;
			}
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.WormholeComponent.IsUnstable)
					{
						RepointWormhole(item);
					}
				}
			}
		}

		private static void RepointWormhole(Unit wormhole)
		{
			wormhole.WormholeComponent.RandomizeTargetAndSetNextChangeTime();
			wormhole.WormholeComponent.RemoveAllFactionIntelOfWormholeEntry();
		}

		public static void UnstableWormholes_RepointOne()
		{
			if (!EngineASX.LoadedAndReady)
			{
				return;
			}
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.WormholeComponent.IsUnstable)
					{
						RepointWormhole(item);
						return;
					}
				}
			}
		}

		public static void PeaceWithCurrentTargetTwoWay()
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalFaction != null && instance.Hud != null && instance.Hud.CurrentTarget != null && instance.Hud.CurrentTarget.Faction != null)
			{
				instance.LocalFaction.SetAsPeaceTwoWay(instance.Hud.CurrentTarget.Faction);
			}
		}

		public static void MakeCurrentTargetFactionNeutralOpinion()
		{
			MakeCurrentTargetOpinionValue(0f);
		}

		public static void MakeCurrentTargetFactionFriendlyOpinion()
		{
			MakeCurrentTargetOpinionValue(0.5f);
		}

		public static void MakeCurrentTargetFactionUnfriendlyOpinion()
		{
			MakeCurrentTargetOpinionValue(-0.5f);
		}

		public static void MakeCurrentTargetOpinionValue(float opinion)
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalFaction != null && instance.Hud != null && instance.Hud.CurrentTarget != null && instance.Hud.CurrentTarget.Faction != null)
			{
				instance.Hud.CurrentTarget.Faction.SetOpinionWith(instance.LocalFaction, opinion);
			}
		}

		public static void SetWorstOpinionWithCurrentTarget()
		{
			SetOpinionWithCurrentTarget(-1f);
		}

		public static void SetOpinionWithCurrentTarget(float opinion)
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalFaction != null && instance.Hud != null && instance.Hud.CurrentTarget != null && instance.Hud.CurrentTarget.Faction != null)
			{
				instance.LocalFaction.SetOpinionWithTwoWay(instance.Hud.CurrentTarget.Faction, opinion);
			}
		}

		public static void KillPlayerUnit()
		{
			if (EngineASX.Instance != null && EngineASX.Instance.LocalPlayer != null && EngineASX.Instance.LocalPlayer.Person.CurrentUnit != null)
			{
				EngineASX.Instance.LocalPlayer.Person.CurrentUnit.Destructable.KillAnonymously();
			}
		}

		public static void SetCredits(int credits)
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalPlayer != null)
			{
				int credits2 = instance.LocalFaction.Credits;
				int num = Mathf.Clamp(credits - credits2, -credits2, int.MaxValue);
				if (num != 0)
				{
					instance.AddCreditsToPlayerFactionWithMsg(num, FactionTransactionType.Gift);
				}
			}
		}

		public static void KillPlayerRootUnit()
		{
			if (EngineASX.Instance != null && EngineASX.Instance.PlayerRootUnit != null && EngineASX.Instance.PlayerRootUnit.Destructable != null)
			{
				EngineASX.Instance.PlayerRootUnit.Destructable.KillAnonymously();
			}
		}

		public static void KillPlayerPerson()
		{
			if (EngineASX.Instance != null && EngineASX.Instance.LocalPlayer != null)
			{
				EngineASX.Instance.LocalPlayer.Person.TryKill();
			}
		}

		public static void KillAllHostiles(Faction localFaction, Faction damagerFaction)
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				KillSectorHostilesAsFaction(localFaction, damagerFaction, sector);
			}
		}

		public static void KillActiveSectorHostiles(Faction localFaction, Faction damagerFaction)
		{
			if (EngineASX.Instance != null && EngineASX.Instance.LocalFaction != null && EngineASX.Instance.ActiveSector != null)
			{
				KillSectorHostilesAsFaction(localFaction, damagerFaction, EngineASX.Instance.ActiveSector);
			}
		}

		private static void KillSectorHostilesAsFaction(Faction localFaction, Faction damagerFaction, Sector sector)
		{
			UnitType[] array = new UnitType[4]
			{
				UnitType.Station,
				UnitType.Projectile,
				UnitType.Ship,
				UnitType.Cargo
			};
			foreach (UnitType unitType in array)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(unitType);
				if (unitsByType == null || unitsByType.Count <= 0)
				{
					continue;
				}
				foreach (Unit item in unitsByType.ToList())
				{
					KillUnitIfHostile(item, localFaction, damagerFaction);
				}
			}
		}

		public static void KillNonPlayerCivilianUnitsAsPlayer(Faction localFaction)
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				KillSectorNonPlayerCivilianUnitsAsPlayer(localFaction, sector);
			}
		}

		public static void KillCurrentSectorNonPlayerCivilianUnitsAsPlayer(Faction localFaction)
		{
			Sector activeSector = EngineASX.Instance.ActiveSector;
			KillSectorNonPlayerCivilianUnitsAsPlayer(localFaction, activeSector);
		}

		private static void KillSectorNonPlayerCivilianUnitsAsPlayer(Faction localFaction, Sector sector)
		{
			UnitType[] array = new UnitType[4]
			{
				UnitType.Station,
				UnitType.Projectile,
				UnitType.Ship,
				UnitType.Cargo
			};
			foreach (UnitType unitType in array)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(unitType);
				if (unitsByType == null || unitsByType.Count <= 0)
				{
					continue;
				}
				foreach (Unit item in unitsByType.ToList())
				{
					if (item.Faction != null && item.Faction.IsCivilianFromFactionType)
					{
						KillUnitIfNotPlayerOwned(item, localFaction);
					}
				}
			}
		}

		public static void KillCurrentSectorNonPlayerUnits(Faction localFaction)
		{
			Sector activeSector = EngineASX.Instance.ActiveSector;
			UnitType[] array = new UnitType[4]
			{
				UnitType.Station,
				UnitType.Projectile,
				UnitType.Ship,
				UnitType.Cargo
			};
			foreach (UnitType unitType in array)
			{
				List<Unit> unitsByType = activeSector.GetUnitsByType(unitType);
				if (unitsByType == null || unitsByType.Count <= 0)
				{
					continue;
				}
				foreach (Unit item in unitsByType.ToList())
				{
					KillUnitIfNotPlayerOwned(item, null);
				}
			}
		}

		private static void KillUnitIfNotPlayerOwned(Unit unit, Faction attackerFaction)
		{
			if (unit.Destructable != null && unit.Faction != EngineASX.Instance.LocalFaction)
			{
				unit.Destructable.KillUnitAndChildren(null, attackerFaction, DamageDirectType.Direct);
			}
		}

		private static void KillUnitIfHostile(Unit unit, Faction localFaction, Faction damagerFaction)
		{
			if (unit.Destructable != null && unit.IsHostileToOrAlwaysHostileToTwoWay(localFaction))
			{
				unit.Destructable.KillUnitAsFaction(damagerFaction);
			}
		}

		private static Unit GetCurrentHudTarget()
		{
			if (EngineASX.Instance != null && EngineASX.Instance.Hud != null && EngineASX.Instance.Hud.CurrentTarget != null)
			{
				return EngineASX.Instance.Hud.CurrentTarget;
			}
			return null;
		}

		private static DestructableUnit GetTargetDestructableUnit()
		{
			Unit currentHudTarget = GetCurrentHudTarget();
			if ((bool)currentHudTarget)
			{
				return currentHudTarget.Destructable;
			}
			return null;
		}

		public static void KillCurrentTargetAsPlayer()
		{
			DestructableUnit targetDestructableUnit = GetTargetDestructableUnit();
			if (targetDestructableUnit != null)
			{
				targetDestructableUnit.KillUnitAsPlayer();
			}
		}

		public static void KillCurrentTarget()
		{
			DestructableUnit targetDestructableUnit = GetTargetDestructableUnit();
			if (targetDestructableUnit != null)
			{
				targetDestructableUnit.KillAnonymously();
			}
		}

		public static void KillCurrentTargetWithDamage()
		{
			DestructableUnit targetDestructableUnit = GetTargetDestructableUnit();
			if (targetDestructableUnit != null)
			{
				targetDestructableUnit.ApplyDamage(Vector3.zero, null, null, DamageDirectType.Direct, new DamageType
				{
					Damage = 999999f
				});
			}
		}

		public static void ForceWinScenario()
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.World != null)
			{
				instance.World.ForceWin();
			}
		}

		public static void ForceFailScenario()
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.World != null)
			{
				instance.World.ForceFail();
			}
		}

		public static void DockWithCurrenTarget()
		{
			if (!(EngineASX.Instance != null) || !EngineASX.Instance.IsPlayerPilot)
			{
				return;
			}
			Unit currentHudTarget = GetCurrentHudTarget();
			if (!(currentHudTarget != null))
			{
				return;
			}
			if (currentHudTarget.UnitType == UnitType.Wormhole)
			{
				Wormhole wormholeComponent = currentHudTarget.WormholeComponent;
				if (wormholeComponent != null && wormholeComponent.ActualTargetSector != null)
				{
					EngineASX.Instance.TryEnterWormhole(wormholeComponent);
				}
			}
			else if (currentHudTarget.IsDockable)
			{
				EngineASX.Instance.PlayerUnit.Components.TryDockInUnit(currentHudTarget);
			}
		}
	}
}
