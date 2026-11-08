using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnitMoon : MonoBehaviour
	{
		private Unit unit;

		private Moon moon;

		private bool hasInit;

		public float OrbitsPerSeconds = 0.00025f;

		private ActiveUnit activeUnit;

		public Unit OrbitUnit
		{
			get
			{
				if (moon != null)
				{
					return moon.OrbitingAroundUnit;
				}
				return null;
			}
		}

		public void Init()
		{
			activeUnit = GetComponent<ActiveUnit>();
			unit = activeUnit.Unit;
			if (unit == null)
			{
				Debug.LogError("Unit expected", this);
				return;
			}
			moon = unit.GetComponent<Moon>();
			if (moon == null)
			{
				Debug.LogError("Moon expected", this);
				return;
			}
			hasInit = true;
			Reposition();
		}

		private void FixedUpdate()
		{
			if (hasInit)
			{
				Reposition();
			}
		}

		public void Reposition()
		{
			if (OrbitUnit != null && (bool)OrbitUnit)
			{
				float y = (float)unit.Seed % 360f + (float)(EngineASX.Instance.ScenarioElapsedTime * (double)OrbitsPerSeconds * 360.0 % 360.0);
				unit.transform.localPosition = OrbitUnit.transform.localPosition + Quaternion.Euler(0f, y, 0f) * moon.OffsetFromPlanet;
			}
		}
	}
}
