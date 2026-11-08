using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class OutlawRaidTargetsSearch
	{
		private Queue<Unit> targetSearchQueue;

		private Faction faction;

		private float bestScore;

		public Fleet Fleet { get; private set; }

		public FactionAIOutlaw OutlawModule { get; private set; }

		public float TotalSearchScore { get; private set; }

		public OutlawRaidTargetsSearchItem? BestTarget { get; set; }

		public bool IsSearching => targetSearchQueue.Count > 0;

		public static OutlawRaidTargetsSearch Initialize(FactionAIOutlaw outlawModule, Fleet fleet)
		{
			OutlawRaidTargetsSearch outlawRaidTargetsSearch = new OutlawRaidTargetsSearch();
			outlawRaidTargetsSearch.Fleet = fleet;
			outlawRaidTargetsSearch.faction = fleet.Faction;
			outlawRaidTargetsSearch.OutlawModule = outlawModule;
			outlawRaidTargetsSearch.Init();
			outlawRaidTargetsSearch.TotalSearchScore += fleet.GetCachedSimpleCombatRating();
			return outlawRaidTargetsSearch;
		}

		private void Init()
		{
			int num = Physics.OverlapSphereNonAlloc(Fleet.Sector.ToWorldPosition(Fleet.SectorPosition), 1500f, EngineASX.ColliderCache, GameController.Instance.ShipsAndStationsMask, QueryTriggerInteraction.Collide);
			targetSearchQueue = new Queue<Unit>(num * 2);
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component.Faction != null && component.Faction != Fleet.Faction)
				{
					targetSearchQueue.Enqueue(component);
				}
			}
		}

		public void ProcessSearch()
		{
			if (targetSearchQueue.Count > 0)
			{
				Unit unit = targetSearchQueue.Dequeue();
				if (unit != null && unit.IsValidAndNotDestroyed)
				{
					ConsiderUnitForAttack(unit, Fleet);
				}
			}
		}

		private void ConsiderUnitForAttack(Unit detected, Fleet ourFleet)
		{
			if (detected.Faction == null || detected.Faction == faction || detected == ourFleet.HomeBaseUnit || !faction.Intel.IsUnitDiscoveredUsingDefaultDiscoveryAge(detected) || detected.IsDocked)
			{
				return;
			}
			float valueOrDefault = faction.GetEffectiveOpinionOrNull(detected.Faction).GetValueOrDefault();
			if (valueOrDefault < 0.25f)
			{
				TotalSearchScore -= detected.CombatRating;
			}
			else if (valueOrDefault > 0.5f)
			{
				return;
			}
			PirateRaidSettings pirateRaidSettings = GameController.Instance.GameSettings.PirateRaidSettings;
			float tradableCargoValue = detected.CargoBayComponent.TradableCargoValue;
			if (!OutlawModule.TryConsiderUnitForAttack(Fleet.LeaderUnit, detected))
			{
				return;
			}
			if (detected.IsPilottedByPlayer() && Fleet.LeaderUnit != null && !Fleet.InCombat)
			{
				EngineASX.Instance.UniverseEventsNotifierController.NotifyPlayerScannedByOther(Fleet.LeaderUnit);
			}
			if (!(tradableCargoValue < pirateRaidSettings.MinRaidableCargoValue) && (!(detected.Sector == faction.HomeSector) || (!(detected.CargoBayComponent.GetVolumeOf(GameController.Instance.CargoClasses.ImpulseCargoClass) / detected.CargoBayComponent.Capacity > 0.05f) && !(detected.CargoBayComponent.GetVolumeOf(GameController.Instance.CargoClasses.ChemicalsCargoClass) / detected.CargoBayComponent.Capacity > 0.05f))))
			{
				float num = ((detected.UnitType == UnitType.Station) ? pirateRaidSettings.BestRaidableCargoValueReferenceStation : pirateRaidSettings.BestRaidableCargoValueReference);
				float num2 = Mathf.Clamp01(tradableCargoValue / num) * pirateRaidSettings.RaidCargoValueFactor;
				TotalSearchScore += num2;
				float num3 = ((0f - valueOrDefault) * pirateRaidSettings.RaidOpinionFactor + num2) / (pirateRaidSettings.RaidCargoValueFactor + pirateRaidSettings.RaidOpinionFactor);
				num3 += AddScoreBasedOnTargetFaction(detected.Faction);
				num3 += pirateRaidSettings.ScoreFudge;
				num3 += Random.value * pirateRaidSettings.RandomScoreFudge;
				if (detected.Faction.Virtue < 0.5f)
				{
					num3 -= (0.5f - detected.Faction.Virtue) / 0.5f * 0.1f;
				}
				if (!(num3 < 0f) && IsTargetBelowCombatRatingForAttack(ourFleet, detected) && (!BestTarget.HasValue || num3 > bestScore))
				{
					BestTarget = new OutlawRaidTargetsSearchItem
					{
						Unit = detected
					};
					bestScore = num3;
				}
			}
		}

		private float AddScoreBasedOnTargetFaction(Faction faction)
		{
			return faction.FactionType switch
			{
				FactionType.Bandit => -0.8f, 
				FactionType.Outlaw => -0.8f, 
				FactionType.Bar => -0.8f, 
				FactionType.PassengerTransport => -0.8f, 
				_ => 0f, 
			};
		}

		private bool IsTargetBelowCombatRatingForAttack(Fleet ourFleet, Unit target)
		{
			float cachedSimpleCombatRating = ourFleet.GetCachedSimpleCombatRating();
			float num = 0f;
			Fleet fleet = target.GetFleet();
			if (fleet != null)
			{
				num = fleet.GetCachedSimpleCombatRating();
			}
			else
			{
				num = target.CombatRating;
				if (target.UnitType == UnitType.Station)
				{
					num *= 3f;
				}
			}
			if (num > 0f && cachedSimpleCombatRating / num < 1.5f - faction.Aggression * 0.3f)
			{
				return false;
			}
			return true;
		}
	}
}
