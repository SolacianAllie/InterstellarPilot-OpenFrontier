using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AsteroidCluster : MonoBehaviour
	{
		[SerializeField]
		private Unit unit;

		public AsteroidType AsteroidType;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				unit = value;
			}
		}

		public void Init(Unit unit)
		{
			this.unit = unit;
		}
	}
}
