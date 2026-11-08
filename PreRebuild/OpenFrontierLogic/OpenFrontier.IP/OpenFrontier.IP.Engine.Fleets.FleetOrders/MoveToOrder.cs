using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class MoveToOrder : FleetOrder
	{
		public float ArrivalThreshold;

		public bool CompleteOnReachTarget = true;

		public bool MatchTargetOrientation;

		public Vector3? PreferredRelativeVectorFromTarget;

		public SectorTarget Target;

		public override FleetOrderType OrderType => FleetOrderType.MoveTo;

		public override string GetDescription()
		{
			return "Move";
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (faction != null && Target != null)
			{
				if (Target.HadSceneObject)
				{
					if (Target.TargetUnit != null)
					{
						return "Move to " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(faction, currentSector, Target.TargetUnit);
					}
				}
				else
				{
					Sector targetSector = Target.GetTargetSector();
					if (targetSector != null)
					{
						return "Move to {" + TextFormattingHelper.FormatSectorPosition(Target.SectorPosition) + "} in " + UnitNamer.GetSectorNameAndDistanceForFaction(faction, currentSector, targetSector);
					}
				}
			}
			return GetDescription();
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveMoveToOrder activeMoveToOrder = gameObject.AddComponent<ActiveMoveToOrder>();
			activeMoveToOrder.MoveToObjective = this;
			return activeMoveToOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
