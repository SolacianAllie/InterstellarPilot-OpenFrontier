using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class PatrolOrder : PatrolOrderBase
	{
		public bool IsLoop;

		public List<AIPatrolPathNode> Nodes = new List<AIPatrolPathNode>();

		public override FleetOrderType OrderType => FleetOrderType.Patrol;

		public override int NodeCount => Nodes.Count;

		public override bool IsPathALoop => IsLoop;

		public override IPatrolPathNode GetNodeAtIndex(int index)
		{
			return Nodes[index];
		}

		public override string GetDescription()
		{
			IEnumerable<Sector> source = Nodes.Select((AIPatrolPathNode e) => e.Sector).Distinct();
			if (source.Any())
			{
				int count = 3;
				source.Count();
				IEnumerable<Sector> source2 = source.Take(count);
				string text = "Patrol " + string.Join(", ", source2.Select((Sector e) => e.Name));
				if (source2.Count() < source.Count())
				{
					text += $" + {source.Count() - source2.Count()} more";
				}
				return text;
			}
			return base.GetDescription();
		}
	}
}
