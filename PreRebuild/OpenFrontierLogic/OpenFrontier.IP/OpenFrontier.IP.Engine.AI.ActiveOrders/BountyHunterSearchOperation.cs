using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.Factions.Intel;
using OpenFrontier.IP.Engine.Fleets.ActiveObjectives;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class BountyHunterSearchOperation
	{
		private FactionBountyItem bestBountyBoardItem;

		private float bestTargetScore = float.MinValue;

		private bool hasFinished;

		private Fleet fleet;

		private List<BountyHunterSearchOperationItem> searchItems = new List<BountyHunterSearchOperationItem>(100);

		private ActiveBountyHunterOrder order;

		private double ourNetWorth;

		private Faction ourFaction;

		public FactionBountyItem Result => bestBountyBoardItem;

		public bool HasFinished => hasFinished;

		public void Initialise(ActiveBountyHunterOrder order)
		{
			hasFinished = false;
			bestBountyBoardItem = null;
			bestTargetScore = float.MinValue;
			this.order = order;
			fleet = this.order.Fleet;
			ourFaction = fleet.Faction;
			ourNetWorth = (double)fleet.Faction.GetCachedNetWorth() * (1.0 + (double)fleet.Faction.Aggression);
			PopulateSearchItems();
			if (searchItems.Count == 0)
			{
				hasFinished = true;
			}
		}

		private void PopulateSearchItems()
		{
			searchItems.Clear();
			if (!(ourFaction != null))
			{
				return;
			}
			foreach (FactionAttitude relation in fleet.Faction.Relations)
			{
				Faction targetFaction = relation.TargetFaction;
				if (targetFaction.BountyBoard != null && targetFaction.BountyBoard.BountyItems.Count > 0)
				{
					searchItems.Add(new BountyHunterSearchOperationItem
					{
						Faction = targetFaction
					});
				}
			}
		}

		public void ProcessUntilCompletion()
		{
			while (!hasFinished)
			{
				Process(0.1f);
			}
		}

		public void Process(float elapsedTime)
		{
			int tradeSearchesPerFrame = GetTradeSearchesPerFrame(elapsedTime);
			for (int i = 0; i < tradeSearchesPerFrame; i++)
			{
				if (searchItems.Count > 0)
				{
					ProcessNextItem();
					continue;
				}
				OnNoMoreSearchItems();
				break;
			}
		}

		private int GetTradeSearchesPerFrame(float elapsedTime)
		{
			return Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.AIBountyHunterSearchSearchesPerSecond * elapsedTime);
		}

		private void ProcessNextItem()
		{
			BountyHunterSearchOperationItem bountyHunterSearchOperationItem = searchItems[0];
			searchItems.RemoveAt(0);
			if (fleet.Faction == null)
			{
				return;
			}
			foreach (FactionBountyItem bountyItem in bountyHunterSearchOperationItem.Faction.BountyBoard.BountyItems)
			{
				if (!bountyItem.IsValid || !order.IsTargetPilotValid(bountyItem.Person) || !CanHuntBounty(bountyItem, out var odds))
				{
					continue;
				}
				UniversePath universePath = fleet.Faction.Intel.GetUniversePath(fleet.GetHomeSectorOrCurrent(), bountyItem.LastKnownSector);
				if (universePath != null && universePath.Jumps >= 0 && universePath.Jumps < order.GetActualMaxJumpDist())
				{
					float bountyScore = GetBountyScore(bountyItem, odds);
					if (bestBountyBoardItem == null || bountyScore > bestTargetScore)
					{
						bestBountyBoardItem = bountyItem;
						bestTargetScore = bountyScore;
					}
				}
			}
		}

		private bool CanHuntBounty(FactionBountyItem bountyBoardItem, out float odds)
		{
			odds = 0f;
			if (bountyBoardItem.Source == null)
			{
				return false;
			}
			if (bountyBoardItem.Source == ourFaction)
			{
				return false;
			}
			if (bountyBoardItem.LastKnownSector == null)
			{
				return false;
			}
			if (ourFaction.GetOpinion(bountyBoardItem.Source) < -0.1f)
			{
				return false;
			}
			Faction faction = bountyBoardItem.Person.Faction;
			if (ourFaction.HomeSector != null && ourFaction.HomeSector.ControllingFaction == faction)
			{
				return false;
			}
			if (bountyBoardItem.LastKnownPilottedShip != null)
			{
				odds = AICombatProbabilityCalculator.GetSimpleFleetProbabilityAgainstUnitOrUnitFleet(fleet, bountyBoardItem.LastKnownPilottedShip);
				if (odds + ourFaction.Aggression * 0.2f > 0.5f)
				{
					return true;
				}
				return false;
			}
			return true;
		}

		private void OnNoMoreSearchItems()
		{
			hasFinished = true;
		}

		private float GetBountyScore(FactionBountyItem bounty, float odds)
		{
			int jumpDistanceTo = fleet.Sector.GetJumpDistanceTo(bounty.Person.CurrentUnit.Sector);
			return GetBountyScore(bounty.Bounty, odds, jumpDistanceTo, fleet.Faction.Greed);
		}

		public static float GetBountyScore(float bounty, float odds, float jumpDist, float greed)
		{
			return 0f - Mathf.Clamp(jumpDist, 0f, 8f) + Mathf.Clamp01(bounty / 100000f) * Mathf.Lerp(0.5f, 1f, greed) * 10f + odds * 10f;
		}
	}
}
