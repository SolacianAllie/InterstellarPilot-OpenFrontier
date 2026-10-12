using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class ActiveRearmAtNearestOrder : ActiveRearmOrder
	{
		private RearmAtNearestSearchOperation searchOperation;

		private float lastStartSearchTime;

		public bool IsSearching => searchOperation != null;

		private void Update()
		{
			if (IsValid && IsSearching && !searchOperation.HasFinished && fleet != null)
			{
				searchOperation.Process(Time.deltaTime);
			}
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (CurrentRearmLocation == null)
			{
				IsIdle = true;
				if (!IsSearching)
				{
					if (Time.time > lastStartSearchTime + 2f)
					{
						StartSearch();
						lastStartSearchTime = Time.time;
					}
				}
				else if (searchOperation.HasFinished)
				{
					CurrentRearmLocation = searchOperation.Result;
					searchOperation = null;
				}
			}
			else
			{
				IsIdle = false;
			}
		}

		private void StartSearch()
		{
			searchOperation = new RearmAtNearestSearchOperation();
			searchOperation.ShipHullTypes = fleet.NpcShipHullTypes;
			searchOperation.LocalFleet = fleet;
			searchOperation.InitialiseAndStartSearch(fleet.Sector, fleet.GetHomeSectorOrCurrent(), fleet.SectorPosition, fleet.Faction, GetActualMaxJumpDist());
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			CurrentRearmLocation = null;
			State = ActiveRearmFleetOrderState.None;
		}

		public override void OnNpcUnableToDockAtNavpoint(NpcPilot npc, Unit dock, UnableToDockReason unableToDockReason)
		{
			if (unableToDockReason == UnableToDockReason.Refused || unableToDockReason == UnableToDockReason.Other)
			{
				CurrentRearmLocation = null;
				State = ActiveRearmFleetOrderState.None;
			}
			else
			{
				base.OnNpcUnableToDockAtNavpoint(npc, dock, unableToDockReason);
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			switch (State)
			{
			case ActiveRearmFleetOrderState.None:
				return "Looking for rearming station";
			case ActiveRearmFleetOrderState.MoveToStation:
				if (CurrentRearmLocation != null)
				{
					return "Move to " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, CurrentRearmLocation);
				}
				break;
			case ActiveRearmFleetOrderState.Rearming:
				if (rearmHasInsufficientCredits)
				{
					return "Insufficient credits";
				}
				return "Rearm In progress";
			}
			return base.GetStatusTextInternal(localFaction);
		}
	}
}
