using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionAIStrategyModule
	{
		private static Dictionary<FactionStrategy, float> fleetStrategyWeight = new Dictionary<FactionStrategy, float>(8);

		public static FactionAIStrategy BuildStrategy(FactionAIBase factionAI)
		{
			FactionAIStrategy factionAIStrategy = new FactionAIStrategy();
			switch (factionAI.Faction.FactionType)
			{
			case FactionType.Bar:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Trade, 0.75f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 0.2f, canAssignToFleet: true));
				break;
			case FactionType.Bandit:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scout, 0.1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Mine, Random.Range(0.1f, 0.25f), canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Trade, Random.Range(0.1f, 0.25f), canAssignToFleet: true));
				break;
			case FactionType.Empire:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 0.8f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scout, 0.2f, canAssignToFleet: true));
				break;
			case FactionType.EquipmentDealer:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.DealEquipment, 0.75f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Escort, 0.25f, canAssignToFleet: true));
				break;
			case FactionType.Trader:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Trade, 0.75f, canAssignToFleet: true));
				if (!factionAI.Faction.IsFreelancer)
				{
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Escort, 0.25f, canAssignToFleet: true));
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 0.1f, canAssignToFleet: true));
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scout, Mathf.Lerp(0f, 0.1f, factionAI.AISettings.OffensiveStance), canAssignToFleet: true));
				}
				break;
			case FactionType.PassengerTransport:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.PassengerTransport, 0.9f, canAssignToFleet: true));
				if (!factionAI.Faction.IsFreelancer)
				{
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Escort, 0.1f, canAssignToFleet: true));
				}
				break;
			case FactionType.BountyHunter:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.BountyHunt, 1f, canAssignToFleet: true));
				break;
			case FactionType.Explorer:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scout, 1f, canAssignToFleet: true));
				break;
			case FactionType.Mercenary:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 1f, canAssignToFleet: true));
				break;
			case FactionType.Miner:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Mine, 1f, canAssignToFleet: true));
				if (!factionAI.Faction.IsFreelancer)
				{
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Escort, Mathf.Lerp(0f, 1f, factionAI.AISettings.OffensiveStance), canAssignToFleet: true));
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, Mathf.Lerp(0f, 0.35f, factionAI.AISettings.OffensiveStance), canAssignToFleet: true));
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scout, Mathf.Lerp(0f, 0.1f, factionAI.AISettings.OffensiveStance), canAssignToFleet: true));
				}
				break;
			case FactionType.Outlaw:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scout, 0.25f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scavenge, 0.1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Trade, 0.1f, canAssignToFleet: true));
				break;
			case FactionType.Scavenger:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scavenge, 1f, canAssignToFleet: true));
				if (!factionAI.Faction.IsFreelancer)
				{
					factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 0.1f, canAssignToFleet: true));
				}
				break;
			case FactionType.Generic:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scout, 0.5f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Scavenge, 1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Trade, 1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.PassengerTransport, 1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Escort, 1f, canAssignToFleet: true));
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.Mine, 1f, canAssignToFleet: true));
				break;
			default:
				factionAIStrategy.Strategies.Add(new FactionAIStrategyItem(FactionStrategy.War, 0.8f, canAssignToFleet: true));
				break;
			}
			factionAIStrategy.Compile();
			return factionAIStrategy;
		}

		public static FactionStrategy GetBestStrategyForFleet(Fleet fleet, FactionAIBase factionAIBase)
		{
			if (fleet.Ships.Count == 0)
			{
				return FactionStrategy.Unspecified;
			}
			if (factionAIBase.Strategy == null)
			{
				return FactionStrategy.Unspecified;
			}
			GameplaySettings gameplaySettings = GameController.Instance.GameSettings.GameplaySettings;
			float fleetStrategyAssignment_ScoreFuzziness = gameplaySettings.FleetStrategyAssignment_ScoreFuzziness;
			float fleetStrategyAssignment_ShipEffectivenessWeight = gameplaySettings.FleetStrategyAssignment_ShipEffectivenessWeight;
			float fleetStrategyAssignment_FactionStrategyWeight = gameplaySettings.FleetStrategyAssignment_FactionStrategyWeight;
			if (fleet.Ships.Count == 1)
			{
				FactionStrategy factionStrategy = FactionStrategy.Unspecified;
				float num = 0f;
				UnitClass unitClass = fleet.Ships[0].UnitClass;
				bool flag = (EngineASX.Instance.GetUnitClassShipStrategyFlags(unitClass) & factionAIBase.Strategy.ActionableStrategyFlags) == 0;
				foreach (ShipPurposeItem shipPurpose in unitClass.ShipPurposes)
				{
					float num2 = shipPurpose.Effectiveness * fleetStrategyAssignment_ShipEffectivenessWeight;
					if (factionAIBase.Strategy.ActionableStrategies.TryGetValue(shipPurpose.FactionStrategy, out var value))
					{
						num2 += value.Weight / factionAIBase.Strategy.TotalWeight * fleetStrategyAssignment_FactionStrategyWeight;
					}
					else if (!flag)
					{
						continue;
					}
					if (fleet.Faction.IsFreelancer && shipPurpose.FactionStrategy == factionAIBase.Strategy.PrimaryStrategy)
					{
						num2 += 10f;
					}
					num2 += Random.value * fleetStrategyAssignment_ScoreFuzziness;
					if (num2 > num)
					{
						factionStrategy = shipPurpose.FactionStrategy;
						num = num2;
					}
				}
				if (factionStrategy != FactionStrategy.Unspecified)
				{
					return factionStrategy;
				}
				return FactionStrategy.Unspecified;
			}
			fleetStrategyWeight.Clear();
			FactionStrategy unachievableFleetPurposes = FactionStrategy.Unspecified;
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (!(ship != null))
				{
					continue;
				}
				foreach (ShipPurposeItem shipPurpose2 in ship.UnitClass.ShipPurposes)
				{
					if (shipPurpose2.Effectiveness > 0f)
					{
						float value2 = 0f;
						if (!fleetStrategyWeight.TryGetValue(shipPurpose2.FactionStrategy, out value2))
						{
							value2 = 0f;
						}
						float num3 = 0f;
						if (factionAIBase.Strategy.ActionableStrategies.TryGetValue(shipPurpose2.FactionStrategy, out var value3))
						{
							num3 += value3.Weight / factionAIBase.Strategy.TotalWeight * fleetStrategyAssignment_FactionStrategyWeight;
						}
						num3 += shipPurpose2.Effectiveness * fleetStrategyAssignment_ShipEffectivenessWeight;
						num3 += Random.value * fleetStrategyAssignment_ScoreFuzziness;
						num3 *= ship.UnitClass.RelativeShipSaleCost;
						fleetStrategyWeight[shipPurpose2.FactionStrategy] = value2 + num3;
					}
					else
					{
						unachievableFleetPurposes |= shipPurpose2.FactionStrategy;
					}
				}
			}
			List<KeyValuePair<FactionStrategy, float>> list = fleetStrategyWeight.ToList();
			if (list.Count == 0)
			{
				return FactionStrategy.Unspecified;
			}
			return (from e in list
				where unachievableFleetPurposes == FactionStrategy.Unspecified || (e.Key & unachievableFleetPurposes) != 0
				orderby e.Value descending
				select e.Key).FirstOrDefault();
		}
	}
}
