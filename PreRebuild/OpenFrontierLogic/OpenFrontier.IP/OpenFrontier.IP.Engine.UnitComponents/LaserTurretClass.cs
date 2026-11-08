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

		[Tooltip("How a weapon of this class is mounted. Fixed fires straight ahead and cannot aim, Gimbal tracks within a limited arc, Turret tracks freely. Stored for now and not yet acted on.")]
		public WeaponMountMode MountMode = WeaponMountMode.Turret;

		[Tooltip("Damage one shot does before armour and range. The Elite tables give this per shot for pulse and burst weapons.")]
		public float DamagePerShot;

		[Tooltip("Sustained damage per second. Continuous weapons such as the beam laser have no rate of fire, so this is their only damage figure.")]
		public float DamagePerSecond;

		[Tooltip("Shots per second. Zero for a continuous weapon, which has no rate of fire.")]
		public float RateOfFire;

		[Tooltip("How well this weapon punches through armour, on the Elite 0 to 100 scale. Higher ignores more of a target's armour.")]
		public float ArmourPiercing;

		[Tooltip("Heat this weapon adds per shot. Nothing reads it yet - there is no heat system - but the Elite tables give it for every thermal weapon.")]
		public float ThermalLoad;

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
