using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class LaserTurretClass : TurretClass
	{
		[SerializeField]
		private float duration = 2f;

		[SerializeField]
		private float fadeOutTime = 1f;

		[Tooltip("How long this weapon must cool down between shots.")]
		public float FiringTimeout = 5f;

		[Tooltip("Effects spawned where a shot lands.")]
		public ParticleSystem[] ImpactPrefabs;

		[Tooltip("The beam visual this weapon draws.")]
		public GameObject LaserLineRendererPrefab;

		[Tooltip("How fast the beam texture scrolls along the shot.")]
		public Vector2 LaserMaterialScaleRate = new Vector2(-1f, 0f);

		[Tooltip("How fast the beam visual travels.")]
		public float LaserMoveSpd = 20f;

		[Tooltip("World size of one tile of the beam texture, so the beam looks the same at any scale.")]
		public float LaserTextureRealWorldSize = 1f;

		[Tooltip("Damage multiplier for a hit at maximum range.")]
		public float MaxRangeMaxDamageMultiplier = 0.25f;

		[Tooltip("Damage multiplier for a hit at minimum range.")]
		public float MaxRangeMinDamageMultiplier = 0.2f;

		[Tooltip("Extra multiplier when this laser is used for mining.")]
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
