using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnitExplosion : MonoBehaviour
	{
		public Unit DestroyedUnit;

		private void Update()
		{
			if ((bool)DestroyedUnit)
			{
				transform.position = DestroyedUnit.transform.position;
			}
		}
	}
}
