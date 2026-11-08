using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Claiming;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Comms.FactionDynamicComms;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.FactionOpinionNeutralizer;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Factions.Npc;
using OpenFrontier.IP.Engine.Factions.StationBuilding;
using OpenFrontier.IP.Engine.Fleets.ActiveObjectives;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.Settings;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Engine.WorldPopulation;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.PilotRanking;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIBase : MonoBehaviour
	{
		private class WeightedUnit : IWeighted
		{
			public float DefendPriority { get; set; }

			public float Weight { get; set; }

			public Unit Unit { get; set; }
		}

		private struct RecentlyLostUnit
		{
			public UnitClass UnitClass;

			public Unit Attacker;

			public Faction AttackerFaction;

			public RecentlyLostUnit(UnitClass unitClass, Unit attacker, Faction attackerFaction)
			{
				this = default;
				UnitClass = unitClass;
				Attacker = attacker;
				AttackerFaction = attackerFaction;
			}
		}

		internal double rearmCooldownTime;

		private float mostPowerfulFleetCombatRating;

		private FactionStationBuildRequestProcessor stationBuildRequestProcessor;

		private float lastExpansionSearchTime;

		private FactionEmpireExpansionSearch expansionSearch;

		private FactionAIAttackModule attackModule;

		public const int MinEquipmentDealerPatrolNodesInSector = 1;

		public const int MaxEquipmentDealerPatrolNodesInSector = 1;

		private FactionAIStrategy strategy;

		private Queue<RecentlyLostUnit> recentlyLostUnits = new Queue<RecentlyLostUnit>();

		internal HashSet<int> cachedBuildableUnitClasses = new HashSet<int>();

		private const float stealCargoVirtueThreshold = 0.38f;

		private const float ratOnBountyTargetThreshold = 0f;

		private const float stealHostilesCargoVirtueThreshold = 0.98f;

		private MercenaryHireInfo mercenaryHireInfo;

		protected List<DistressCall> recentDistressCalls = new List<DistressCall>();

		private DynamicCommsHandler commsHandler = new DynamicCommsHandler();

		private FactionStationBuilder stationBuilder;

		private FactionShipBuilder shipBuilder;

		private const int MaxFactionAlliances = 6;

		private double lastOrderedPatrolTime = double.MinValue;

		public const float TimeBetweenBanditPatrols = 360f;

		private double lastBuiltUnitTime;

		public const int MinCreditsForTrading = 2000;

		public const int PlaceBountyMinCreditsReserve = 10000;

		public const int MaxPlaceableBounty = 25000;

		public const int DefaultMaxJumpDistance = 5;

		private const float stolenCargoValueToBountyConversion = 0.5f;

		private const float destroyedShipValueToBountyConversion = 0.1f;

		private const float CheckForRepairsFrequency = 0.7f;

		private const float CheckForIdleFrequency = 5f;

		private float nextCheckForRepairsTime;

		private float nextCheckForIdles = 2f;

		public const int DefaultShipBuildCap = 4;

		public const float ForeignDockRequiredOpinion = -0.75f;

		protected EngineASX engine;

		protected Faction faction;

		protected double nextBuildUnitsTime;

		private int numFleetsSpawned;

		private int numUnitsSpawned;

		[FormerlySerializedAs("SpawnMode")]
		public FactionSpawnMode ShipBuildSectorMode = FactionSpawnMode.AnySector;

		[FormerlySerializedAs("SpawnOnlyAtOwnedDocks")]
		public bool ShipBuildOnlyAtOwnedDocks;

		[FormerlySerializedAs("SpawnScenes")]
		public List<Sector> ShipBuildSectors = new List<Sector>();

		private FactionAIUnitsUnderAttackProcessor unitsUnderAttackProcessor;

		private Queue<Fleet> idleFleetQueueChecker = new Queue<Fleet>(4);

		private FactionAISettings aISettings;

		private FactionAIRearmer fleetRearmer;

		private Dictionary<int, double> lastSentDistressCallTimes = new Dictionary<int, double>();

		private static List<WeightedUnit> defendUnitCache = new List<WeightedUnit>(20);

		private Dictionary<int, float> defendTargetCooldownTime = new Dictionary<int, float>(20);

		public static Queue<Unit> DismantleCache = new Queue<Unit>(10);

		private int repairFleetIndex = -1;

		private static HashSet<int> protectedFleetCached = new HashSet<int>(4);

		public float MinInterceptionDistance = 1000f;

		public float MaxInterceptionDistance = 10000f;

		private int lastRpProvision;

		private int lastRpUsage;

		public FactionAIStrategy Strategy => strategy;

		public Faction Faction => faction;

		public EngineASX Engine => engine;

		public virtual FactionAIType AIType => FactionAIType.Generic;

		public FactionShipBuilder ShipBuilder
		{
			get
			{
				if (shipBuilder == null)
				{
					shipBuilder = new FactionShipBuilder();
					shipBuilder.FactionAI = this;
				}
				return shipBuilder;
			}
		}

		public FactionStationBuilder StationBuilder
		{
			get
			{
				if (stationBuilder == null)
				{
					stationBuilder = new FactionStationBuilder();
					stationBuilder.FactionAI = this;
				}
				return stationBuilder;
			}
		}

		public double NextUnitSpawnTime
		{
			get
			{
				return nextBuildUnitsTime;
			}
			set
			{
				nextBuildUnitsTime = value;
			}
		}

		public int NumUnitsSpawned
		{
			get
			{
				return numUnitsSpawned;
			}
			set
			{
				numUnitsSpawned = value;
			}
		}

		public int NumGroupsSpawned
		{
			get
			{
				return numFleetsSpawned;
			}
			set
			{
				numFleetsSpawned = value;
			}
		}

		public FactionAISettings AISettings => aISettings;

		public static PriorityQueue<Unit, float> IdleUnitCache { get; } = new PriorityQueue<Unit, float>(50);

		public virtual bool BuildStealthShips => FactionTypeInfo.BuildCloakedShips;

		public virtual float PreferenceToBuildCloakedShips => aISettings.CloakShipPreference;

		public virtual bool GroupStealthShipsTogether => true;

		public double LastOrderedPatrolTime
		{
			get
			{
				return lastOrderedPatrolTime;
			}
			set
			{
				lastOrderedPatrolTime = value;
			}
		}

		public double LastBuiltUnitTime
		{
			get
			{
				return lastBuiltUnitTime;
			}
			set
			{
				lastBuiltUnitTime = value;
			}
		}

		public int LastRpProvision => lastRpProvision;

		public int LastRpUsage => lastRpUsage;

		public int TotalRpProvision
		{
			get
			{
				int num = faction.AdditionalRpProvision;
				foreach (Unit unit in faction.Units)
				{
					num += unit.RpProvision;
				}
				if (faction.IsAIFactionType)
				{
					if (FactionTypeInfo != null)
					{
						num += FactionTypeInfo.BaseRpProvision;
					}
					else
					{
						Debug.LogError("Could not find engine faction type to apply rp provision: " + faction.FactionType);
					}
				}
				return num;
			}
		}

		public FactionTypeInfo FactionTypeInfo => Faction.FactionTypeInfo;

		public MercenaryHireInfo MercenaryHireInfo
		{
			get
			{
				return mercenaryHireInfo;
			}
			set
			{
				if (mercenaryHireInfo != value)
				{
					mercenaryHireInfo = value;
				}
			}
		}

		public Sector HomeSector => faction.HomeSector;

		public void OnClaimedUnit(Unit targetUnit, Fleet claimerFleet)
		{
			if (!targetUnit.IsNormalShip())
			{
				return;
			}
			FactionStrategy unitClassShipStrategyFlags = EngineASX.Instance.GetUnitClassShipStrategyFlags(targetUnit.UnitClass);
			if (targetUnit.UnitType != UnitType.Ship || !CanUseShip(targetUnit) || claimerFleet.FleetStrategy == FactionStrategy.Unspecified || (claimerFleet.FleetStrategy & unitClassShipStrategyFlags) == 0)
			{
				return;
			}
			foreach (UnitComponentHolder item in claimerFleet.Ships.ToList())
			{
				if (item != null && item.PilotNpc != null && targetUnit.CombatRating > item.CombatRating * 1.25f)
				{
					targetUnit.Components.PilotNpc = item.PilotNpc;
					EngineASX.Instance.DebugInfo.NumTimesNpcPilotJumpedIntoClaimedShip++;
					break;
				}
			}
		}

		public virtual int GetMaxShipBuildDistFromHomeSector()
		{
			return 6;
		}

		public void HandleUnitUnderAttack(Unit attackedUnit, Faction attackingFaction, Unit attackerUnit, RecentAttackType attackType)
		{
			Fleet fleet = attackedUnit.GetFleet();
			if (fleet != null && CanOrderFleet(fleet))
			{
				TryOrderUnitToRespondToAttack(fleet, attackerUnit, attackType);
			}
			if (attackType == RecentAttackType.UpdatedAttack && faction.Fleets.Count > 1)
			{
				OrderOtherFleetsToDefendAttackedUnit(attackedUnit);
			}
			if ((attackType == RecentAttackType.NewAttack || attackType == RecentAttackType.UpdatedAttack) && UnityEngine.Random.value < 0.2f)
			{
				Faction controllingFaction = attackedUnit.Sector.ControllingFaction;
				if (faction.IsCivilianFromFactionType && controllingFaction != null && controllingFaction != faction && faction.GetOpinion(controllingFaction) > -0.25f && !faction.IsHostileTo(controllingFaction))
				{
					AppealToHelpFromControllingFaction(controllingFaction, attackingFaction, (attackedUnit != null) ? attackedUnit.Sector : null);
				}
			}
		}

		private void AppealToHelpFromControllingFaction(Faction sectorController, Faction attackingFaction, Sector attackSector)
		{
			EngineASX.Instance.DebugInfo.NumTimesFactionRequestedHelpFromSectorController++;
			sectorController.RequestHelpAgainstAttackingFaction(this, attackingFaction, attackSector);
		}

		public static float GetUnitDefendOrderPriority(Unit unit)
		{
			float num = 0f;
			if (unit.IsStation())
			{
				num = ((!unit.IsUnderConstruction) ? (num - Mathf.Clamp01(unit.CombatRating / 10f) * 0.25f) : 0.3f);
				num += Mathf.Clamp01(WorldStationSeeder.GetStationPurposeDefensePriority(unit.UnitClass.StationPurpose) / 10f) * 0.5f;
				num += unit.Destructable.NormalizedDamage * 0.5f;
			}
			else
			{
				num += unit.Destructable.NormalizedDamage * (0.25f + unit.UnitClass.RelativeShipSaleCost * 0.5f);
			}
			return Mathf.Clamp01(num);
		}

		private void OrderOtherFleetsToDefendAttackedUnit(Unit attackedUnit)
		{
			if (!attackedUnit.IsStationOrShip())
			{
				return;
			}
			float num = GetUnitDefendOrderPriority(attackedUnit);
			if (!(num > 0f))
			{
				return;
			}
			int num2 = 0;
			foreach (Fleet fleet in faction.Fleets)
			{
				if (fleet.FleetStrategy.IsPeaceful())
				{
					num *= 0.5f;
				}
				if (fleet.IsArmed && fleet != attackedUnit.GetFleet() && CanOrderFleet(fleet) && CanOverrideFleetOrder(fleet, num))
				{
					EngineASX.Instance.DebugInfo.NumTimesFleetOrderedToDefendAttackedUnit++;
					OrderFleetToProtectUnit(fleet, attackedUnit, num);
					num2++;
					if (num2 >= 3)
					{
						break;
					}
				}
			}
		}

		public int GetHiredMercenaryHours(Fleet mercenaryFleet, FactionAIMercenary mercenaryFaction)
		{
			int maxMercenaryHireHours = GetMaxMercenaryHireHours(mercenaryFaction.GetHourlyRate(mercenaryFleet, faction));
			maxMercenaryHireHours = Mathf.Min(24, maxMercenaryHireHours);
			return UnityEngine.Random.Range(1, maxMercenaryHireHours);
		}

		private int GetMaxMercenaryHireHours(int hourlyRate)
		{
			return (faction.Credits - 10000) / hourlyRate;
		}

		public bool RequestHireMercenary(Fleet mercenaryFleet, Fleet targetFleet, FactionAIMercenary factionAIMercenary)
		{
			if (GetMaxMercenaryHireHours(factionAIMercenary.GetHourlyRate(mercenaryFleet, faction)) < 3)
			{
				return false;
			}
			FactionType factionType = faction.FactionType;
			if (factionType == FactionType.StationBuilder || factionType == FactionType.Explorer || factionType == FactionType.Mercenary)
			{
				return false;
			}
			if (!faction.IsBanditOrOutlaw() && !targetFleet.IsUnderAttack)
			{
				FactionStrategy fleetStrategy = targetFleet.FleetStrategy;
				if (fleetStrategy == FactionStrategy.Trade || fleetStrategy == FactionStrategy.Mine)
				{
					return true;
				}
				return false;
			}
			if (faction.IsHostileToOrAlwaysHostileTo(factionAIMercenary.faction))
			{
				return false;
			}
			if (factionAIMercenary.faction.IsHostileToOrAlwaysHostileTo(faction))
			{
				return false;
			}
			return faction.GetEffectiveOpinionOrNull(factionAIMercenary.faction) > -0.5f;
		}

		protected ReturnToBaseOrder OrderFleetReturnToBase(Fleet fleet)
		{
			ReturnToBaseOrder returnToBaseOrder = UnityObjectHelper.NewGameObject<ReturnToBaseOrder>();
			returnToBaseOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			returnToBaseOrder.MaxDuration = 600f;
			fleet.SetOrder(returnToBaseOrder);
			OnFleetOrdered(fleet);
			return returnToBaseOrder;
		}

		private void TryOrderUnitToRespondToAttack(Fleet fleet, Unit attackerUnit, RecentAttackType attackType)
		{
			if (fleet.CurState == FleetState.CombatInterception || !faction.IsHostileTo(attackerUnit) || !CanOverrideFleetOrder(fleet, 0.6f) || TryOrderFleetToFightBack(fleet, attackerUnit))
			{
				return;
			}
			UpdateFleetHomeBase(fleet);
			if (fleet.HomeBaseUnit != null)
			{
				ReturnToBaseOrder returnToBaseOrder = OrderFleetReturnToBase(fleet);
				returnToBaseOrder.Priority = 0.9f;
				returnToBaseOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
				returnToBaseOrder.AllowCombatInterception = false;
				EngineASX.Instance.DebugInfo.NumTimesFleetsOrderedToFlee++;
				return;
			}
			HashSet<int> discoveredUnitIdsInSectorIgnoringTimeOfDiscovery = faction.Intel.GetDiscoveredUnitIdsInSectorIgnoringTimeOfDiscovery(fleet.Sector);
			if (discoveredUnitIdsInSectorIgnoringTimeOfDiscovery != null)
			{
				foreach (int item in discoveredUnitIdsInSectorIgnoringTimeOfDiscovery)
				{
					Unit unitByid = EngineASX.Instance.GetUnitByid(item);
					if (unitByid != null && unitByid.UnitType == UnitType.Station && unitByid.IsDockable && unitByid.Faction != null && (unitByid.Faction == faction || unitByid.Faction.RequestDock(unitByid, faction)))
					{
						DockOrder dockOrder = UnityObjectHelper.NewGameObject<DockOrder>();
						dockOrder.AllowCombatInterception = false;
						dockOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
						dockOrder.Priority = 0.8f;
						dockOrder.MaxDuration = 600f;
						fleet.SetOrder(dockOrder);
						OnFleetOrdered(fleet);
						EngineASX.Instance.DebugInfo.NumTimesFleetsOrderedToFlee++;
						return;
					}
				}
			}
			List<Unit> unitsByType = fleet.Sector.GetUnitsByType(UnitType.Wormhole);
			if (unitsByType != null)
			{
				foreach (Unit item2 in unitsByType)
				{
					if (item2 != null && faction.Intel.IsUnitDiscovered(item2))
					{
						DockOrder dockOrder2 = UnityObjectHelper.NewGameObject<DockOrder>();
						dockOrder2.AllowCombatInterception = false;
						dockOrder2.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
						dockOrder2.Priority = 0.8f;
						dockOrder2.MaxDuration = 600f;
						fleet.SetOrder(dockOrder2);
						OnFleetOrdered(fleet);
						EngineASX.Instance.DebugInfo.NumTimesFleetsOrderedToFlee++;
						return;
					}
				}
			}
			MoveToOrder moveToOrder = UnityObjectHelper.NewGameObject<MoveToOrder>();
			moveToOrder.AllowCombatInterception = false;
			moveToOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
			Vector3 randomSafeDeploymentSectorPosition = fleet.Sector.GetRandomSafeDeploymentSectorPosition(0f, 0.9f, 50f, GameController.Instance.StaticNonOverlappingMask);
			moveToOrder.Target = new SectorTarget
			{
				Sector = fleet.Sector,
				SectorPosition = randomSafeDeploymentSectorPosition
			};
			moveToOrder.Priority = 0.8f;
			moveToOrder.MaxDuration = 600f;
			fleet.SetOrder(moveToOrder);
			OnFleetOrdered(fleet);
			EngineASX.Instance.DebugInfo.NumTimesFleetsOrderedToFlee++;
		}

		public virtual bool RequestStealCargo(Faction ownerFaction, CargoOwnership cargoOwnership)
		{
			if (cargoOwnership == CargoOwnership.OwnedByHostile)
			{
				return true;
			}
			return faction.GetOpinion(ownerFaction) + faction.Virtue < 0.38f;
		}

		public bool IsFleetHomeBaseValid(Fleet fleet, Unit homeBaseUnit)
		{
			if (homeBaseUnit != null && homeBaseUnit.IsValidAndNotDestroyed)
			{
				if (!(homeBaseUnit.Faction == faction))
				{
					return !homeBaseUnit.IsHostileToOrAlwaysHostileToTwoWay(faction);
				}
				return true;
			}
			return false;
		}

		private bool TryOrderFleetToFightBack(Fleet fleet, Unit attackerUnit)
		{
			if (!faction.IsHostileTo(attackerUnit))
			{
				return false;
			}
			if (AICombatProbabilityCalculator.GetSimpleFleetProbabilityAgainstUnitOrUnitFleet(fleet, attackerUnit) + faction.Aggression * 0.3f > 0.5f)
			{
				AttackTargetOrder attackTargetOrder = UnityObjectHelper.NewGameObject<AttackTargetOrder>();
				attackTargetOrder.AllowCombatInterception = true;
				attackTargetOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
				attackTargetOrder.Priority = 0.6f;
				attackTargetOrder.MaxDuration = 180f;
				attackTargetOrder.TargetUnit = attackerUnit;
				fleet.SetOrder(attackTargetOrder);
				OnFleetOrdered(fleet);
				EngineASX.Instance.DebugInfo.NumTimesFleetsOrderedToFightBack++;
				return true;
			}
			return false;
		}

		public bool CanOverrideFleetOrder(Fleet fleet, float priority)
		{
			FleetOrder currentOrNextOrder = fleet.GetCurrentOrNextOrder();
			if (currentOrNextOrder != null)
			{
				return priority > currentOrNextOrder.Priority;
			}
			return true;
		}

		public virtual void OnNewUnitScanned(Unit scanner, Unit scanned, DiscoverUnitResult discoverUnitResult)
		{
			if (discoverUnitResult == DiscoverUnitResult.New || discoverUnitResult == DiscoverUnitResult.Updated)
			{
				RandomlyRatOnBountyTarget(scanned);
				Fleet fleet = scanner.GetFleet();
				if (fleet != null && scanned.UnitType == UnitType.Cargo && scanned.CargoComponent != null && scanned.CargoComponent.Quantity > 0 && (fleet.FleetStrategy == FactionStrategy.Scavenge || CanOverrideFleetOrder(fleet, GetPriorityToCollectCargo(scanned.CargoComponent))))
				{
					CargoOwnership cargoOwnership = CollectCargoHelper.CalculateCargoOwnership(scanned.CargoComponent, faction);
					if ((cargoOwnership == CargoOwnership.OwnedByHostile || cargoOwnership == CargoOwnership.OwnedByOther) && !RequestStealCargo(scanned.Faction, cargoOwnership))
					{
						return;
					}
					if (CanOrderFleetToCollectCargo(fleet))
					{
						OrderFleetToCollectCargo(fleet, scanned);
					}
				}
			}
			if (!(scanned.Faction == null) || !ClaimUnitHelper.CanClaimUnit(scanned) || !CanClaimUnit(scanned))
			{
				return;
			}
			Fleet fleet2 = scanner.GetFleet();
			if (fleet2 != null)
			{
				float priority = 0.8f;
				if (CanOrderFleet(fleet2) && CanOverrideFleetOrder(fleet2, priority) && !fleet2.IsUnderAttack && !(fleet2.ActiveOrder is ActiveClaimUnitOrder))
				{
					OrderFleetToClaimUnit(fleet2, scanned, priority);
				}
			}
		}

		protected virtual bool CanClaimUnit(Unit scanned)
		{
			return true;
		}

		private void OrderFleetToClaimUnit(Fleet fleet, Unit scanned, float priority)
		{
			ClaimUnitOrder claimUnitOrder = UnityObjectHelper.NewGameObject<ClaimUnitOrder>();
			claimUnitOrder.AllowCombatInterception = true;
			claimUnitOrder.MaxJumpDistance = 99;
			claimUnitOrder.TargetUnit = scanned;
			claimUnitOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			claimUnitOrder.Priority = priority;
			fleet.InsertOrderAndRequeueActive(claimUnitOrder);
			OnFleetOrdered(fleet);
		}

		private float GetPriorityToCollectCargo(Cargo cargoComponent)
		{
			int creditsValue = cargoComponent.CreditsValue;
			float num = Mathf.Lerp(100000f, 1000f, faction.Greed);
			return Mathf.Lerp(0.05f, 0.8f, Mathf.Clamp01((float)creditsValue / num));
		}

		private bool CanOrderFleetToCollectCargo(Fleet fleet)
		{
			if (CanOrderFleet(fleet) && fleet.GetCachedFreeCargoSpace01() > 0.2f && !IsFleetCollectingCargo(fleet))
			{
				return !fleet.InCombat;
			}
			return false;
		}

		private bool IsFleetCollectingCargo(Fleet fleet)
		{
			return fleet.ActiveOrder is ActiveCollectCargoOrder;
		}

		private void RandomlyRatOnBountyTarget(Unit unit)
		{
			if (!(unit.Components != null) || !(UnityEngine.Random.value < 0.2f) || !(unit.Components.PilotPerson != null) || !(unit.Components.PilotPerson.Faction != faction))
			{
				return;
			}
			List<FactionBountyItem> bountiesOnPerson = EngineASX.Instance.GetBountiesOnPerson(unit.Components.PilotPerson);
			if (bountiesOnPerson == null || bountiesOnPerson.Count <= 0)
			{
				return;
			}
			foreach (FactionBountyItem item in bountiesOnPerson)
			{
				if (item.IsValid && !(faction.GetOpinion(item.Person.Faction) - faction.Virtue > 0f))
				{
					BountyHelper.UpdateBountyLastKnownPositionAndTime(unit.Components.PilotPerson);
				}
			}
		}

		public FactionAIPatrolSettings GetPatrolSettingsOrDefault()
		{
			if (AISettings.PatrolSettings != null)
			{
				return AISettings.PatrolSettings;
			}
			return GameController.Instance.GameSettings.FactionSettings.AIPatrolSettings;
		}

		public bool WillTradeWith(Faction otherFaction)
		{
			if (!faction.IsHostileTo(otherFaction))
			{
				return faction.GetOpinion(otherFaction) > -0.05f - faction.Cooperation * 0.2f;
			}
			return false;
		}

		public virtual bool WillSellIntelItemTo(Faction localFaction, Unit unit)
		{
			if (!faction.IsCivilianFromFactionType && unit.Faction == faction)
			{
				float num = 0.5f;
				if (faction.IsBanditOrOutlaw())
				{
					num += 0.4f;
				}
				num -= faction.Virtue * 0.5f;
				num -= faction.Cooperation * 0.2f;
				if (faction.GetOpinion(localFaction) < num)
				{
					return false;
				}
			}
			return true;
		}

		public virtual bool WillSellIntelTo(Faction otherFaction)
		{
			return WillTradeWith(otherFaction);
		}

		protected internal virtual void OnNewStationFullyConstructed(Unit newStation)
		{
			if (!newStation.IsMinorStation() && faction.LeaderPerson != null && CanStationHostLeader(newStation) && (faction.LeaderPerson.CurrentUnit == null || faction.LeaderPerson.CurrentUnit.Faction != faction))
			{
				faction.LeaderPerson.CurrentUnit = newStation;
			}
		}

		public void OnRequestFriendsToJoinWar(FactionWarDeclaration warDeclaration)
		{
			IEnumerable<Faction> enumerable = FactionWarFriendRequestor.MakeRequests(faction, warDeclaration);
			List<Faction> list = ((faction == warDeclaration.Aggressor) ? warDeclaration.AggressorFactionsRequestedToJoin : warDeclaration.DefenderFactionsRequestedToJoin);
			List<Faction> list2 = ((faction == warDeclaration.Aggressor) ? warDeclaration.AggressorFactionsJoined : warDeclaration.DefenderFactionsJoined);
			foreach (Faction item in enumerable)
			{
				list.Add(item);
				if (item.RequestToJoinInWar(faction, warDeclaration))
				{
					list2.Add(item);
				}
			}
		}

		public virtual float GetStationTypeBuildPriority(UnitClass unitClass)
		{
			return 0f;
		}

		public virtual float GetDamageMultiplierForIndirectFireFromNpcFaction(Faction sourceFaction)
		{
			return GameController.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings.NpcFactionIndirectDamageMultiplier;
		}

		protected internal virtual bool AllowNewStationBuild()
		{
			return true;
		}

		public bool RequestToJoinWar(Faction requestor, FactionWarDeclaration warDeclaration)
		{
			return FactionWarRequestHandler.HandleRequest(requestor, faction, warDeclaration);
		}

		public virtual bool CanBuildShipsInSector(Sector sector)
		{
			return true;
		}

		public virtual bool CanBuildStationInSector(Sector sector, UnitClass unitClass)
		{
			if (unitClass.StationPurpose == StationPurpose.Outpost)
			{
				FactionType factionType = faction.FactionType;
				if (factionType == FactionType.Bandit || factionType == FactionType.Outlaw)
				{
					return true;
				}
				return !sector.HasPlanets;
			}
			return true;
		}

		public virtual bool CanTraverseIntoSector(Sector sector)
		{
			if (HomeSector != null && AISettings.MaxJumpDistanceFromHomeSector >= 0)
			{
				return HomeSector.GetJumpDistanceTo(sector) <= AISettings.MaxJumpDistanceFromHomeSector;
			}
			return true;
		}

		public virtual void Init()
		{
			if (engine == null)
			{
				FindFaction();
				if (faction.FactionAI != null && faction.FactionAI != this)
				{
					throw new Exception("Multiple faction AIs not supported");
				}
				faction.CreateAISettingsIfNull();
				aISettings = faction.AISettings;
				faction.FactionAI = this;
				faction.CreateAISettingsIfNull();
				FindAIPatrolSettingsIfNull();
				AssignStrategyIfNull();
				unitsUnderAttackProcessor = new FactionAIUnitsUnderAttackProcessor
				{
					FactionAI = this
				};
				fleetRearmer = new FactionAIRearmer(this);
				engine = EngineASX.Instance;
				if (engine == null)
				{
					Debug.LogError("Faction is missing  the engine", this);
				}
				attackModule = new FactionAIAttackModule
				{
					Faction = faction
				};
			}
		}

		private bool FleetCanHaveEscort(Fleet fleet)
		{
			FactionStrategy fleetStrategy = fleet.FleetStrategy;
			if (fleetStrategy == FactionStrategy.Trade || fleetStrategy == FactionStrategy.Mine || fleetStrategy == FactionStrategy.PassengerTransport)
			{
				return true;
			}
			return false;
		}

		private void FindAIPatrolSettingsIfNull()
		{
			if (AISettings.PatrolSettings == null)
			{
				AISettings.PatrolSettings = AISettings.GetComponent<FactionAIPatrolSettings>();
			}
		}

		private void FindFaction()
		{
			faction = UnityObjectHelper.FindInParentsOrSelf<Faction>(gameObject);
		}

		private void SetAllFleetStances()
		{
			foreach (Fleet fleet in faction.Fleets)
			{
				if (CanOrderFleet(fleet))
				{
					SetFleetStance(fleet);
				}
			}
		}

		private void SetFleetStance(Fleet fleet)
		{
			SetDefaultFleetStance(fleet);
			if (fleet.ActiveOrder != null)
			{
				SetFleetStanceForOrder(fleet, fleet.ActiveOrder);
			}
		}

		private void SetFleetStanceForOrder(Fleet fleet, ActiveFleetOrder order)
		{
		}

		protected virtual void SetDefaultFleetStance(Fleet fleet)
		{
			fleet.Settings.PreferToDock = DockedPreference.Undock;
			fleet.Settings.AllowCombatInterception = true;
			fleet.Settings.AllowAttack = true;
			fleet.Settings.Aggression = GetFleetAggression(fleet);
			fleet.Settings.CargoCollectionPreference = FleetCargoCollectionPreference.CompatibleEquipment | FleetCargoCollectionPreference.IncompatibleEquipment | FleetCargoCollectionPreference.TradableCargo;
		}

		private float GetFleetAggression(Fleet fleet)
		{
			if (fleet.FleetStrategy != FactionStrategy.Unspecified)
			{
				switch (fleet.FleetStrategy)
				{
				case FactionStrategy.Scout:
					return Mathf.Lerp(0.05f, 0.5f, fleet.Faction.Aggression);
				case FactionStrategy.War:
					return Mathf.Lerp(0.5f, 1f, fleet.Faction.Aggression);
				case FactionStrategy.BountyHunt:
				case FactionStrategy.Escort:
					return Mathf.Lerp(0.5f, 1f, fleet.Faction.Aggression);
				case FactionStrategy.Mine:
					return Mathf.Lerp(0.2f, 1f, fleet.Faction.Aggression);
				default:
					return Mathf.Lerp(0.1f, 0.85f, fleet.Faction.Aggression);
				}
			}
			return GetDefaultFleetAggressionForFaction(fleet.Faction);
		}

		public float GetDefaultFleetAggressionForFaction(Faction faction)
		{
			switch (faction.FactionType)
			{
			case FactionType.PassengerTransport:
			case FactionType.EquipmentDealer:
				return 0.01f;
			case FactionType.Trader:
			case FactionType.Bar:
				return 0.01f + this.faction.Aggression * 0.2f;
			case FactionType.Empire:
			case FactionType.Bandit:
			case FactionType.Outlaw:
				return 0.5f + this.faction.Aggression * 0.5f;
			case FactionType.Mercenary:
				return 0.25f + this.faction.Aggression * 0.5f;
			default:
				return 0.05f + this.faction.Aggression * 0.5f;
			}
		}

		public void UpdateHeavy()
		{
			if (enabled)
			{
				AutoSetHomeSectorPosition();
				if (AISettings.SectorControlLikelihood > 0f)
				{
					UpdateExpansion();
				}
				UpdateHeavyInternal();
				if (recentlyLostUnits.Count > 0)
				{
					RecentlyLostUnit recentlyLostUnit = recentlyLostUnits.Dequeue();
					PlaceRevengeBounty(recentlyLostUnit.UnitClass, recentlyLostUnit.Attacker, recentlyLostUnit.AttackerFaction);
				}
				AssignStrategyIfNull();
				int upgradeStationsAvailableCredits = GetUpgradeStationsAvailableCredits();
				if (upgradeStationsAvailableCredits > 10000 && UnityEngine.Random.value < GameController.Instance.GameSettings.GameplaySettings.ProbabilityOfFactionAIUpgradingStation)
				{
					UpgradeStations(upgradeStationsAvailableCredits);
				}
			}
		}

		private int GetUpgradeStationsAvailableCredits()
		{
			return (int)((float)(faction.Credits - faction.CreditsReserve - 100000) * 0.05f);
		}

		private void UpgradeStations(int availableCredits)
		{
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType == null)
			{
				return;
			}
			Unit random = unitsByType.GetRandom();
			if (random != null && random.IsValidAndNotDestroyed && !random.IsUnderAttack())
			{
				ComponentBay random2 = random.Components.Bays.GetRandom();
				if (random2 != null && ModdedUnitSeeder.ModUnitBayForFaction(random, random2, allowDowngrade: false, faction, availableCredits) != null)
				{
					EngineASX.Instance.DebugInfo.NumTimesFactionAIUpgradedStations++;
				}
			}
		}

		public void AutoSetHomeSectorPosition()
		{
			if (faction.HomeSector != null && !faction.HomeSectorPosition.HasValue)
			{
				faction.HomeSectorPosition = GetBestHomeSectorPosition(faction.HomeSector);
			}
		}

		public Vector3? GetBestHomeSectorPositionFromOwnedStations(Sector homeSector)
		{
			float num = 0f;
			Unit unit = null;
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.Sector == homeSector)
					{
						float stationScoreAsFleetHomeBase = GetStationScoreAsFleetHomeBase(item, null, 4f);
						if (unit == null || stationScoreAsFleetHomeBase > num)
						{
							num = stationScoreAsFleetHomeBase;
							unit = item;
						}
					}
				}
			}
			if (unit != null)
			{
				return unit.SectorPosition + Geometry.RandomXZUnitVector() * UnityEngine.Random.Range(200f, 800f);
			}
			return null;
		}

		public Unit GetBestHomeBaseFromFriendlyStations(Sector homeSector)
		{
			float num = 0f;
			Unit unit = null;
			List<Unit> unitsByType = homeSector.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (!item.IsMinorStation() && item.Faction != null && faction.Intel.IsUnitDiscovered(item) && !item.IsHostileTo(faction))
					{
						float stationScoreAsFleetHomeBase = GetStationScoreAsFleetHomeBase(item, null, 10f);
						if (unit == null || stationScoreAsFleetHomeBase > num)
						{
							unit = item;
							num = stationScoreAsFleetHomeBase;
						}
					}
				}
			}
			return unit;
		}

		public Vector3? GetBestHomeSectorPositionFromFriendlyStations(Sector homeSector)
		{
			Unit bestHomeBaseFromFriendlyStations = GetBestHomeBaseFromFriendlyStations(homeSector);
			if (bestHomeBaseFromFriendlyStations != null)
			{
				return bestHomeBaseFromFriendlyStations.SectorPosition + Geometry.RandomXZUnitVector() * UnityEngine.Random.Range(200f, 800f);
			}
			return null;
		}

		protected virtual Vector3? GetBestHomeSectorPosition(Sector homeSector)
		{
			return GetBestHomeSectorPositionFromOwnedStations(homeSector) ?? GetBestHomeSectorPositionFromFriendlyStations(homeSector) ?? new Vector3?(homeSector.GetRandomSectorPositionWithinGateDistance());
		}

		public void AssignStrategyIfNull()
		{
			if (strategy == null)
			{
				strategy = FactionAIStrategyModule.BuildStrategy(this);
			}
		}

		/// <summary>
		/// Per-frame AI upkeep. Called by the engine, not by Unity.
		/// </summary>
		/// <remarks>
		/// Was a MonoBehaviour Update(). There is one FactionAIBase per AI
		/// faction - 1,521 of them at 1024 sectors - so this cost ~1.1ms a frame
		/// in dispatch. EngineASX.UpdateFactions drives them from the faction
		/// list now.
		///
		/// Note the body is unchanged, including UpdateLight() running every
		/// frame while only UpdateHeavy() sits on the 3s FactionAIUpdateInterval.
		/// Throttling UpdateLight was considered and deliberately not done here:
		/// unitsUnderAttackProcessor and attackModule are the reactive half of
		/// this, and cutting them to 3s is a design change, not a refactor.
		/// </remarks>
		public void Tick()
		{
			if (engine == null || !GameController.Instance.GameSettings.DebugSettings.FactionAIUpdateEnabled)
			{
				return;
			}

			// Whether this copy is allowed to have opinions about this faction.
			//
			// Authority.OwnsFaction was written to answer exactly this and
			// asked by nothing. The answer was going to be "no" anyway -
			// ClientAuthorityProvider returns false for every faction, and says
			// why: a client does not run an economy, a reputation drift, or an
			// expansion search. So the whole of UpdateHeavy was running on
			// every machine in the session: sector expansion, station upgrades,
			// revenge bounties, and UpgradeStations spending a copy of the
			// faction's credits that the host was also spending.
			//
			// Not "the client's view drifts a little". A second, independent
			// simulation of the economy of an entire galaxy, on every client,
			// reaching nobody. And it is the reason the faction-state push
			// needed a receipt at all: the host's number and the client's
			// number were both being written, continuously, by different code.
			//
			// Not even the player's own faction is excepted, which is the part
			// worth arguing about. Repair, rearm and defending its ships look
			// harmless, but the host owns those ships too and does the same
			// work on the authoritative copy - so letting a client repeat it
			// only produces a second opinion about a hull that the client is
			// never told about anyway (condition is not on the wire; that is a
			// separate piece of work). A repair the client performs for itself
			// would be a thing it invented, and would have to be undone later.
			//
			// The host, and a game with nobody else in it, are untouched:
			// OwnsFaction returns true for everything when this copy is not a
			// client.
			if (!Authority.OwnsFaction(faction))
			{
				return;
			}

			UpdateLight();

			if (Time.time > faction.NextFactionAIUpdate)
			{
				UpdateHeavy();
				faction.NextFactionAIUpdate = Time.time + GameController.Instance.GameSettings.PerformanceSettings.FactionAIUpdateInterval;
			}
		}

		protected virtual void UpdateLight()
		{
			RepairFleets();
			RearmFleets();
			if (unitsUnderAttackProcessor != null)
			{
				unitsUnderAttackProcessor.Update();
			}
			if (attackModule != null)
			{
				attackModule.Update();
			}
			if (expansionSearch != null)
			{
				UpdateExpansionSearch();
			}
			if (stationBuildRequestProcessor != null)
			{
				stationBuildRequestProcessor.Tick();
			}
		}

		public void CacheMostPowerfulFleet()
		{
			mostPowerfulFleetCombatRating = 0f;
			foreach (Fleet fleet in faction.Fleets)
			{
				if (fleet != null)
				{
					mostPowerfulFleetCombatRating = Mathf.Max(mostPowerfulFleetCombatRating, fleet.GetCachedSimpleCombatRating());
				}
			}
		}

		private void UpdateExpansion()
		{
			if (!(mostPowerfulFleetCombatRating < 11f) && expansionSearch == null && Time.time > lastExpansionSearchTime + GameController.Instance.GameSettings.FactionSettings.TimeBetweenExpansionSearches)
			{
				if (EngineASX.Instance.ScenarioElapsedTime > (double)GameController.Instance.GameSettings.GameplaySettings.MinTimeBeforeFactionSectorExpansion && UnityEngine.Random.value < AISettings.SectorControlLikelihood && faction.Fleets.Count > 0)
				{
					StartExpansionSearch();
				}
				lastExpansionSearchTime = Time.time;
			}
		}

		private void UpdateExpansionSearch()
		{
			if (expansionSearch.HasFinished)
			{
				if (expansionSearch.BestSector != null)
				{
					OnExpansionSearchFoundBestSector(expansionSearch.BestSector);
				}
				expansionSearch = null;
			}
			else
			{
				expansionSearch.Process();
			}
		}

		private void StartExpansionSearch()
		{
			expansionSearch = new FactionEmpireExpansionSearch
			{
				ScoreControlledSectors = true
			};
			expansionSearch.Initialise(this);
		}

		private void OnExpansionSearchFoundBestSector(Sector sector)
		{
			if (sector.ControllingFaction == faction)
			{
				Debug.LogError("Expansion search thinks we should expand into our own controlled sector", this);
				expansionSearch = null;
				return;
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log(string.Format("Finished expansion search for {0}. Best sector found: {1} Controlled by: {2}", faction, sector, (sector != null) ? ((object)sector.ControllingFaction) : ((object)"N/A")), faction, 1);
			}
			if (sector.ControllingFaction != null)
			{
				if (faction.IsHostileTo(sector.ControllingFaction) || !(AISettings.SectorControlLikelihood > 0.75f))
				{
					return;
				}
				float opinion = faction.GetOpinion(sector.ControllingFaction);
				if (opinion + faction.Cooperation < 0.25f)
				{
					float num = 0f;
					if (opinion < 0f)
					{
						num = Mathf.Abs(opinion) * 14f;
					}
					float num2 = num + faction.Aggression * 14f + faction.Greed * 14f;
					float num3 = 1f + UnityEngine.Random.value * num2;
					num3 *= 1f - faction.Cooperation;
					FactionRecentDamageController.HandleDamageFromSource(faction, num3, sector.ControllingFaction, null, DamageDirectType.Indirect);
				}
				else if (faction.Greed > 0.25f || opinion < 0f)
				{
					float num4 = 0.02f * faction.Greed;
					faction.ChangeOpinion(sector.ControllingFaction, 0f - num4);
				}
			}
			else if (CanStartConstructionOfSectorControl(sector))
			{
				Vector3? newStationSectorPosition = WorldStationSeeder.GetNewStationSectorPosition(GameController.Instance.UnitClasses.SectorHQ, sector, faction);
				if (newStationSectorPosition.HasValue)
				{
					RequestToBuildNewStation(GameController.Instance.UnitClasses.SectorHQ, sector, newStationSectorPosition.Value);
				}
			}
		}

		public bool RequestToBuildNewStation(UnitClass unitClass, Sector sector, Vector3 sectorPosition)
		{
			if (stationBuildRequestProcessor == null)
			{
				stationBuildRequestProcessor = new FactionStationBuildRequestProcessor(this);
			}
			return stationBuildRequestProcessor.RequestToBuildNewStation(unitClass, sector, sectorPosition);
		}

		private bool CanStartConstructionOfSectorControl(Sector sector)
		{
			if (sector.GetCountOfStationPurpose(StationPurpose.SectorControl) > 0)
			{
				return false;
			}
			return faction.IsAffordableConsideringReserve(Mathf.RoundToInt((float)GameController.Instance.UnitClasses.SectorHQ.SaleCost * 1.25f));
		}

		public void OnNewGame()
		{
			if (faction.IsFreelancer && faction.LeaderPerson == null)
			{
				if (faction.People.Count == 0)
				{
					faction.LeaderPerson = CreateLeaderPilot();
				}
				else
				{
					PickLeaderFromExisingPilotsIfNone();
				}
			}
			onNewGame();
			lastExpansionSearchTime = Time.time - UnityEngine.Random.value * GameController.Instance.GameSettings.FactionSettings.TimeBetweenExpansionSearches;
			if (shipBuilder != null)
			{
				nextBuildUnitsTime = EngineASX.Instance.ScenarioElapsedTime + (double)UnityEngine.Random.Range(GameController.Instance.GameSettings.GameplaySettings.MinTimeBeforeFactionShipBuild, GameController.Instance.GameSettings.GameplaySettings.MaxTimeBeforeFactionShipBuild);
			}
			rearmCooldownTime = UnityEngine.Random.Range(60f, 240f);
		}

		public void ValidateSettings()
		{
			ShipBuildSectors.TrimNulls();
			if (ShipBuildSectorMode == FactionSpawnMode.SpecificSectors && ShipBuildSectors.Count == 0)
			{
				Debug.LogWarningFormat(this, "\"{0}\": Spawn mode set to specific scenes but no scenes listed", this);
			}
		}

		public static Fleet SpawnFleet(Faction faction, Fleet fleetPrefab, Sector sector, Vector3 sectorPosition, Unit homeBase = null)
		{
			if (fleetPrefab != null)
			{
				Sector sector2 = ((homeBase != null) ? homeBase.Sector : sector);
				Fleet fleet = UnityObjectHelper.InstantiateAndGetComponent(fleetPrefab, sector2.transform);
				fleet.Init();
				fleet.Faction = faction;
				if (faction.PreferredFormationStyle != null)
				{
					fleet.FleetFormation = EngineASX.Instance.GetFleetFormationById(faction.PreferredFormationStyle.UniqueId);
				}
				else
				{
					NpcFleetFormationStylePreference randomWeighted = GameController.Instance.GameSettings.FormationSettings.NpcFleetFormationStylePreferences.GetRandomWeighted();
					if (randomWeighted != null)
					{
						fleet.FleetFormation = EngineASX.Instance.GetFleetFormationById(randomWeighted.FleetFormationStyle.UniqueId);
					}
					fleet.Settings.FormationTightness = UnityEngine.Random.Range(0.8f, 1f);
				}
				if (homeBase != null)
				{
					fleet.SetHomeBaseToUnit(homeBase);
				}
				fleet.Sector = sector2;
				fleet.Settings.DestroyWhenNoPilots = true;
				if (homeBase != null)
				{
					Vector3 checkSectorPosition = homeBase.SectorPosition + Maths.RandomXZDirection() * UnityEngine.Random.Range(homeBase.UnitClass.ShieldRingRadius, homeBase.UnitClass.ShieldRingRadius * 3f);
					fleet.transform.localPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(fleet.Sector, checkSectorPosition, 100f, GameController.Instance.NonOVerlappingUnitsMask);
				}
				else
				{
					fleet.transform.localPosition = sectorPosition;
				}
				return fleet;
			}
			Debug.LogError("Cannot create fleet. Prefab is null", faction);
			return null;
		}

		public int GetAvailableRequisitionPoints()
		{
			return GetTotalRpProvision() - GetTotalRpUsage();
		}

		public int GetTotalRpProvision()
		{
			return (int)((float)TotalRpProvision * faction.RequisitionPointMultiplier * engine.World.AIRequisitionPointMultiplier);
		}

		public int GetTotalRpUsage()
		{
			int num = 0;
			foreach (Unit unit in faction.Units)
			{
				if (unit != null && unit.IsValidAndNotDestroyed)
				{
					num += unit.UnitClass.RpCost;
				}
			}
			return num;
		}

		[ContextMenu("AutoName GameObject")]
		public void AutoNameGameObject()
		{
			name = $"FactionAI_{AIType}";
		}

		protected virtual void onNewGame()
		{
		}

		protected virtual void UpdateHeavyInternal()
		{
			if (!engine.World.AllowFactionRetire || !RetireFactionWhenNeeded())
			{
				TryCreateOrPickLeaderIfNone();
				UpdateIdleNpcs();
				UpdateBuilding();
				TrimInvalidDistressCalls();
				updateHeavy();
			}
		}

		protected virtual void updateHeavy()
		{
		}

		private void UpdateBuilding()
		{
			if (!(engine.ScenarioElapsedTime > nextBuildUnitsTime))
			{
				return;
			}
			faction.FindAndPickHomeSectorIfNull();
			AssignStrategyIfNull();
			if (HomeSector != null && strategy != null)
			{
				if (engine.World.AllowFactionStationBuild)
				{
					OnBuildStations();
				}
				if (engine.World.AllowFactionShipsBuild && AISettings.BuildShips)
				{
					OnBuildShips();
				}
			}
			SetNextBuildUnitsTime();
		}

		protected virtual void OnBuildShips()
		{
			bool includeUnaffordableFromCredits = true;
			if (faction.IsFreelancer || faction.GetCountOfUnitType(UnitType.Ship) + faction.GetCountOfUnitType(UnitType.Station) == 0)
			{
				includeUnaffordableFromCredits = false;
			}
			ShipBuilder.BuildShips(1f, 4, includeUnaffordableFromCredits, includeUnaffordableFromRp: true);
		}

		protected virtual void OnBuildStations()
		{
			StationBuilder.BuildStations();
		}

		public bool CanRequestToBuildNewStations()
		{
			if (stationBuildRequestProcessor != null)
			{
				return stationBuildRequestProcessor.CanRequestToBuildNewStations();
			}
			return true;
		}

		private void UpdateIdleNpcs()
		{
			if (!(Time.time > nextCheckForIdles))
			{
				return;
			}
			CreatePilotsAndHandleSurplusShips(!faction.IsFreelancer);
			CreateFleets();
			nextCheckForIdles = Time.time + 5f;
			if (idleFleetQueueChecker.Count == 0)
			{
				foreach (Fleet fleet2 in faction.Fleets)
				{
					if (fleet2.IdleAndNoObjectives)
					{
						idleFleetQueueChecker.Enqueue(fleet2);
					}
				}
				return;
			}
			Fleet fleet = idleFleetQueueChecker.Dequeue();
			if (fleet != null && fleet.IdleAndNoObjectives && CanOrderFleet(fleet))
			{
				AssignOrdersToIdleFleet(fleet);
			}
		}

		public void CreatePilotsAndHandleSurplusShips(bool createPilots)
		{
			IdleUnitCache.Clear();
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && CanUseShip(item))
					{
						UnitComponentHolder components = item.Components;
						if (components != null && components.PilotPerson == null)
						{
							IdleUnitCache.Enqueue(components.Unit, components.Unit.CombatRating);
						}
					}
				}
			}
			if (IdleUnitCache.Count == 0)
			{
				return;
			}
			int num = int.MaxValue;
			if (faction.AISettings.FixedShipCount > 0)
			{
				num = Mathf.Min(faction.AISettings.FixedShipCount, num);
			}
			int num2 = 0;
			Person pilotPrefab = GetPilotPrefab();
			DismantleCache.Clear();
			while (IdleUnitCache.Count > 0 && num2 < num)
			{
				Unit value = IdleUnitCache.Dequeue().Value;
				if (createPilots)
				{
					FindOrCreateNpcPilotForUnit(pilotPrefab, value);
					num2++;
				}
				else if (FindNpcPilotForUnit(pilotPrefab, value) != null)
				{
					num2++;
				}
				else
				{
					DismantleCache.Enqueue(value);
				}
			}
			while (DismantleCache.Count > 0)
			{
				Unit unit = DismantleCache.Dequeue();
				if (unit.CanDismantleNow(faction))
				{
					unit.Components.StartDismantle();
					EngineASX.Instance.DebugInfo.NumFactionAISurplusShipsStartedDismantling++;
				}
			}
		}

		public void CreateFleets()
		{
			Fleet fleetPrefab = GetFleetPrefab();
			Person pilotPrefab = GetPilotPrefab();
			if (!(fleetPrefab != null))
			{
				return;
			}
			PopulatePilottedUnitsWithoutAFleet();
			while (IdleUnitCache.Count > 0)
			{
				Unit value = IdleUnitCache.Dequeue().Value;
				Fleet bestFleetForUnit = GetBestFleetForUnit(value);
				if (bestFleetForUnit != null)
				{
					Fleet fleet = CreateFleetAndNpcForUnitAndAssignStrategy(fleetPrefab, pilotPrefab, value);
					if (bestFleetForUnit.HomeBase != null)
					{
						fleet.SetHomeBase(bestFleetForUnit.HomeBase.Copy());
					}
					JoinFleetOrder joinFleetOrder = UnityObjectHelper.NewGameObject<JoinFleetOrder>();
					joinFleetOrder.TargetFleet = bestFleetForUnit;
					joinFleetOrder.MaxJumpDistance = int.MaxValue;
					joinFleetOrder.MaxDuration = 600f;
					fleet.EnqueueOrder(joinFleetOrder);
				}
				else
				{
					Fleet fleet2 = CreateFleetAndNpcForUnitAndAssignStrategy(fleetPrefab, pilotPrefab, value);
					fleet2.SetHomeBase(FindBestHomeBaseForFleet(fleet2));
				}
			}
		}

		public Fleet GetBestFleetForUnit(Unit unit)
		{
			if (faction.Fleets.Count > 0)
			{
				float num = 0f;
				Fleet fleet = null;
				{
					foreach (Fleet fleet2 in faction.Fleets)
					{
						if (fleet2 != null && fleet2.IsValid && CanGroupUnitIntoFleet(unit, fleet2, out var pilotCount))
						{
							float groupUnitIntoFleetScore = GetGroupUnitIntoFleetScore(unit, fleet2, pilotCount);
							if (fleet == null || num > groupUnitIntoFleetScore)
							{
								fleet = fleet2;
								num = groupUnitIntoFleetScore;
							}
						}
					}
					return fleet;
				}
			}
			return null;
		}

		public virtual bool CanGroupUnitIntoFleet(Unit unit, Fleet fleet, out int pilotCount)
		{
			pilotCount = fleet.PilotCount;
			if (pilotCount > 0 && fleet.HasOrderToJoinFleet())
			{
				return false;
			}
			float effectivenessAtStrategy = unit.UnitClass.GetEffectivenessAtStrategy(fleet.FleetStrategy);
			if (effectivenessAtStrategy == 0f)
			{
				return false;
			}
			if (effectivenessAtStrategy + UnityEngine.Random.value * 0.2f < 0.2f)
			{
				return false;
			}
			if (pilotCount == 0)
			{
				return true;
			}
			int preferredFleetShipCount = GetPreferredFleetShipCount(fleet);
			if (pilotCount > preferredFleetShipCount)
			{
				return false;
			}
			if (!CanMergeFleetsWithDifferentCloakCapabilities() && unit.Components.CloakComponent != null != (fleet.Ships[0].CloakComponent != null))
			{
				return false;
			}
			foreach (Fleet fleet2 in faction.Fleets)
			{
				foreach (FleetOrder item in fleet2.OrderQueue)
				{
					if (item is JoinFleetOrder joinFleetOrder && joinFleetOrder.TargetFleet == fleet)
					{
						pilotCount += fleet2.Ships.Count;
					}
				}
				if (fleet2.ActiveOrder is ActiveJoinFleetOrder activeJoinFleetOrder && activeJoinFleetOrder.JoinFleetOrder.TargetFleet == fleet)
				{
					pilotCount += fleet2.Ships.Count;
				}
			}
			if (pilotCount >= Mathf.Min(preferredFleetShipCount, AISettings.MaxGroupUnitCount))
			{
				return false;
			}
			return true;
		}

		protected virtual bool CanMergeFleetsWithDifferentCloakCapabilities()
		{
			return false;
		}

		private int GetPreferredFleetShipCount(Fleet fleet)
		{
			return fleet.FleetStrategy switch
			{
				FactionStrategy.BountyHunt => UnityEngine.Random.Range(1, 4), 
				FactionStrategy.Trade => UnityEngine.Random.Range(1, 3), 
				FactionStrategy.Explore => UnityEngine.Random.Range(1, 3), 
				FactionStrategy.DealEquipment => 1, 
				FactionStrategy.Scavenge => UnityEngine.Random.Range(1, 3), 
				FactionStrategy.Escort => UnityEngine.Random.Range(1, 5), 
				FactionStrategy.PassengerTransport => 1, 
				FactionStrategy.Mine => UnityEngine.Random.Range(1, 3), 
				FactionStrategy.Scout => UnityEngine.Random.Range(1, 3), 
				FactionStrategy.War => UnityEngine.Random.Range(2, 8), 
				_ => 1, 
			};
		}

		private float GetGroupUnitIntoFleetScore(Unit unit, Fleet fleet, int pilotcount)
		{
			float num = 0f;
			int jumpDistanceTo = unit.Sector.GetJumpDistanceTo(fleet.Sector);
			if ((float)jumpDistanceTo > 0f)
			{
				num -= (float)jumpDistanceTo * 5f;
			}
			num -= Mathf.Clamp01(fleet.GetCachedTotalCargoCapacity() / 200f);
			return num - (float)pilotcount * 1f;
		}

		public void TryCreateOrPickLeaderIfNone()
		{
			if (!TryCreateLeaderAtOwnedStationIfNone())
			{
				PickLeaderFromExisingPilotsIfNone();
			}
		}

		private void PickLeaderFromExisingPilotsIfNone()
		{
			if (faction.LeaderPerson == null || faction.LeaderPerson.Faction != faction)
			{
				faction.LeaderPerson = FactionLeaderPicker.PickBasedOnRank(faction);
			}
		}

		private bool TryCreateLeaderAtOwnedStationIfNone()
		{
			if (faction.IsFreelancer)
			{
				return false;
			}
			if (faction.LeaderPerson == null)
			{
				Unit bestStationForLeader = GetBestStationForLeader();
				if (bestStationForLeader != null)
				{
					Person person = CreateLeaderPilot();
					person.Faction = faction;
					person.CurrentUnit = bestStationForLeader;
					faction.LeaderPerson = person;
					return true;
				}
			}
			return false;
		}

		public void PlaceLeaderAtBestStation()
		{
			Person leaderPerson = faction.LeaderPerson;
			if (!(leaderPerson != null))
			{
				return;
			}
			Unit bestStationForLeader = GetBestStationForLeader();
			if (bestStationForLeader != null)
			{
				if (leaderPerson.NpcPilot != null)
				{
					leaderPerson.NpcPilot.Fleet = null;
				}
				leaderPerson.CurrentUnit = bestStationForLeader;
			}
		}

		private Person CreateLeaderPilot()
		{
			Person pilotPrefab = GetPilotPrefab();
			if (pilotPrefab != null)
			{
				Person person = UnityEngine.Object.Instantiate(pilotPrefab);
				if (person != null)
				{
					person.Init();
					person.RandomizeNameAndPersonalityAndAssignDialog();
					person.DestroyGameObjectOnKill = true;
				}
				return person;
			}
			return null;
		}

		private Unit GetBestStationForLeader()
		{
			Unit unit = null;
			float num = 0f;
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && CanStationHostLeader(item) && (GetStationScoreForLeader(item) > num || unit == null))
					{
						unit = item;
						num = item.UnitClass.SaleCost;
					}
				}
			}
			return unit;
		}

		public float GetStationScoreForLeader(Unit station)
		{
			float num = 0f;
			if (faction.HomeSector != null && station.Sector == faction.HomeSector)
			{
				num += 10f;
			}
			return num + GetStationScoreForLeaderFromStationPurpose(station.UnitClass.StationPurpose);
		}

		public float GetStationScoreForLeaderFromStationPurpose(StationPurpose stationPurpose)
		{
			switch (stationPurpose)
			{
			case StationPurpose.TradeStation:
				return 10f;
			case StationPurpose.Shipyard:
				return 8f;
			case StationPurpose.Factory:
			case StationPurpose.Scrapyard:
				return 5f;
			default:
				return 1f;
			}
		}

		public static bool CanStationHostLeader(Unit station)
		{
			return station.IsDockable;
		}

		public void SetNextBuildUnitsTime()
		{
			nextBuildUnitsTime = Engine.ScenarioElapsedTime + (double)UnityEngine.Random.Range(engine.GameSettings.FactionSettings.MinAIBuildUnitsInterval, engine.GameSettings.FactionSettings.MaxAIBuildUnitsInterval);
		}

		private bool RetireFactionWhenNeeded()
		{
			if (FactionRetirer.ShouldRetireFaction(faction))
			{
				FactionRetirer.RetireFaction(faction);
				return true;
			}
			return false;
		}

		public void SetLastBuiltUnitTimeToCurrent()
		{
			LastBuiltUnitTime = EngineASX.Instance.ScenarioElapsedTime;
		}

		public void RegisterUnitUnderAttack(Unit attackedUnit, Fleet fleet, Faction attackingFaction, Unit attackingUnit, RecentAttackType attackType)
		{
			unitsUnderAttackProcessor.RegisterUnitUnderAttack(attackedUnit, fleet, attackingUnit, attackingFaction, attackType);
		}

		private bool FactionTypeShouldSendDistressCalls()
		{
			return true;
		}

		public void SendDistressCalls(Unit attackedUnit, Faction attackingFaction, Unit attackingUnit)
		{
			if (!(attackedUnit.Sector != null) || !FactionTypeShouldSendDistressCalls() || !(attackingUnit != null) || !attackingUnit.IsValidAndNotDestroyed || !(attackedUnit != null) || !attackedUnit.IsValidAndNotDestroyed || !(attackingFaction != null) || !faction.IsHostileTo(attackingFaction))
			{
				return;
			}
			Fleet fleet = attackedUnit.GetFleet();
			if (fleet != null && CanOrderFleet(fleet))
			{
				double value = 0.0;
				if ((!lastSentDistressCallTimes.TryGetValue(fleet.UniqueId, out value) || engine.ScenarioElapsedTime - value > 120.0) && UnityEngine.Random.value < engine.GameSettings.FactionSettings.ProbabilityOfDistressCall * FactionTypeInfo.DistressCallProbabilityMultiplier && FleetNeedsToSendDistressCall(fleet, attackingUnit))
				{
					SendDistressCallAndToPlayer(attackedUnit, attackingFaction, attackingUnit, fleet);
				}
			}
		}

		public void SendDistressCallAndToPlayer(Unit attackedUnit, Faction attackingFaction, Unit attackingUnit, Fleet attackedFleet)
		{
			SendDistressCall(attackedUnit, attackedFleet, attackingUnit, attackingFaction);
			if (engine.LocalUnit != null)
			{
				float num = GameController.Instance.GameSettings.FactionSettings.ProbabilityOfDistressCallGoingToPlayer;
				if (attackedUnit.Faction != null && attackedUnit.Faction.FactionTypeInfo != null)
				{
					num *= attackedUnit.Faction.FactionTypeInfo.ProbabilityOfDistressCallGoingToPlayerMultiplier;
				}
				if (UnityEngine.Random.value < num && engine.LocalUnit.Sector.GetJumpDistanceTo(attackedUnit.Sector) <= 1)
				{
					EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(attackedUnit);
					lastSentDistressCallTimes[attackedFleet.UniqueId] = engine.ScenarioElapsedTime;
					SendDistressCallToLocalPlayer(attackedUnit, attackingUnit, attackingFaction);
				}
			}
		}

		public ICommsHandler GetCommsHandlerForPilot(Person pilot)
		{
			commsHandler.OwnerPilot = pilot;
			commsHandler.OwnerFaction = faction;
			return commsHandler;
		}

		private bool FleetNeedsToSendDistressCall(Fleet attackedFleet, Unit attackingUnit)
		{
			return AICombatProbabilityCalculator.GetSimpleFleetProbabilityAgainstUnitOrUnitFleet(attackedFleet, attackingUnit) < 0.4f;
		}

		private void SendDistressCall(Unit attackedUnit, Fleet attackedGroup, Unit attackingUnit, Faction attackingFaction)
		{
			DistressCall item = new DistressCall
			{
				TimeOfCall = Time.time,
				SenderFaction = faction,
				SenderGroup = attackedGroup,
				AttackingFaction = attackingFaction,
				SenderUnit = attackedUnit,
				Sector = attackedUnit.Sector,
				SectorPosition = attackedUnit.SectorPosition
			};
			EngineASX.Instance.DebugInfo.NumDistressCallsSent++;
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.IsAIFactionType)
				{
					recentDistressCalls.Add(item);
				}
			}
		}

		public bool RequestEnforcePeace(FactionAttitude attitude)
		{
			if (Faction.IsAlwaysHostileToFaction(attitude.TargetFaction) || attitude.TargetFaction.IsAlwaysHostileToFaction(faction))
			{
				return false;
			}
			FactionPeaceController.EnforcePeace(this, attitude);
			return true;
		}

		public virtual bool CanBuildStation(UnitClass unitClass)
		{
			return true;
		}

		public virtual int GetPreferredCountOfUnitClass(UnitClass unitClass)
		{
			switch (unitClass.StationPurpose)
			{
			case StationPurpose.Defence:
				return faction.GetValidNonMinorStationCount() * 2;
			case StationPurpose.Repair:
				return 1 + faction.ControlledSectorCount / 3;
			case StationPurpose.TradeStation:
				return 2 + faction.ControlledSectorCount / 3;
			case StationPurpose.Refinery:
				return 3 + faction.ControlledSectorCount / 3;
			case StationPurpose.Scrapyard:
				return 1 + faction.ControlledSectorCount / 2;
			case StationPurpose.Factory:
				if (unitClass.UniqueID == GameController.Instance.UnitClasses.Lab.UniqueID)
				{
					return 1 + faction.ControlledSectorCount / 3;
				}
				return 3 + faction.ControlledSectorCount / 2;
			case StationPurpose.Shipyard:
				return 1 + faction.ControlledSectorCount / 2;
			case StationPurpose.Equipment:
				return 1 + faction.ControlledSectorCount / 2;
			case StationPurpose.Outpost:
				return 1 + faction.ControlledSectorCount / 2;
			case StationPurpose.Satellite:
				return faction.ControlledSectorCount;
			default:
				return Mathf.CeilToInt((float)engine.Sectors.Count / 3f);
			}
		}

		public virtual Unit GetUnitToDefend(Fleet fleet, out float priority)
		{
			defendUnitCache.Clear();
			priority = 0.2f;
			List<Unit> unitsByType = fleet.Faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.IsValidAndNotDestroyed && item.UnitClass.StationPurpose != StationPurpose.None && !item.IsMinorStation() && (!defendTargetCooldownTime.TryGetValue(item.UniqueId, out var value) || Time.time > value))
					{
						float unitDefendOrderPriority = GetUnitDefendOrderPriority(item);
						float num = unitDefendOrderPriority;
						int jumpDistanceTo = fleet.GetHomeSectorOrCurrent().GetJumpDistanceTo(item.Sector);
						num -= (float)jumpDistanceTo * 4f;
						if (item.IsUnderAttack())
						{
							num *= 8f;
						}
						defendUnitCache.Add(new WeightedUnit
						{
							Weight = num,
							Unit = item,
							DefendPriority = unitDefendOrderPriority
						});
					}
				}
			}
			WeightedUnit randomWeighted = defendUnitCache.GetRandomWeighted();
			if (randomWeighted != null)
			{
				bool flag = randomWeighted.Unit.IsUnderAttack();
				priority = randomWeighted.DefendPriority;
				if (flag)
				{
					priority *= 2f;
				}
				return randomWeighted.Unit;
			}
			return null;
		}

		public virtual bool TryOrderFleetToDefend(Fleet fleet)
		{
			Unit unitToDefend = GetUnitToDefend(fleet, out var priority);
			if (unitToDefend != null)
			{
				defendTargetCooldownTime[unitToDefend.UniqueId] = Time.time + 120f;
				OrderFleetToProtectUnit(fleet, unitToDefend, priority);
				return true;
			}
			OrderDefensivePatrol(fleet);
			return true;
		}

		private void OrderDefensivePatrol(Fleet fleet)
		{
			EngineASX.Instance.DebugInfo.NumTimesFleetOrderedToDefensivePatrol++;
			fleet.Faction.FactionAI.GetPatrolSettingsOrDefault();
			PatrolOrder patrolOrder = PatrolRouteCreator.CreatePatrolObjective(fleet.Faction, (fleet.HomeSector != null) ? fleet.HomeSector : fleet.Sector, 1, 1, GetPatrolSettingsOrDefault().MinPatrolNodesInSector, GetPatrolSettingsOrDefault().MaxPatrolNodesInSector, 0, considerStationsAsNodes: true);
			patrolOrder.Priority = 0.2f;
			AssignDefaultOrderMaxDuration(patrolOrder);
			fleet.EnqueueOrder(patrolOrder);
			fleet.Faction.FactionAI.LastOrderedPatrolTime = fleet.Engine.ScenarioElapsedTime;
			OnFleetOrdered(fleet);
		}

		public ProtectOrder OrderFleetToProtectUnit(Fleet fleet, Unit unit, float priority)
		{
			SectorTarget sectorTarget = SectorTarget.FromUnit(unit);
			float num = 50f;
			if (unit.IsStation())
			{
				num += UnityEngine.Random.Range(350f, 500f);
			}
			sectorTarget.RelativeTargetPosition = Geometry.RandomXZUnitVector() * (num + unit.UnitClass.ShieldRingRadius * 2f);
			ProtectOrder protectOrder = OrderFleetToProtectSectorTarget(fleet, sectorTarget);
			protectOrder.Priority = priority;
			return protectOrder;
		}

		public ProtectOrder OrderFleetToProtectSectorTarget(Fleet fleet, SectorTarget sectorTarget)
		{
			fleet.ClearOrders();
			ProtectOrder protectOrder = UnityObjectHelper.NewGameObject<ProtectOrder>();
			protectOrder.Target = sectorTarget;
			protectOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			protectOrder.MaxDuration = 360f;
			fleet.EnqueueOrder(protectOrder);
			fleet.AssignNextOrder();
			OnFleetOrdered(fleet);
			return protectOrder;
		}

		public void OrderFleetToProtectFleet(Fleet fleet, Fleet targetFleet)
		{
			fleet.ClearOrders();
			ProtectOrder protectOrder = UnityObjectHelper.NewGameObject<ProtectOrder>();
			protectOrder.Target = new SectorTarget
			{
				TargetFleet = targetFleet
			};
			protectOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			float num = targetFleet.VeryBasicFleetRadiusCalculation() + fleet.VeryBasicFleetRadiusCalculation();
			protectOrder.Target.RelativeTargetPosition = Geometry.RandomXZUnitVector() * (num * 1.2f);
			protectOrder.MaxDuration = 360f;
			fleet.EnqueueOrder(protectOrder);
			fleet.AssignNextOrder();
			OnFleetOrdered(fleet);
		}

		internal void OnUnitScanned(Unit scannedUnit, Unit scanningShip)
		{
			if (faction.RecentDamageSettings.ShipScanDamageEnabled && scanningShip.Faction != null && scanningShip.Faction != this && (faction.FactionType == FactionType.Bandit || faction.FactionType == FactionType.Outlaw))
			{
				Person pilot = scannedUnit.GetPilot();
				FactionRecentDamageSettings recentDamageSettings = faction.RecentDamageSettings;
				if (pilot != null && pilot.Aggression > recentDamageSettings.ScannedShipDamageMinAggression && faction.Intel.IsUnitDiscoveredUsingDefaultDiscoveryAge(scanningShip) && faction.GetOrCreateAttitude(scanningShip.Faction).Opinion < recentDamageSettings.ScannedShipDamageMaxOpinion)
				{
					float damage = (pilot.Aggression - recentDamageSettings.ScannedShipDamageMinAggression) / (1f - recentDamageSettings.ScannedShipDamageMinAggression) * recentDamageSettings.ScannedShipMaxDamage;
					faction.HandleDamageFromSource(damage, scanningShip.Faction, scanningShip, DamageDirectType.Direct);
				}
			}
		}

		private bool TryOrderNavalGroupToRespondToDistressCall(Fleet fleet)
		{
			int? respondableDistressCallForRescue = GetRespondableDistressCallForRescue(fleet);
			if (respondableDistressCallForRescue.HasValue)
			{
				DistressCall distressCall = recentDistressCalls[respondableDistressCallForRescue.Value];
				MoveToOrder moveToOrder = UnityObjectHelper.NewGameObject<MoveToOrder>();
				moveToOrder.Target = new SectorTarget
				{
					SectorPosition = distressCall.SectorPosition,
					Sector = distressCall.Sector
				};
				moveToOrder.MaxDuration = 600f;
				moveToOrder.CompleteOnReachTarget = true;
				moveToOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
				moveToOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
				fleet.EnqueueOrder(moveToOrder);
				recentDistressCalls.RemoveAt(respondableDistressCallForRescue.Value);
				TrimInvalidDistressCalls();
				OnFleetOrdered(fleet);
				return true;
			}
			return false;
		}

		protected int? GetRespondableDistressCallForScavenge(Fleet responderFleet)
		{
			if (responderFleet.Sector == null)
			{
				return null;
			}
			for (int i = 0; i < recentDistressCalls.Count; i++)
			{
				DistressCall distressCall = recentDistressCalls[i];
				if (!(distressCall.Sector == null) && (!(Time.time - distressCall.TimeOfCall < 400f) || !AreDistressCallParticipantsHostileToUs(ref distressCall)) && responderFleet.Sector.GetJumpDistanceTo(distressCall.Sector) <= responderFleet.Settings.MaxJumpDistance && responderFleet.Faction.Intel.HasPathFromToSector(responderFleet.Sector, distressCall.Sector))
				{
					return i;
				}
			}
			return null;
		}

		protected bool AreDistressCallParticipantsHostileToUs(ref DistressCall distressCall)
		{
			if (!(distressCall.SenderFaction != null) || !distressCall.SenderFaction.IsHostileTo(faction))
			{
				if (distressCall.AttackingFaction != null)
				{
					return distressCall.AttackingFaction.IsHostileTo(faction);
				}
				return false;
			}
			return true;
		}

		private int? GetRespondableDistressCallForRescue(Fleet responderGroup)
		{
			for (int i = 0; i < recentDistressCalls.Count; i++)
			{
				DistressCall distressCall = recentDistressCalls[i];
				if (distressCall.AttackingFaction != null && distressCall.SenderGroup != responderGroup && distressCall.SenderFaction != null && !responderGroup.Faction.IsHostileTo(distressCall.SenderFaction) && responderGroup.Faction.IsHostileTo(distressCall.AttackingFaction) && responderGroup.Sector != null && distressCall.Sector != null && responderGroup.Sector.GetJumpDistanceTo(distressCall.Sector) <= 1 && responderGroup.Faction.Intel.HasPathFromToSector(responderGroup.Sector, distressCall.Sector))
				{
					return i;
				}
			}
			return null;
		}

		public virtual void NotifyScannedHostileTargetByFleet(Fleet scanner, Unit hostileUnit, float distance)
		{
			if (attackModule != null)
			{
				attackModule.NotifyScannedHostileTargetByFleet(scanner, hostileUnit, distance);
			}
		}

		public virtual void NotifyScannedHostileTargetByNpc(NpcPilot npcPilot, Unit hostileUnit, float distance)
		{
			if (attackModule != null)
			{
				attackModule.NotifyScannedHostileTargetByNpc(npcPilot, hostileUnit, distance);
			}
		}

		public void TrimInvalidDistressCalls()
		{
			for (int i = 0; i < recentDistressCalls.Count; i++)
			{
				if (IsDistressCallStale(recentDistressCalls[i].TimeOfCall))
				{
					recentDistressCalls.RemoveAt(i);
					i--;
				}
			}
		}

		protected virtual bool IsDistressCallStale(float timeOfCall)
		{
			return Time.time - timeOfCall > 480f;
		}

		private void SendDistressCallToLocalPlayer(Unit attackedUnit, Unit attackingUnit, Faction sourceFaction)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("To all friendly pilots. I am under attack in the ");
			stringBuilder.Append(attackedUnit.Sector.Name);
			stringBuilder.Append(" sector");
			if (sourceFaction != null)
			{
				stringBuilder.Append(" by");
				if (!sourceFaction.IsFreelancer)
				{
					string text = ((attackingUnit.UnitType == UnitType.Ship) ? "ships" : "hostile targets");
					stringBuilder.Append(" " + text + " belonging to ");
					if (sourceFaction.FactionType == FactionType.Outlaw)
					{
						stringBuilder.Append("the " + sourceFaction.GetLongNameElseShort() + " gang");
					}
					else
					{
						stringBuilder.Append(sourceFaction.GetLongNameElseShort());
					}
				}
				else
				{
					switch (sourceFaction.FactionType)
					{
					case FactionType.BountyHunter:
						stringBuilder.Append(" a bounty hunter");
						break;
					case FactionType.Bandit:
					case FactionType.Outlaw:
						stringBuilder.Append(" pirates");
						break;
					default:
					{
						string text2 = ((attackingUnit.UnitType == UnitType.Ship) ? "hostile ships" : "hostile targets");
						stringBuilder.Append(" " + text2);
						break;
					}
					}
				}
			}
			stringBuilder.Append(". Please send immediate help.");
			string messageText = stringBuilder.ToString();
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage
			{
				SubjectText = "Distress Call in " + attackedUnit.Sector.Name,
				FromText = GetDistressCallFromText(attackedUnit),
				MessageText = messageText,
				AllowDelete = true
			};
			playerActiveMessage.SetSenderUnitAndPosition(attackedUnit);
			engine.LocalPlayer.AddMessage(playerActiveMessage);
		}

		private string GetDistressCallFromText(Unit attackedUnit)
		{
			string text = ((!faction.IsFreelancer) ? (" (" + faction.GetLongNameElseShort() + ")") : string.Empty);
			return attackedUnit.Components.PilotPerson.GetNameAndRank() + " in " + attackedUnit.GetFriendlyName() + text;
		}

		[ContextMenu("CalculateRpUsageAndProvision")]
		public void CalculateRpUsageAndProvision()
		{
			CalculateAvailableRp();
		}

		public int CalculateAvailableRp(float percentage = 1f)
		{
			int totalRpProvision = GetTotalRpProvision();
			int totalRpUsage = GetTotalRpUsage();
			int result = Mathf.RoundToInt((float)(totalRpProvision - totalRpUsage) * percentage);
			lastRpProvision = totalRpProvision;
			lastRpUsage = totalRpUsage;
			return result;
		}

		private void OnDestroy()
		{
			if (faction != null)
			{
				faction.FactionAI = null;
			}
		}

		public virtual bool RequestDock(Unit dockTarget, Faction requestorFaction)
		{
			if (requestorFaction != faction)
			{
				if (dockTarget != null && dockTarget.UnitType == UnitType.Ship)
				{
					if (faction.FactionType == FactionType.EquipmentDealer)
					{
						return faction.RequestDockByAttitude(requestorFaction);
					}
					if (!faction.IsHostileToOrAlwaysHostileTo(requestorFaction))
					{
						if (!(faction.GetOpinion(requestorFaction) > 0.8f))
						{
							return faction.IsAlliedTo(requestorFaction);
						}
						return true;
					}
					return false;
				}
				return faction.RequestDockByAttitude(requestorFaction);
			}
			return true;
		}

		public Fleet GetFleetPrefab()
		{
			return faction.Engine.GenericFleetPrefab;
		}

		public Fleet CreateFleetAndNpcPilotsForUnits(Fleet fleetPrefab, Person pilotPrefab, IEnumerable<Unit> units)
		{
			Unit unit = units.FirstOrDefault();
			if (unit != null)
			{
				Fleet fleet = SpawnFleet(faction, fleetPrefab, unit.Sector, unit.SectorPosition);
				SetFleetSettings(fleet);
				foreach (Unit unit2 in units)
				{
					FindOrCreateNpcPilotForUnit(pilotPrefab, unit2).Fleet = fleet;
				}
				fleet.FleetStrategy = FactionAIStrategyModule.GetBestStrategyForFleet(fleet, this);
				return fleet;
			}
			return null;
		}

		public Fleet CreateFleetAndNpcForUnit(Unit unit)
		{
			Fleet fleetPrefab = GetFleetPrefab();
			Person pilotPrefab = GetPilotPrefab();
			return CreateFleetAndNpcForUnit(fleetPrefab, pilotPrefab, unit);
		}

		public Fleet CreateFleetAndNpcForUnit(Fleet fleetPrefab, Person pilotPrefab, Unit unit)
		{
			Fleet fleet = SpawnFleet(faction, fleetPrefab, unit.Sector, unit.SectorPosition);
			SetFleetSettings(fleet);
			FindOrCreateNpcPilotForUnit(pilotPrefab, unit).Fleet = fleet;
			return fleet;
		}

		public Fleet CreateFleetAndNpcForUnitAndAssignStrategy(Fleet fleetPrefab, Person pilotPrefab, Unit unit)
		{
			Fleet fleet = CreateFleetAndNpcForUnit(fleetPrefab, pilotPrefab, unit);
			fleet.FleetStrategy = FactionAIStrategyModule.GetBestStrategyForFleet(fleet, this);
			return fleet;
		}

		protected virtual void OnNewFleetSpawned(Fleet fleet)
		{
		}

		private Fleet GetFleetWithFewestUnits()
		{
			Fleet result = null;
			int num = int.MaxValue;
			foreach (Fleet fleet in faction.Fleets)
			{
				int count = fleet.Ships.Count;
				if (count < num)
				{
					num = count;
					result = fleet;
				}
			}
			return result;
		}

		public Person GetPilotPrefab()
		{
			return faction.Engine.GenericPilotPrefab;
		}

		public NpcPilot FindNpcPilotForUnit(Person pilotPrefab, Unit unit)
		{
			Person pilotPerson = unit.Components.PilotPerson;
			NpcPilot npcPilot = null;
			if (pilotPerson == null)
			{
				Person person = FindPersonNotAttachedToFleet();
				if (person != null)
				{
					npcPilot = person.NpcPilot;
					if (npcPilot == null)
					{
						npcPilot = person.gameObject.AddComponent<NpcPilot>();
						npcPilot.Init();
					}
				}
			}
			else
			{
				npcPilot = pilotPerson.NpcPilot;
				if (npcPilot == null)
				{
					npcPilot = pilotPerson.gameObject.AddComponent<NpcPilot>();
					npcPilot.Init();
				}
			}
			if (npcPilot != null)
			{
				npcPilot.CurrentUnitComponents = unit.Components;
			}
			return npcPilot;
		}

		public NpcPilot FindOrCreateNpcPilotForUnit(Person pilotPrefab, Unit unit)
		{
			Person pilotPerson = unit.Components.PilotPerson;
			NpcPilot npcPilot = null;
			if (pilotPerson == null)
			{
				Person person = FindPersonNotAttachedToFleet();
				if (person != null)
				{
					npcPilot = person.NpcPilot;
					if (npcPilot == null)
					{
						npcPilot = person.gameObject.AddComponent<NpcPilot>();
						npcPilot.Init();
					}
				}
			}
			else
			{
				npcPilot = pilotPerson.NpcPilot;
				if (npcPilot == null)
				{
					npcPilot = pilotPerson.gameObject.AddComponent<NpcPilot>();
					npcPilot.Init();
				}
			}
			if (npcPilot == null)
			{
				npcPilot = SpawnAndInitialiseNpcPilotWithRandomName(pilotPrefab, unit);
			}
			npcPilot.CurrentUnitComponents = unit.Components;
			return npcPilot;
		}

		public NpcPilot SpawnAndInitialiseNpcPilotWithRandomName(Person pilotPrefab, Unit unit = null)
		{
			NpcPilot npcPilot = SpawnAndInitialiseNpcPilot(pilotPrefab, unit);
			npcPilot.Person.AutoAssignName();
			return npcPilot;
		}

		public Person SpawnAndInitialisePerson(Person pilotPrefab)
		{
			Person person = WorldHelper.SpawnPerson(pilotPrefab);
			person.Faction = faction;
			person.AutoAssignAvatarProfileFromFactionIfNone();
			person.AssignFirstPilotRankIfNull();
			if (AISettings != null && AISettings.PilotGender != GenderChoice.Unspecified)
			{
				person.IsMale = AISettings.PilotGender == GenderChoice.Male;
			}
			else
			{
				person.IsMale = UnityEngine.Random.value <= 0.5f;
			}
			person.RandomizePersonality();
			person.AutoAssignDialogProfileIfNone();
			return person;
		}

		public NpcPilot SpawnAndInitialiseNpcPilot(Person pilotPrefab, Unit unit = null)
		{
			NpcPilot npcPilot = WorldHelper.SpawnNpcFromForPerson(SpawnAndInitialisePerson(pilotPrefab), (unit != null) ? unit.Components : null);
			npcPilot.SetCombatEfficiencyFromFactionRange(faction);
			npcPilot.Settings.RestrictedWeaponPreference = Maths.RandomFloatWithPower(0.1f, 0.5f, 1f);
			npcPilot.Person.Faction = faction;
			float power = Mathf.Lerp(1.25f, 0.25f, faction.Aggression);
			npcPilot.Person.Aggression = Maths.RandomFloatWithPower(0.1f, 1f, power);
			float power2 = Mathf.Lerp(4f, 0.3f, faction.Virtue);
			float min = Mathf.Lerp(0.02f, 0.4f, faction.Virtue);
			npcPilot.Person.Properness = Maths.RandomFloatWithPower(min, 1f, power2);
			float power3 = Mathf.Lerp(1.25f, 0.25f, faction.Greed);
			npcPilot.Person.Greed = Maths.RandomFloatWithPower(0.1f, 1f, power3);
			return npcPilot;
		}

		private Person FindPersonNotAttachedToFleet()
		{
			foreach (Person person in faction.People)
			{
				if (person != null && !person.IsPilot && (AISettings.PreferSingleShip || person != faction.LeaderPerson))
				{
					return person;
				}
			}
			return null;
		}

		public int CountPeopleNotAttachedToFleet()
		{
			int num = 0;
			foreach (Person person in faction.People)
			{
				if (person != null && !person.IsPilot && (AISettings.PreferSingleShip || person != faction.LeaderPerson))
				{
					num++;
				}
			}
			return num;
		}

		public void PopulatePilottedUnitsWithoutAFleet()
		{
			IdleUnitCache.Clear();
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item != null && CanUseShip(item))
				{
					UnitComponentHolder components = item.Components;
					if (components != null && components.PilotPerson != null && components.PilotPerson.GetFleet() == null)
					{
						IdleUnitCache.Enqueue(components.Unit, components.Unit.CombatRating);
					}
				}
			}
		}

		public bool CanUseShip(Unit unit)
		{
			if (!AISettings.ExcludedUnitIds.Contains(unit.UniqueId) && unit.UnitClass.ShipPurposes.Count > 0)
			{
				return unit.IsPilottable();
			}
			return false;
		}

		public Fleet GetFirstIdleUsableGroup()
		{
			foreach (Fleet fleet in faction.Fleets)
			{
				if (fleet.IdleAndNoObjectives && CanOrderFleet(fleet))
				{
					return fleet;
				}
			}
			return null;
		}

		public static bool CanOrderFleet(Fleet fleet)
		{
			if (CanUseFleet(fleet) && fleet.IsMobile && HasFleetOrderCooldownTimeElapsed(fleet) && fleet.Leader != null && !fleet.IsAboutToEnterGate)
			{
				return fleet.OrderQueue.Count < 8;
			}
			return false;
		}

		public static bool HasFleetOrderCooldownTimeElapsed(Fleet fleet)
		{
			return Time.time - fleet.lastAiIssuedOrderTime > EngineASX.Instance.GameSettings.FactionSettings.AIOrderCooldownTime;
		}

		private void RepairFleets()
		{
			if (faction.Fleets.Count != 0 && Time.time > nextCheckForRepairsTime)
			{
				nextCheckForRepairsTime = Time.time + UnityEngine.Random.Range(0.7f, 1.4f);
				repairFleetIndex++;
				if (repairFleetIndex >= faction.Fleets.Count)
				{
					repairFleetIndex = 0;
				}
				Fleet fleet = faction.Fleets[repairFleetIndex];
				if (!(fleet == null) && fleet.IsValid && (fleet.GetCachedHealth() < fleet.GetCachedMaxHealth() * 0.8f || fleet.GetCachedShieldHealth() < fleet.GetCachedMaxShieldHealth() * 0.8f))
				{
					TryRepairFleet(fleet);
				}
			}
		}

		private void RearmFleets()
		{
			if (EngineASX.Instance.ScenarioElapsedTime > rearmCooldownTime)
			{
				fleetRearmer.Update(Time.deltaTime);
			}
		}

		public bool TryRepairFleet(Fleet fleet)
		{
			if (CanOrderFleet(fleet) && !IsGroupActiveMercenaryGroup(fleet) && CanRepairFleet(fleet) && FactionAIRepairer.EvaluateFleetNeedsRepair(Faction, fleet))
			{
				FactionAIRepairer.RepairFleet(fleet, this);
				return true;
			}
			return false;
		}

		internal void ProcessRequestForHelp(FactionAIBase factionAIRequestor, Faction attackingFaction, Sector attackSector)
		{
			float num = faction.GetEffectiveOpinionOrNull(factionAIRequestor.faction).GetValueOrDefault();
			FactionAttitude attitude = faction.GetAttitude(attackingFaction);
			if (attitude != null)
			{
				Neutrality neutrality = attitude.Neutrality;
				if (neutrality == Neutrality.Hostile || neutrality == Neutrality.Allied)
				{
					return;
				}
				num -= attitude.Opinion;
				if (attackingFaction.FactionType == FactionType.BountyHunter)
				{
					num -= 0.25f;
				}
				else
				{
					float opinion = attitude.Opinion;
					opinion -= 0.04f;
					opinion -= UnityEngine.Random.value * 0.025f;
					attitude.SetOpinionAndRecordTimeOfChange(opinion, faction);
				}
			}
			float num2 = 0f - faction.Aggression * 0.1f;
			if (!(num > num2))
			{
				return;
			}
			float damage = (num - num2) * 20f;
			if (faction.GetRecentDamageFrom(attackingFaction) <= 0f)
			{
				faction.HandleDamageFromSource(0.1f, attackingFaction, null, DamageDirectType.Indirect);
				if (attackingFaction.IsPlayerFaction)
				{
					EngineASX.Instance.UniverseEventsNotifierController.OnFactionPissedAtPlayerForAttackingCivilians(faction, factionAIRequestor.faction, attackSector);
				}
			}
			else
			{
				faction.HandleDamageFromSource(damage, attackingFaction, null, DamageDirectType.Indirect);
			}
		}

		public bool IsGroupActiveMercenaryGroup(Fleet fleet)
		{
			return mercenaryHireInfo != null;
		}

		public virtual bool CanRepairFleet(Fleet fleet)
		{
			if (!fleet.IsMobile)
			{
				return false;
			}
			return true;
		}

		public virtual bool CanRearmFleet(Fleet fleet)
		{
			if (!fleet.IsMobile)
			{
				return false;
			}
			return true;
		}

		public static bool CanUseFleet(Fleet fleet)
		{
			if (!fleet.ExcludeFromFactionAI)
			{
				return fleet.IsValid;
			}
			return false;
		}

		private void OnFailedToAssignStrategyToFleet(Fleet fleet)
		{
			EngineASX.Instance.DebugInfo.NumFactionAIFleetsDisbanded++;
			fleet.SafeDestroy();
		}

		public void AssignOrdersToIdleFleet(Fleet fleet)
		{
			if (fleet.FleetStrategy == FactionStrategy.Unspecified)
			{
				fleet.FleetStrategy = FactionAIStrategyModule.GetBestStrategyForFleet(fleet, this);
				if (fleet.FleetStrategy == FactionStrategy.Unspecified && fleet.Ships.Count > 1)
				{
					OnFailedToAssignStrategyToFleet(fleet);
					return;
				}
			}
			UpdateFleetHomeBase(fleet);
			Unit currentUnit = fleet.Leader.CurrentUnit;
			if (TryRepairFleet(fleet))
			{
				return;
			}
			Unit dockUnit = currentUnit.GetDockUnit();
			if (dockUnit != null && dockUnit.UnitClass.StationPurpose != StationPurpose.None && fleet.AllControllersDockCooldownElapsed())
			{
				fleet.UndockAll();
			}
			if (fleet.FleetStrategy != FactionStrategy.Unspecified)
			{
				if (!fleet.RecentlyTimedOutObjective)
				{
					AssignOrdersToIdleFleetInternal(fleet, currentUnit);
				}
				else
				{
					AssignOrdersToTimedOutFleetInternal(fleet, currentUnit);
				}
			}
		}

		private void ValidateNewFleetStrategy(Fleet fleet)
		{
			if (fleet.FleetStrategy == FactionStrategy.Unspecified)
			{
				string text = string.Join(", ", fleet.Ships.Select((UnitComponentHolder e) => e.Unit.GetClassAndSeriesName()));
				string text2 = string.Join(", ", strategy.ActionableStrategies.Keys);
				Debug.LogWarning($"Faction AI {faction} could not assign a strategy to fleet {fleet}. The fleet may be disbanded. Actionable strategies: {text2} Fleet units: {text}", fleet);
				return;
			}
			if (fleet.Ships.Count == 0)
			{
				Debug.LogWarning($"Assigning strategy to fleet {fleet} with no ships", this);
			}
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if ((EngineASX.Instance.GetUnitClassShipStrategyFlags(ship.UnitClass) & fleet.FleetStrategy) == 0)
				{
					Debug.LogWarning($"Fleet {fleet} was assigned a strategy {fleet.FleetStrategy} which ship {ship} cannot perform", fleet);
				}
			}
		}

		private void UpdateFleetHomeBase(Fleet fleet)
		{
			if (!fleet.IsHomeBaseValid)
			{
				fleet.SetHomeBase(FindBestHomeBaseForFleet(fleet));
			}
		}

		private void AssignOrdersToIdleFleetInternal(Fleet fleet, Unit groupLeaderUnit)
		{
			AssignOrdersToIdleFleet(fleet, groupLeaderUnit);
			fleet.AssignNextOrderIfNoCurrentOrder();
			SetFleetStance(fleet);
		}

		protected virtual void AssignOrdersToIdleFleet(Fleet fleet, Unit groupLeaderUnit)
		{
			if (fleet.GetCachedTradableCargoLoad() / fleet.GetCachedTotalCargoCapacity() > 0.1f)
			{
				if (TryOrderSellCargo(fleet, sellEquipment: false, 1f))
				{
					return;
				}
			}
			else if (fleet.GetCachedIncompatibleEquipmentValue() > 0f && fleet.FleetStrategy != FactionStrategy.Scavenge)
			{
				fleet.DumpAllIncompatibleAmmo();
			}
			switch (fleet.FleetStrategy)
			{
			case FactionStrategy.BountyHunt:
			{
				AutonomousBountyHunterOrder autonomousBountyHunterOrder = UnityObjectHelper.NewGameObject<AutonomousBountyHunterOrder>();
				autonomousBountyHunterOrder.MaxJumpDistance = 3;
				autonomousBountyHunterOrder.MaxDuration = autonomousBountyHunterOrder.AIDefaultMaxDuration;
				fleet.EnqueueOrder(autonomousBountyHunterOrder);
				OnFleetOrdered(fleet);
				break;
			}
			case FactionStrategy.Trade:
				if (fleet.Faction.Credits > 2000)
				{
					AutonomousTradeOrder autonomousTradeOrder = UnityObjectHelper.NewGameObject<AutonomousTradeOrder>();
					autonomousTradeOrder.MaxJumpDistance = 4;
					autonomousTradeOrder.MaxDuration = 0f;
					fleet.EnqueueOrder(autonomousTradeOrder);
					OnFleetOrdered(fleet);
				}
				else
				{
					AssignScavengerOrders(fleet);
				}
				break;
			case FactionStrategy.PassengerTransport:
			{
				AutonomousTransportPassengersOrder autonomousTransportPassengersOrder = UnityObjectHelper.NewGameObject<AutonomousTransportPassengersOrder>();
				fleet.Settings.AllowCombatInterception = true;
				autonomousTransportPassengersOrder.MaxJumpDistance = 3;
				autonomousTransportPassengersOrder.MaxDuration = 0f;
				fleet.EnqueueOrder(autonomousTransportPassengersOrder);
				OnFleetOrdered(fleet);
				break;
			}
			case FactionStrategy.Explore:
				AssignExploreOrder(fleet);
				break;
			case FactionStrategy.Mine:
			{
				if (faction.Intel.GetCountOfDiscoveredUnitsByType(UnitType.Asteroid) == 0)
				{
					AssignExploreOrder(fleet);
					break;
				}
				MineOrder mineOrder = UnityObjectHelper.NewGameObject<MineOrder>();
				mineOrder.MaxJumpDistance = 2;
				mineOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
				mineOrder.CollectOwnerMode = GetCollectCargoOwnerMode();
				mineOrder.MaxDuration = mineOrder.AIDefaultMaxDuration;
				fleet.EnqueueOrder(mineOrder);
				SellCargoOrder sellCargoOrder = UnityObjectHelper.NewGameObject<SellCargoOrder>();
				sellCargoOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
				sellCargoOrder.SellEquipment = false;
				sellCargoOrder.MaxJumpDistance = 4;
				sellCargoOrder.MaxDuration = sellCargoOrder.AIDefaultMaxDuration;
				sellCargoOrder.CompleteWhenNoCargoToSell = true;
				sellCargoOrder.CompleteWhenNoBuyerFound = false;
				sellCargoOrder.MinBuyPriceMultiplier = 0.05f;
				fleet.EnqueueOrder(sellCargoOrder);
				OnFleetOrdered(fleet);
				break;
			}
			case FactionStrategy.DealEquipment:
			{
				PatrolOrder patrolOrder = PatrolRouteCreator.CreatePatrolObjective(fleet.Faction, fleet.Sector, 2, 2, 1, 1, 4, considerStationsAsNodes: true, CanEquipmentDealerParkAtStation);
				patrolOrder.Priority = 0.2f;
				AssignDefaultOrderMaxDuration(patrolOrder);
				fleet.EnqueueOrder(patrolOrder);
				WaitOrder waitOrder = UnityObjectHelper.NewGameObject<WaitOrder>();
				waitOrder.WaitTime = GameController.Instance.GameSettings.EquipmentDealerNodeWaitTime;
				waitOrder.MaxDuration = waitOrder.WaitTime * 2f;
				waitOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
				waitOrder.Priority = 0.2f;
				fleet.EnqueueOrder(waitOrder);
				OnFleetOrdered(fleet);
				break;
			}
			case FactionStrategy.Scavenge:
				AssignOrdersToScavengeFleet(fleet);
				break;
			case FactionStrategy.War:
				AssignOrdersToWarFleet(fleet);
				break;
			case FactionStrategy.Escort:
			{
				Fleet fleetToEscort = GetFleetToEscort(fleet);
				if (fleetToEscort != null)
				{
					OrderFleetToProtectFleet(fleet, fleetToEscort);
					EngineASX.Instance.DebugInfo.NumTimesFleetsOrderedToEscortFleet++;
				}
				break;
			}
			case FactionStrategy.Scout:
				SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(fleet.Sector, 4, faction);
				if (SectorFinder.Results.Count == 0)
				{
					break;
				}
				if (attackModule != null && attackModule.LastSearchSectorsWithHostiles.Count > 0)
				{
					foreach (int sectorWithHostiles in attackModule.LastSearchSectorsWithHostiles)
					{
						if (SectorFinder.Results.Any((SectorFinder.SectorResult e) => e.Sector.UniqueId == sectorWithHostiles))
						{
							OrderMoveToRandomSectorPosition(fleet, EngineASX.Instance.GetSectorById(sectorWithHostiles)).Priority = 0.3f;
							break;
						}
					}
					break;
				}
				OrderMoveToRandomSectorPosition(fleet, SectorFinder.Results.GetRandom().Sector).Priority = 0.3f;
				break;
			default:
				Debug.LogWarning($"Don't know how to assign fleet with strategy: {fleet.FleetStrategy}");
				break;
			}
		}

		private void AssignExploreOrder(Fleet fleet)
		{
			ExploreOrder exploreOrder = UnityObjectHelper.NewGameObject<ExploreOrder>();
			exploreOrder.MaxJumpDistance = 3;
			exploreOrder.MaxDuration = 600f;
			fleet.EnqueueOrder(exploreOrder);
			OnFleetOrdered(fleet);
		}

		public virtual bool RequestCapture(Unit unit)
		{
			return true;
		}

		private MoveToOrder OrderMoveToRandomSectorPosition(Fleet fleet, Sector randomSectorWithHostiles)
		{
			MoveToOrder moveToOrder = UnityObjectHelper.NewGameObject<MoveToOrder>();
			moveToOrder.Target = new SectorTarget
			{
				Sector = randomSectorWithHostiles,
				SectorPosition = randomSectorWithHostiles.GetRandomSafeDeploymentSectorPosition(1.2f, 100f, GameController.Instance.StaticNonOverlappingMask)
			};
			moveToOrder.MaxDuration = 1000f;
			fleet.EnqueueOrder(moveToOrder);
			OnFleetOrdered(fleet);
			return moveToOrder;
		}

		private Fleet GetFleetToEscort(Fleet fleet)
		{
			protectedFleetCached.Clear();
			foreach (Fleet fleet2 in faction.Fleets)
			{
				if (fleet2.IsValid && fleet2 != fleet && fleet2.FleetStrategy == FactionStrategy.Escort && fleet2.IsEscortingFleet(out var escortedFleet))
				{
					protectedFleetCached.Add(escortedFleet.UniqueId);
				}
			}
			foreach (Fleet fleet3 in faction.Fleets)
			{
				if (fleet3.IsValid && fleet != fleet3 && FleetCanHaveEscort(fleet3) && !protectedFleetCached.Contains(fleet3.UniqueId))
				{
					return fleet3;
				}
			}
			return null;
		}

		protected virtual void AssignOrdersToScavengeFleet(Fleet fleet)
		{
			if (!TryOrderSellCargo(fleet, sellEquipment: true, Mathf.Lerp(0.6f, 0.2f, faction.Greed)) && !TryOrderScavengerFleetToRespondToDistressCall(fleet) && !TryOrderScavengerToRecentlyDestroyedShip(fleet))
			{
				AssignScavengerOrders(fleet);
			}
		}

		private bool TryOrderScavengerToRecentlyDestroyedShip(Fleet fleet)
		{
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(fleet.Sector, 2, faction);
			foreach (SectorFinder.SectorResult item in SectorFinder.Results.OrderBy((SectorFinder.SectorResult e) => fleet.Sector.GetJumpDistanceTo(e.Sector)))
			{
				List<DestroyedUnitInfo> destroyedInSector = EngineASX.Instance.DestroyedUnitInfoController.GetDestroyedInSector(item.Sector);
				if (destroyedInSector != null && destroyedInSector.Count > 0)
				{
					DestroyedUnitInfo destroyedUnitInfo = destroyedInSector.OrderByDescending((DestroyedUnitInfo e) => e.TimeOfDestruction + (double)(UnityEngine.Random.value * 240f)).First();
					OrderFleetToScavengerInSector(fleet, destroyedUnitInfo.Sector, destroyedUnitInfo.SectorPosition);
					return true;
				}
			}
			return false;
		}

		private bool TryOrderScavengerFleetToRespondToDistressCall(Fleet fleet)
		{
			int? respondableDistressCallForScavenge = GetRespondableDistressCallForScavenge(fleet);
			if (respondableDistressCallForScavenge.HasValue)
			{
				DistressCall distressCall = recentDistressCalls[respondableDistressCallForScavenge.Value];
				OrderFleetToScavengerInSector(fleet, distressCall.Sector, distressCall.SectorPosition);
				recentDistressCalls.RemoveAt(respondableDistressCallForScavenge.Value);
				TrimInvalidDistressCalls();
				return true;
			}
			return false;
		}

		private void OrderFleetToScavengerInSector(Fleet fleet, Sector sector, Vector3? customSectorPosition)
		{
			ScavengeOrder scavengeOrder = UnityObjectHelper.NewGameObject<ScavengeOrder>();
			scavengeOrder.TargetSector = sector;
			scavengeOrder.MaxDuration = 600f;
			scavengeOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			scavengeOrder.CollectOwnerMode = GetCollectCargoOwnerMode();
			scavengeOrder.Priority = 0.2f;
			fleet.ClearOrders();
			fleet.EnqueueOrder(scavengeOrder);
			fleet.AssignNextOrder();
			if (customSectorPosition.HasValue)
			{
				((ActiveScavengeOrder)fleet.ActiveOrder).RoamSectorLocalPosition = customSectorPosition.Value;
			}
			OnFleetOrdered(fleet);
		}

		protected virtual void AssignOrdersToWarFleet(Fleet fleet)
		{
			if (TryOrderNavalGroupToRespondToDistressCall(fleet))
			{
				EngineASX.Instance.DebugInfo.NumTimesEmpireRespondedToDistressCall++;
			}
			else
			{
				if (UnityEngine.Random.value > AISettings.OffensiveStance && TryOrderFleetToDefend(fleet))
				{
					return;
				}
				if (UnityEngine.Random.value < AISettings.OffensiveStance * GameController.Instance.GameSettings.GameplaySettings.ProbabilityOfEmpireRandomAttack && TryOrderFleetToAttack(fleet))
				{
					switch (faction.FactionType)
					{
					case FactionType.Empire:
						EngineASX.Instance.DebugInfo.NumTimesEmpireLaunchedAttacks++;
						break;
					case FactionType.Bandit:
						EngineASX.Instance.DebugInfo.NumTimesBanditsLaunchedAttacks++;
						break;
					default:
						EngineASX.Instance.DebugInfo.NumTimesOtherFactionLaunchedAttacks++;
						break;
					}
				}
				else
				{
					OrderPatrol(fleet);
				}
			}
		}

		protected virtual void AssignDefaultOrderMaxDuration(FleetOrder fleetOrder)
		{
			if (fleetOrder.MaxDuration == 0f)
			{
				fleetOrder.MaxDuration = fleetOrder.AIDefaultMaxDuration * UnityEngine.Random.Range(0.75f, 1.25f);
			}
		}

		protected virtual void OrderPatrol(Fleet fleet)
		{
			FactionAIPatrolSettings patrolSettingsOrDefault = fleet.Faction.FactionAI.GetPatrolSettingsOrDefault();
			PatrolOrder patrolOrder = null;
			patrolOrder = ((faction.ControlledSectorCount != 0 && !(UnityEngine.Random.value < 0.2f)) ? BorderPatrolRouteCreator.TryCreate(fleet.Faction, fleet.GetHomeSectorOrCurrent(), patrolSettingsOrDefault.MinPatrolScenes, patrolSettingsOrDefault.MaxPatrolScenes, patrolSettingsOrDefault.MinPatrolNodesInSector, patrolSettingsOrDefault.MaxPatrolNodesInSector, (fleet.Settings.MaxJumpDistance > -1) ? fleet.Settings.MaxJumpDistance : 3, considerStationsAsNodes: true) : PatrolRouteCreator.CreatePatrolObjective(fleet.Faction, fleet.GetHomeSectorOrCurrent(), patrolSettingsOrDefault.MinPatrolScenes, patrolSettingsOrDefault.MaxPatrolScenes, patrolSettingsOrDefault.MinPatrolNodesInSector, patrolSettingsOrDefault.MaxPatrolNodesInSector, (fleet.Settings.MaxJumpDistance > -1) ? fleet.Settings.MaxJumpDistance : 3, considerStationsAsNodes: true));
			if (patrolOrder != null)
			{
				patrolOrder.Priority = 0.2f;
				AssignDefaultOrderMaxDuration(patrolOrder);
				if (patrolOrder.NodeCount >= 2)
				{
					fleet.EnqueueOrder(patrolOrder);
					OnFleetOrdered(fleet);
				}
			}
		}

		protected bool TryOrderFleetToAttack(Fleet fleet)
		{
			if (attackModule.BestAttackTarget != null && attackModule.IsUnitValidAttackTarget(attackModule.BestAttackTarget))
			{
				AttackTargetOrder attackTargetOrder = UnityObjectHelper.NewGameObject<AttackTargetOrder>();
				attackTargetOrder.TargetUnit = attackModule.BestAttackTarget;
				attackTargetOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
				attackTargetOrder.AttackPriority = 10f;
				attackTargetOrder.MaxDuration = 600f;
				fleet.EnqueueOrder(attackTargetOrder);
				OnFleetOrdered(fleet);
				attackModule.ClearBestAttackTarget();
				return true;
			}
			return false;
		}

		private static bool CanEquipmentDealerParkAtStation(Unit unit)
		{
			return unit.Faction.FactionType != FactionType.Bandit;
		}

		private void AssignOrdersToTimedOutFleetInternal(Fleet fleet, Unit groupLeaderUnit)
		{
			AssignOrdersToTimedOutFleet(fleet, groupLeaderUnit);
			fleet.AssignNextOrderIfNoCurrentOrder();
			SetFleetStance(fleet);
		}

		protected virtual void AssignOrdersToTimedOutFleet(Fleet fleet, Unit groupLeaderUnit)
		{
			AssignOrdersToIdleFleetInternal(fleet, groupLeaderUnit);
		}

		protected void OrderExplore(Fleet fleet)
		{
			ExploreOrder exploreOrder = UnityObjectHelper.NewGameObject<ExploreOrder>();
			exploreOrder.MaxJumpDistance = 3;
			exploreOrder.MaxDuration = UnityEngine.Random.Range(300f, 600f);
			fleet.EnqueueOrder(exploreOrder);
			OnFleetOrdered(fleet);
		}

		public SectorTarget FindBestHomeBaseForFleet(Fleet fleet)
		{
			float num = 0f;
			Unit unit = null;
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType != null && unitsByType.Count > 0)
			{
				foreach (Unit item in unitsByType)
				{
					if (item != null && item.IsDockable)
					{
						float stationScoreAsFleetHomeBase = GetStationScoreAsFleetHomeBase(item, fleet);
						if (unit == null || stationScoreAsFleetHomeBase > num)
						{
							num = stationScoreAsFleetHomeBase;
							unit = item;
						}
					}
				}
			}
			if (unit != null)
			{
				return SectorTarget.FromUnit(unit);
			}
			if (faction.HomeSector != null)
			{
				unit = GetBestHomeBaseFromFriendlyStations(faction.HomeSector);
				if (unit != null)
				{
					return SectorTarget.FromUnit(unit);
				}
				if (!faction.HomeSectorPosition.HasValue)
				{
					AutoSetHomeSectorPosition();
				}
				if (faction.HomeSectorPosition.HasValue)
				{
					return SectorTarget.FromSectorPosition(faction.HomeSector, faction.HomeSectorPosition.Value + Geometry.RandomXZUnitVector() * UnityEngine.Random.Range(300f, 700f));
				}
			}
			return null;
		}

		public virtual float GetStationScoreAsFleetHomeBase(Unit unit, Fleet fleet, float maxRandomness = 1f)
		{
			float num = 0f;
			if (unit.UnitClass.HasRepairFacilities)
			{
				num += 7f;
			}
			if (unit.Faction != faction)
			{
				float opinion = faction.GetOpinion(unit.Faction);
				num += opinion * 20f;
			}
			if (fleet != null)
			{
				foreach (Fleet fleet2 in faction.Fleets)
				{
					if (fleet2 != fleet)
					{
						Sector homeSectorOrFactionHomeSector = fleet2.HomeSectorOrFactionHomeSector;
						if (homeSectorOrFactionHomeSector != null)
						{
							num += (float)homeSectorOrFactionHomeSector.GetJumpDistanceTo(unit.Sector);
						}
					}
				}
			}
			return num + UnityEngine.Random.value * maxRandomness;
		}

		public void HandleCargoStolen(Faction thievingFaction, Unit theivingUnit, int cargoValue)
		{
			if (thievingFaction != null && ShouldApplyCargoStolenDamage(thievingFaction))
			{
				FactionRecentDamageSettings factionRecentDamageSettings = GameController.Instance.GameSettings.FactionSettings.FactionRecentDamageSettings;
				float num = (float)cargoValue * factionRecentDamageSettings.StolenCargoDamageConversion;
				num += factionRecentDamageSettings.StolenCargoBaseDamage;
				faction.HandleDamageFromSource(num, thievingFaction, theivingUnit, factionRecentDamageSettings.StolenCargoDamageType);
			}
			if (faction.AISettings.PreferenceToPlaceBounty >= 0f && thievingFaction != null && ShouldPlaceBounty() && ShouldPlaceBountyOnUnit(thievingFaction, theivingUnit))
			{
				Person pilot = theivingUnit.GetPilot();
				if (pilot != null && UnityEngine.Random.value < faction.AISettings.PreferenceToPlaceBounty)
				{
					TryPlaceBountyWithRounding((float)cargoValue * 0.5f, pilot);
				}
			}
		}

		protected virtual bool ShouldApplyCargoStolenDamage(Faction thievingFaction)
		{
			return true;
		}

		private Person GetActualControllerOfUnit(Unit unit)
		{
			Person person = unit.Components.PilotPerson;
			if (person.Faction.IsPlayerFaction)
			{
				person = faction.Engine.LocalPlayer.Person;
			}
			return person;
		}

		public virtual void OnNewStationConstructionStarted(Unit newStation)
		{
			SetLastBuiltUnitTimeToCurrent();
		}

		public bool RequestTruce(Faction requestor, out int creditsCost)
		{
			creditsCost = 0;
			if (IsAlwaysAtWarWithFaction(requestor))
			{
				return false;
			}
			if (faction.FactionType != FactionType.BountyHunter || !HasBountyObjectivesTargettingFaction(requestor))
			{
				creditsCost = TruceHelper.CalculateTruceCost(requestor, this);
				return true;
			}
			return false;
		}

		private bool HasBountyObjectivesTargettingFaction(Faction faction)
		{
			foreach (Fleet fleet in this.faction.Fleets)
			{
				if (fleet != null && fleet.ActiveOrder != null)
				{
					ActiveBountyHunterOrder activeBountyHunterOrder = fleet.ActiveOrder as ActiveBountyHunterOrder;
					if (activeBountyHunterOrder != null && activeBountyHunterOrder.TargetPilot != null && activeBountyHunterOrder.TargetPilot.Faction == faction)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool ShouldPlaceBounty()
		{
			if (faction.People.Count > 0)
			{
				return faction.EstimateNonMinorShipAndStationCount() > 0;
			}
			return false;
		}

		private bool ShouldPlaceBountyOnUnit(Faction faction, Unit unit)
		{
			if (this.faction.IsHostileTo(faction) && unit != null && !unit.UnitClass.IsTurret && unit.Components != null)
			{
				return unit.Components.PilotPerson != null;
			}
			return false;
		}

		internal void HandleBountyHunterTargetDestroyedByOtherFaction(Fleet bountyHunterFleet, Faction attackingFaction)
		{
			FactionAttitude orCreateAttitude = faction.GetOrCreateAttitude(attackingFaction);
			orCreateAttitude.SetOpinionAndRecordTimeOfChange(orCreateAttitude.Opinion - 0.25f);
			if (orCreateAttitude.Opinion + faction.Virtue < 0.4f)
			{
				if (!faction.IsHostileTo(attackingFaction))
				{
					faction.SetAsHostileTo(attackingFaction);
					if (attackingFaction.IsPlayerFaction && UnityEngine.Random.value > 0.5f)
					{
						PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage
						{
							FromText = faction.GetLongNameElseShort(),
							ToText = attackingFaction.GetLongNameElseShort(),
							MessageText = "You just killed my target. You've cost me time and credits. Now you will pay..."
						};
						playerActiveMessage.SetSenderUnitAndPosition(bountyHunterFleet.LeaderUnit);
						faction.Engine.LocalPlayer.AddMessageDelayed(playerActiveMessage, 2f);
					}
				}
			}
			else if (attackingFaction.IsPlayerFaction)
			{
				PlayerActiveMessage playerActiveMessage2 = new PlayerActiveMessage
				{
					FromText = faction.GetLongNameElseShort(),
					ToText = attackingFaction.GetLongNameElseShort(),
					MessageText = "I don't like someone stealing my bounty. I suggest you stay out of my way in future"
				};
				playerActiveMessage2.SetSenderUnitAndPosition(bountyHunterFleet.LeaderUnit);
				faction.Engine.LocalPlayer.AddMessageDelayed(playerActiveMessage2, 2f);
			}
		}

		private bool HostilesNearSectorPosition(Sector sector, Vector3 sectorPosition, float maxDistance = 1500f)
		{
			int num = Physics.OverlapSphereNonAlloc(sector.ToWorldPosition(sectorPosition), maxDistance, EngineASX.ColliderCache, GameController.Instance.ShipsAndStationsMask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.IsValidAndNotDestroyed && component.Sector == sector && (faction.IsHostileTo(component) || component.IsHostileTo(faction)))
				{
					return true;
				}
			}
			return false;
		}

		public void HandleOwnedUnitKilled(Unit lostUnit, Unit attacker, Faction attackerFaction)
		{
			if (lostUnit.IsStationOrShip() && attackerFaction != null && attackerFaction != faction)
			{
				recentlyLostUnits.Enqueue(new RecentlyLostUnit(lostUnit.UnitClass, attacker, attackerFaction));
			}
		}

		private void PlaceRevengeBounty(UnitClass lostUnitClass, Unit attacker, Faction attackerFaction)
		{
			if (!(attacker == null) && !(attackerFaction == null) && faction.AISettings.PreferenceToPlaceBounty >= 0f && ShouldPlaceBounty() && ShouldPlaceBountyOnUnit(attackerFaction, attacker))
			{
				int saleCost = lostUnitClass.SaleCost;
				Person pilot = attacker.GetPilot();
				if (pilot != null && UnityEngine.Random.value < faction.AISettings.PreferenceToPlaceBounty)
				{
					TryPlaceBountyWithRounding((float)saleCost * 0.1f, pilot);
				}
			}
		}

		private void TryPlaceBountyWithRounding(float bounty, Person pilot)
		{
			int bounty2 = BountyHelper.RoundBountyToNearest(bounty);
			TryPlaceBounty(bounty2, pilot);
		}

		private void TryPlaceBounty(int bounty, Person pilot)
		{
			FactionBountyBoard nearestBountyBoardToHomeSector = faction.GetNearestBountyBoardToHomeSector();
			if (!(nearestBountyBoardToHomeSector == null))
			{
				int num = Mathf.Clamp(bounty, faction.Engine.GameSettings.BountyMinCreditsPlaced, 25000);
				if (faction.Credits - faction.CreditsReserve > num && BountyHelper.CanFactionPlaceBounty(faction, num))
				{
					BountyHelper.PlaceBountyWithNotifications(faction, nearestBountyBoardToHomeSector, pilot, num);
				}
			}
		}

		public void OnGroupObjectiveTimeout(Fleet group, ActiveFleetOrder objective)
		{
			if (objective is ActiveRepairFleetOrder && FactionAIRepairer.RepairFleet(group, this, forceWaitForRepair: true))
			{
				EngineASX.Instance.DebugInfo.NumTimesFleetSendForRepairsTimeoutAndReissued++;
			}
			else
			{
				group.ClearOrders();
			}
		}

		private void SetPermanentWarWithFactionAndWorstOpinion(Faction otherFaction, bool twoWay)
		{
			faction.GetOrCreateAttitude(otherFaction).SetOpinionAndRecordTimeOfChange(-1f);
			faction.SetAsHostileTo(otherFaction, permanentWar: true);
			if (twoWay)
			{
				otherFaction.SetAsHostileTo(faction, permanentWar: true);
			}
		}

		public void OnNewGameFaction(Faction otherFaction)
		{
		}

		private bool ShouldDisbandFleet(Fleet fleet)
		{
			if (fleet.FleetStrategy != FactionStrategy.Unspecified)
			{
				foreach (UnitComponentHolder ship in fleet.Ships)
				{
					if (ship != null && (EngineASX.Instance.GetUnitClassShipStrategyFlags(ship.UnitClass) & fleet.FleetStrategy) == 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void OnWorldInit()
		{
			if (faction.Fleets.Count > 0)
			{
				List<Fleet> list = new List<Fleet>();
				foreach (Fleet fleet in faction.Fleets)
				{
					if (fleet.ExcludeFromFactionAI)
					{
						continue;
					}
					if (fleet.Ships.Count > 0)
					{
						FactionStrategy bestStrategyForFleet = FactionAIStrategyModule.GetBestStrategyForFleet(fleet, this);
						if (bestStrategyForFleet == FactionStrategy.Unspecified || ShouldDisbandFleet(fleet))
						{
							list.Add(fleet);
						}
						else if (bestStrategyForFleet != fleet.FleetStrategy && (fleet.FleetStrategy & strategy.ActionableStrategyFlags) == 0)
						{
							fleet.FleetStrategy = bestStrategyForFleet;
						}
					}
					InitFLeetOnWorldInit(fleet);
				}
				foreach (Fleet item in list)
				{
					OnFailedToAssignStrategyToFleet(item);
				}
			}
			CacheMostPowerfulFleet();
			faction.CreateAISettingsIfNull();
			ReapplyFactionSettingsFromPrefab();
			onWorldInit();
		}

		protected virtual void onWorldInit()
		{
		}

		private void InitFLeetOnWorldInit(Fleet fleet)
		{
			SetFleetSettings(fleet);
			if (fleet.ActiveOrder != null)
			{
				fleet.ActiveOrder.FleetOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			}
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				item.CompletionMode = FleetOrderCompletionMode.Destroy;
			}
		}

		private int GetTotalNumberOfAlliances()
		{
			int num = 0;
			foreach (FactionAttitude relation in faction.Relations)
			{
				if (relation != null && relation.Neutrality == Neutrality.Allied)
				{
					num++;
				}
			}
			return num;
		}

		public void BreakAllAlliances()
		{
			FactionAttitude[] array = faction.Relations.ToArray();
			foreach (FactionAttitude factionAttitude in array)
			{
				if (factionAttitude != null && factionAttitude.Neutrality == Neutrality.Allied && factionAttitude.TargetFaction != null)
				{
					BreakAllianceTwoWay(factionAttitude);
				}
			}
		}

		private void BreakAllianceTwoWay(FactionAttitude attitude)
		{
			attitude.Neutrality = Neutrality.Neutral;
			FactionAttitude attitude2 = attitude.TargetFaction.GetAttitude(faction);
			if (attitude2 != null && attitude2.Neutrality == Neutrality.Allied)
			{
				attitude2.Neutrality = Neutrality.Neutral;
			}
		}

		public virtual void SetFleetSettings(Fleet fleet)
		{
			SetFleetStance(fleet);
			SetFLeetInterceptionDistance(fleet);
			fleet.Settings.DestroyWhenNoPilots = true;
			if (faction.FactionType == FactionType.BountyHunter)
			{
				fleet.Settings.MaxJumpDistance = 10;
			}
			else
			{
				fleet.Settings.MaxJumpDistance = 5;
			}
			fleet.Settings.PreferCloak = true;
		}

		private void SetFLeetInterceptionDistance(Fleet fleet)
		{
			fleet.Settings.TargetInterceptionLowerDistance = Mathf.Lerp(MinInterceptionDistance, MaxInterceptionDistance, Mathf.Pow(fleet.Settings.Aggression, 3f));
			fleet.Settings.TargetInterceptionUpperDistance = fleet.Settings.TargetInterceptionLowerDistance + 1500f;
		}

		public bool RequestPermissionToDeclareWarOn(Faction sourceFaction, bool isBeingAttacked)
		{
			return true;
		}

		private void ReapplyFactionSettingsFromPrefab()
		{
			Faction factionPrefabById = faction.Engine.GetFactionPrefabById(faction.UniqueId);
			if (factionPrefabById != null)
			{
				if (factionPrefabById.AISettings != null)
				{
					faction.AISettings.DailyIncome = factionPrefabById.AISettings.DailyIncome;
					faction.AISettings.HostileWithAll = factionPrefabById.AISettings.HostileWithAll;
				}
				faction.TradeEfficiency = factionPrefabById.TradeEfficiency;
				faction.Aggression = factionPrefabById.Aggression;
				faction.Virtue = factionPrefabById.Virtue;
				faction.FactionType = factionPrefabById.FactionType;
			}
		}

		public void OnNewAttitudeWithFaction(Faction otherFaction, FactionAttitude attitude)
		{
			if (IsAlwaysAtWarWithFaction(otherFaction))
			{
				faction.SetAsHostileToTwoWay(otherFaction);
				faction.SetOpinionWith(otherFaction, UnityEngine.Random.Range(-1f, -0.5f));
				return;
			}
			float? initialOpinionWithDiscoveredFaction = GetInitialOpinionWithDiscoveredFaction(otherFaction);
			if (initialOpinionWithDiscoveredFaction.HasValue)
			{
				faction.SetOpinionWith(otherFaction, initialOpinionWithDiscoveredFaction.Value);
			}
		}

		public virtual float? GetInitialOpinionWithDiscoveredFaction(Faction otherFaction)
		{
			if (ShouldIgnoreNewlyDiscoveredFaction(otherFaction))
			{
				return null;
			}
			float changeMultiplier;
			return FactionOpinionNeutralizerComponent.GetBaselineOpinion(faction, otherFaction, out changeMultiplier);
		}

		private bool ShouldIgnoreNewlyDiscoveredFaction(Faction otherFaction)
		{
			if (faction.IsCivilianFromFactionType && otherFaction.Virtue < 0.4f)
			{
				return false;
			}
			if (!faction.IsMinor && otherFaction.IsMinor)
			{
				return true;
			}
			return false;
		}

		private bool AllyWithOtherFactionRandomly(FactionAttitude attitude)
		{
			if (faction.FactionType != FactionType.Mercenary && attitude.TargetFaction != null && !attitude.TargetFaction.IsPlayerFaction && !AISettings.PreferSingleShip && attitude.TargetFaction.FactionAI != null && attitude.TargetFaction.FactionAI.HomeSector != null && !attitude.TargetFaction.AISettings.PreferSingleShip && HomeSector != null && HomeSector.GetJumpDistanceTo(attitude.TargetFaction.FactionAI.HomeSector) < 3 && attitude.Neutrality == Neutrality.Neutral && attitude.Opinion > 0.75f && UnityEngine.Random.value < attitude.Opinion - 0.59f && GetTotalNumberOfAlliances() < 6)
			{
				faction.SetNeutralityWith(attitude.TargetFaction, Neutrality.Allied);
				attitude.TargetFaction.SetNeutralityWith(faction, Neutrality.Allied);
				return true;
			}
			return false;
		}

		public virtual bool IsAlwaysAtWarWithFaction(Faction otherFaction)
		{
			if (faction.AISettings.HostileWithAll)
			{
				return true;
			}
			if (mercenaryHireInfo != null && mercenaryHireInfo.HiringFaction != null && mercenaryHireInfo.HiringFaction.IsHostileTo(otherFaction))
			{
				return true;
			}
			return false;
		}

		public Unit GetFirstUsableShip()
		{
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					if (item.UnitClass.ShipType == ShipType.Normal)
					{
						return item;
					}
				}
			}
			return null;
		}

		public CollectCargoOwnerMode GetCollectCargoOwnerMode()
		{
			return GetCollectCargoOwnerMode(faction.Virtue, 0f);
		}

		public CollectCargoOwnerMode GetCollectCargoOwnerMode(float virtue, float opinion)
		{
			if (virtue < 0.98f)
			{
				if (virtue < 0.38f)
				{
					return CollectCargoOwnerMode.AnyOwnership;
				}
				return CollectCargoOwnerMode.OwnedNoFactionOrHostile;
			}
			return CollectCargoOwnerMode.OwnedOrNoFaction;
		}

		public bool TryOrderSellCargo(Fleet fleet, bool sellEquipment, float lowSpaceThreshold = 0.25f)
		{
			if (fleet.GetCachedFreeCargoSpace01() < lowSpaceThreshold)
			{
				SellCargoOrder sellCargoOrder = UnityObjectHelper.NewGameObject<SellCargoOrder>();
				sellCargoOrder.MaxJumpDistance = -1;
				sellCargoOrder.SellEquipment = sellEquipment;
				sellCargoOrder.CompleteWhenNoBuyerFound = false;
				sellCargoOrder.CompleteWhenNoCargoToSell = true;
				sellCargoOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
				sellCargoOrder.MaxDuration = 800f;
				fleet.EnqueueOrder(sellCargoOrder);
				OnFleetOrdered(fleet);
				return true;
			}
			return false;
		}

		protected void AssignScavengerOrders(Fleet fleet)
		{
			ScavengeOrder scavengeOrder = UnityObjectHelper.NewGameObject<ScavengeOrder>();
			scavengeOrder.AllowCombatInterception = true;
			scavengeOrder.MaxJumpDistance = -1;
			scavengeOrder.CollectOwnerMode = GetCollectCargoOwnerMode();
			scavengeOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			scavengeOrder.MaxDuration = UnityEngine.Random.Range(120f, 180f);
			scavengeOrder.Priority = 0.3f;
			fleet.EnqueueOrder(scavengeOrder);
			OnFleetOrdered(fleet);
		}

		private void OrderFleetToCollectCargo(Fleet fleet, Unit cargo)
		{
			CollectCargoOrder collectCargoOrder = UnityObjectHelper.NewGameObject<CollectCargoOrder>();
			collectCargoOrder.AllowCombatInterception = true;
			collectCargoOrder.MaxJumpDistance = 99;
			collectCargoOrder.TargetUnit = cargo;
			collectCargoOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			fleet.InsertOrderAndRequeueActive(collectCargoOrder);
			OnFleetOrdered(fleet);
		}

		public static FactionAIBase CreateFactionAIType(Transform parent, FactionAIType aiType)
		{
			return aiType switch
			{
				FactionAIType.Explorer => UnityObjectHelper.NewGameObject<FactionAIExplorer>(parent), 
				FactionAIType.BountyHunter => UnityObjectHelper.NewGameObject<FactionAIBountyHunter>(parent), 
				FactionAIType.EquipmentDealer => UnityObjectHelper.NewGameObject<FactionAIEquipmentDealer>(parent), 
				FactionAIType.PassengerTransport => UnityObjectHelper.NewGameObject<FactionAIPassengerTransport>(parent), 
				FactionAIType.Empire => UnityObjectHelper.NewGameObject<FactionAIEmpire>(parent), 
				FactionAIType.Bandit => UnityObjectHelper.NewGameObject<FactionAIBandit>(parent), 
				FactionAIType.Scavenger => UnityObjectHelper.NewGameObject<FactionAIScavenger>(parent), 
				FactionAIType.Mercenary => UnityObjectHelper.NewGameObject<FactionAIMercenary>(parent), 
				FactionAIType.Trader => UnityObjectHelper.NewGameObject<FactionAITrader>(parent), 
				FactionAIType.StationOwner => UnityObjectHelper.NewGameObject<FactionAIStationOwner>(parent), 
				FactionAIType.Miner => UnityObjectHelper.NewGameObject<FactionAIMiner>(parent), 
				FactionAIType.Patroller => UnityObjectHelper.NewGameObject<FactionAIPatroller>(parent), 
				FactionAIType.StationBuilder => UnityObjectHelper.NewGameObject<FactionAIStationBuilder>(parent), 
				FactionAIType.Generic => UnityObjectHelper.NewGameObject<FactionAIBase>(parent), 
				FactionAIType.Outlaw => UnityObjectHelper.NewGameObject<FactionAIOutlaw>(parent), 
				_ => null, 
			};
		}

		public void CacheBuildableShipUnitClasses()
		{
			cachedBuildableUnitClasses.Clear();
			if (faction.FactionAI.strategy == null)
			{
				Debug.LogError($"No buildable ships will be cached as the faction {faction} does not have a strategy", faction);
				return;
			}
			foreach (UnitClass factionAIBuildableShip in EngineASX.Instance.FactionAIBuildableShips)
			{
				if (IsShipUnitClassValidForBuilding(factionAIBuildableShip))
				{
					cachedBuildableUnitClasses.Add(factionAIBuildableShip.UniqueID);
				}
			}
		}

		public bool IsShipUnitClassValidForBuilding(UnitClass unitClass)
		{
			if (IsUnitClassValidPurposeForBuilding(unitClass) && IsUnitClassValidCombatRatingForBuilding(unitClass))
			{
				return IsUnitClassValidStealthPurposeForBuilding(unitClass);
			}
			return false;
		}

		public bool IsUnitClassValidStealthPurposeForBuilding(UnitClass unitClass)
		{
			if (unitClass.PurposeStealth)
			{
				return BuildStealthShips;
			}
			return true;
		}

		public bool IsUnitClassValidPurposeForBuilding(UnitClass unitClass)
		{
			if (strategy == null)
			{
				Debug.LogError("Faction requires a strategy", faction);
				return false;
			}
			return (strategy.StrategyFlags & EngineASX.Instance.GetUnitClassShipStrategyFlags(unitClass)) != 0;
		}

		public bool IsUnitClassValidCombatRatingForBuilding(UnitClass unitClass)
		{
			if (unitClass.CombatRating >= FactionTypeInfo.MinShipCombatRating)
			{
				return unitClass.CombatRating <= FactionTypeInfo.MaxShipCombatRating;
			}
			return false;
		}

		public void OnFleetOrdered(Fleet fleet)
		{
			fleet.lastAiIssuedOrderTime = Time.time;
		}

		public bool DoesFleetHaveRepairOrder(Fleet fleet)
		{
			if (fleet.ActiveOrder != null && fleet.ActiveOrder.FleetOrder is RepairFleetOrder)
			{
				return true;
			}
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				if (item is RepairFleetOrder)
				{
					return true;
				}
			}
			return false;
		}

		public bool DoesFleetHaveRearmOrder(Fleet fleet)
		{
			if (fleet.ActiveOrder != null && fleet.ActiveOrder.FleetOrder is RearmOrder)
			{
				return true;
			}
			foreach (FleetOrder item in fleet.OrderQueue)
			{
				if (item is RearmOrder)
				{
					return true;
				}
			}
			return false;
		}

		public Fleet GetStrongestFleet(Func<Fleet, bool> predicate, out float bestCombatRating)
		{
			bestCombatRating = 0f;
			Fleet fleet = null;
			foreach (Fleet fleet2 in faction.Fleets)
			{
				if (fleet2.IsValid && (predicate == null || predicate(fleet2)))
				{
					float cachedSimpleCombatRating = fleet2.GetCachedSimpleCombatRating();
					if (fleet == null || cachedSimpleCombatRating > bestCombatRating)
					{
						bestCombatRating = cachedSimpleCombatRating;
						fleet = fleet2;
					}
				}
			}
			return fleet;
		}

		public void AssignAllPilotRanks(bool updateDebugInfo)
		{
			PromotePilotsFromShipSeeder.AssignRanksForFaction(faction, updateDebugInfo);
		}

		public bool AnyFleetBuildingStations()
		{
			foreach (Fleet fleet in faction.Fleets)
			{
				if (fleet != null)
				{
					if (fleet.ActiveOrder is ActiveBuildStationOrder)
					{
						return true;
					}
					if (fleet.OrderQueue.Count > 0 && fleet.OrderQueue[0] is BuildStationOrder)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
