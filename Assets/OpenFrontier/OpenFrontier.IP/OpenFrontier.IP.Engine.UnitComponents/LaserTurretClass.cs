using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class LaserTurretClass : TurretClass
	{
		[SerializeField]
		private float duration = 2f;

		[SerializeField]
		private float fadeOutTime = 1f;

		public float FiringTimeout = 5f;

		public ParticleSystem[] ImpactPrefabs;

		public GameObject LaserLineRendererPrefab;

		public Vector2 LaserMaterialScaleRate = new Vector2(-1f, 0f);

		public float LaserMoveSpd = 20f;

		public float LaserTextureRealWorldSize = 1f;

		public float MaxRangeMaxDamageMultiplier = 0.25f;

		public float MaxRangeMinDamageMultiplier = 0.2f;

		public float MiningDamageMultiplier = 0.1f;

		public float FadeOutTime
		{
			get
			{
				return fadeOutTime;
			}
			set
			{
				fadeOutTime = value;
			}
		}

		public float Duration
		{
			get
			{
				return duration;
			}
			set
			{
				duration = value;
			}
		}

		public override float GetMinFireRequiredEnergy()
		{
			return EnergyCost * 0.325f;
		}

		protected override ComponentBase createComponent(GameObject target)
		{
			LaserTurretComponent laserTurretComponent = target.AddComponent<LaserTurretComponent>();
			laserTurretComponent.AutoChargeEnabled = true;
			laserTurretComponent.LaserTurretClass = this;
			laserTurretComponent.TurretClass = this;
			return laserTurretComponent;
		}
	}
}
