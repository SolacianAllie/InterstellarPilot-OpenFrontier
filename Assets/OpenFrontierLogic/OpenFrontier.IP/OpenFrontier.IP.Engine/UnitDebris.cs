using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitDebris : MonoBehaviour
	{
		private EngineASX engine;

		public bool Expires = true;

		private double expiryTime = 5.0;

		public UnitClass RelatedUnitClass;

		public int ScrapQuantity = 1;

		private Unit unit;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			private set
			{
				if (unit != value)
				{
					unit = value;
				}
			}
		}

		public double ExpiryTime
		{
			get
			{
				return expiryTime;
			}
			set
			{
				expiryTime = value;
			}
		}

		public void Init()
		{
			engine = EngineASX.Instance;
			if (engine == null)
			{
				Debug.LogError("Engine null");
			}
			Unit = GetComponent<Unit>();
			unit.DebrisComponent = this;
			expiryTime = engine.ScenarioElapsedTime + (double)engine.GameSettings.DebrisLifetime;
		}

		public void Tick()
		{
			if (unit != null && Expires && engine.ScenarioElapsedTime > expiryTime && (!unit.IsInActiveSector || (!unit.IsOwnedByPlayer && (unit.ActiveUnit == null || unit.ActiveUnit.LastDistanceFromCamera > GameController.Instance.GameSettings.ExpireCargoInActiveSceneDistanceFromCamera))))
			{
				unit.SafeDestroy();
				unit = null;
			}
		}

		public void UpdateName()
		{
			if (RelatedUnitClass != null)
			{
				unit.UnitName = $"{RelatedUnitClass.GetClassAndSeriesName()} Debris";
			}
			else
			{
				unit.UnitName = "Ship Debris";
			}
		}

		private void Update()
		{
			Tick();
		}
	}
}
