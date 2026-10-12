using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AIPatrolPath : MonoBehaviour
	{
		private EngineASX engine;

		public bool IsLoop;

		private List<AIPatrolPathNodeBehaviour> nodes = new List<AIPatrolPathNodeBehaviour>();

		private Sector sector;

		[SerializeField]
		private int uniqueId = -1;

		public int UniqueId
		{
			get
			{
				return uniqueId;
			}
			set
			{
				uniqueId = value;
			}
		}

		public int NodeCount => nodes.Count;

		public EngineASX Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				if (!(engine != value))
				{
					return;
				}
				EngineASX engineASX = engine;
				engine = value;
				if (engineASX != null)
				{
					engineASX.DeregisterPath(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniquePatrolPathId();
					}
					engine.RegisterPath(this);
				}
			}
		}

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

		public IEnumerable<AIPatrolPathNodeBehaviour> Nodes => nodes;

		public void FindNodes()
		{
			nodes.Clear();
			nodes.AddRange(from e in GetComponentsInChildren<AIPatrolPathNodeBehaviour>()
				orderby e.Order
				select e);
			foreach (AIPatrolPathNodeBehaviour node in nodes)
			{
				node.Path = this;
			}
		}

		public void Init()
		{
			Engine = EngineASX.Instance;
			if (Sector == null)
			{
				Sector = UnityObjectHelper.FindInParentsOrSelf<Sector>(gameObject);
			}
			FindNodes();
		}

		public AIPatrolPathNodeBehaviour GetNodeAtIndex(int index)
		{
			return nodes[index];
		}
	}
}
