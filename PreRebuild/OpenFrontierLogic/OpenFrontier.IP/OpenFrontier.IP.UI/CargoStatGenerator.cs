using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.ActiveEffects;

namespace OpenFrontier.IP.UI
{
	public class CargoStatGenerator : StatGeneratorUI<CargoClass>
	{
		protected override void refresh()
		{
			base.refresh();
			if (!(Item != null))
			{
				return;
			}
			AddStat("Avg Price", TextFormattingHelper.FormatCredits(Item.BasePrice, includeSuffix: true));
			AddStat("Volume", TextFormattingHelper.FormatCargoVolume(Item.Volume));
			if (!(Item.RelatedPrefab != null))
			{
				return;
			}
			Projectile component = Item.RelatedPrefab.GetComponent<Projectile>();
			Countermeasure component2 = Item.RelatedPrefab.GetComponent<Countermeasure>();
			if (component2 == null)
			{
				if (component != null)
				{
					AddStat("Fuel Range", component.ProjectileClass.MaxMoveRange);
					if (component.GetComponent<Missile>() != null)
					{
						AddStat("Speed", component.ProjectileClass.GetMissileMaxSpd(), 1);
					}
					else
					{
						AddStat("Speed", component.ProjectileClass.MoveSpeed, 1);
					}
					if (component.ProjectileClass.ExplosionClass != null && component.ProjectileClass.ExplosionClass.DamageEnabled)
					{
						if (component.ProjectileClass.ExplosionClass.DamageEnabled)
						{
							AddStat("Explosive yield", component.ProjectileClass.ExplosionClass.MaxDamage);
						}
						foreach (ActiveEffectBase activeEffect in component.ProjectileClass.ExplosionClass.ActiveEffects)
						{
							AddStat("Effect", activeEffect.Name);
						}
					}
					if (!component.ProjectileClass.SetDamageUsingEnergyCost)
					{
						AddStat("Damage", component.ProjectileClass.Damage);
					}
				}
				if (Item.RelatedPrefab.GetComponent<Missile>() != null)
				{
					AddStat("Guidance", "Standard");
					AddStat("Turn Rate", component.ProjectileClass.TurnRate, 1);
				}
			}
			else
			{
				AddStat("Effectiveness", component2.CountermeasureClass.Effectiveness, 2);
				AddStat("Range", component2.CountermeasureClass.MaxDisruptionRange);
			}
		}
	}
}
