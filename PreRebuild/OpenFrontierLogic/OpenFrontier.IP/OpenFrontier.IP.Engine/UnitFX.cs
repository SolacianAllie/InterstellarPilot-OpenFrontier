using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitFX : MonoBehaviour
	{
		private ActiveUnit activeUnit;

		private EngineASX engine;

		private bool isActive;

		public bool IsActive
		{
			get
			{
				return isActive;
			}
			set
			{
				if (isActive != value)
				{
					isActive = value;
					RecycleObjects();
					if (isActive)
					{
						OnFxActive();
					}
					else
					{
						OnFxInactive();
					}
				}
			}
		}

		public ActiveUnit ActiveUnit => activeUnit;

		public EngineASX Engine => engine;

		protected virtual float DrawDistanceMultiplier => 1f;

		protected virtual void OnFxInactive()
		{
		}

		public void Awake()
		{
			activeUnit = UnityObjectHelper.FindInParentsOrSelf<ActiveUnit>(gameObject);
			engine = EngineASX.Instance;
			awake();
		}

		public void Start()
		{
		}

		protected virtual void awake()
		{
		}

		protected virtual bool ShouldBeActive()
		{
			if (activeUnit != null && InDrawRange())
			{
				return activeUnit.ShouldEffectsBeDrawn;
			}
			return false;
		}

		protected virtual void update()
		{
		}

		protected virtual void OnFxActive()
		{
		}

		protected virtual void RecycleObjects()
		{
		}

		private void Update()
		{
			IsActive = ShouldBeActive();
			update();
		}

		private bool InDrawRange()
		{
			if (isActive)
			{
				if (ActiveUnit.LastDistanceFromCamera > ActiveUnit.ActiveUnitClass.DamageParticlesDrawFar * DrawDistanceMultiplier)
				{
					return false;
				}
			}
			else if (ActiveUnit.LastDistanceFromCamera < ActiveUnit.ActiveUnitClass.DamageParticlesDrawNear * DrawDistanceMultiplier)
			{
				return true;
			}
			return isActive;
		}

		private void OnDestroy()
		{
			IsActive = false;
		}
	}
}
