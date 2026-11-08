using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitCargoLoadoutAuto : UnitCargoLoadout
	{
		public bool AutoApply = true;

		private Unit unit;

		private void Awake()
		{
			unit = GetComponentInParent<Unit>();
		}

		private void Update()
		{
			if (AutoApply && EngineASX.LoadedAndReady)
			{
				Apply(unit);
				Object.Destroy(this);
			}
		}
	}
}
