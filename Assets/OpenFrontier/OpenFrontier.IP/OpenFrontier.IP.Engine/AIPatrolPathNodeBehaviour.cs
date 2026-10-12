using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AIPatrolPathNodeBehaviour : MonoBehaviour, IPatrolPathNode
	{
		public int Order;

		private AIPatrolPath patrolPath;

		public Vector3 SectorPosition
		{
			get
			{
				return transform.position;
			}
			set
			{
				transform.position = value;
			}
		}

		public Sector Sector
		{
			get
			{
				if (patrolPath != null)
				{
					return patrolPath.Sector;
				}
				return null;
			}
		}

		public AIPatrolPath Path
		{
			get
			{
				return patrolPath;
			}
			set
			{
				patrolPath = value;
			}
		}

		public void FindParents()
		{
			Path = UnityObjectHelper.FindInParentsOrSelf<AIPatrolPath>(gameObject);
		}

		private void Awake()
		{
			if (Path == null)
			{
				FindParents();
			}
		}
	}
}
