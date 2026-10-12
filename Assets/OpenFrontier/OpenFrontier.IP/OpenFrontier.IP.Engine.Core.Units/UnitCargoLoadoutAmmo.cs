using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public class UnitCargoLoadoutAmmo : MonoBehaviour
	{
		public bool AutoApply = true;

		private Unit unit;

		public bool IgnoreBayCapacity = true;

		public float LoadoutMultiplier = 1f;

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

		public void Apply(Unit unit)
		{
			if (unit != null)
			{
				if (unit.CargoBayComponent != null)
				{
					foreach (TurretComponent turret in unit.Components.Turrets)
					{
						if (turret is ProjectileTurretComponent projectileTurretComponent)
						{
							foreach (ProjectileClass compatibleProjectile in projectileTurretComponent.ProjectileTurretClass.CompatibleProjectiles)
							{
								if (compatibleProjectile.AmmoClass != null)
								{
									int delta = Mathf.CeilToInt((float)compatibleProjectile.DefaultAmmoComplement * LoadoutMultiplier);
									unit.CargoBayComponent.AddToCargo(compatibleProjectile.AmmoClass, delta, ignoreCapacity: true);
								}
							}
						}
					}
					return;
				}
				Debug.LogWarning($"{this}: Not attached to a unit with a cargo bay");
			}
			else
			{
				Debug.LogWarning($"{this}: Not attached to a gameobject with a unit");
			}
		}
	}
}
