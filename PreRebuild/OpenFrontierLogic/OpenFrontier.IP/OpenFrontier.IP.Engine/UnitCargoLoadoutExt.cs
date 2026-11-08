using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitCargoLoadoutExt : UnitCargoLoadout
	{
		public bool AutoApply = true;

		public Unit Unit;

		private void Update()
		{
			if (AutoApply)
			{
				Apply(Unit);
				Object.Destroy(this);
			}
		}
	}
}
