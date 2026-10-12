using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI
{
	public class ComponentInfoGenerator : StatGeneratorUI<ComponentClass>
	{
		protected override void refresh()
		{
			base.refresh();
			AddStat("Avg. Price", TextFormattingHelper.FormatCredits(Item.GetActualCost(), includeSuffix: true));
			if (Item is PwrGeneratorClass)
			{
				PwrGeneratorClass pwrGeneratorClass = (PwrGeneratorClass)Item;
				AddStat("Gen. Rate", pwrGeneratorClass.PowerGenRate, 1);
			}
			else if (Item is TractorTurretClass)
			{
				TractorTurretClass tractorTurretClass = (TractorTurretClass)Item;
				AddStat("Energy Cost", tractorTurretClass.EnergyCost, 1);
				AddStat("Charge Time", GetChargeTimeText(tractorTurretClass));
				AddStat("Range", tractorTurretClass.MaxFiringRange);
			}
			else if (Item is LaserTurretClass)
			{
				LaserTurretClass laserTurretClass = (LaserTurretClass)Item;
				AddStat("Energy Cost", laserTurretClass.EnergyCost, 1);
				AddStat("Efficiency", laserTurretClass.EnergyToDamageConversion, 2);
				AddStat("Charge Time", GetChargeTimeText(laserTurretClass));
				AddStat("Range", laserTurretClass.MaxFiringRange);
				AddStat("Mining Pwr", laserTurretClass.MiningDamageMultiplier, 1);
			}
			else if (Item is ProjectileTurretClass)
			{
				ProjectileTurretClass projectileTurretClass = (ProjectileTurretClass)Item;
				AddStat("Energy Cost", projectileTurretClass.EnergyCost, 1);
				if (projectileTurretClass.CompatibleProjectiles.Count == 1 && projectileTurretClass.CompatibleProjectiles[0] != null && projectileTurretClass.CompatibleProjectiles[0].AmmoClass == null)
				{
					ProjectileClass projectileClass = projectileTurretClass.CompatibleProjectiles[0];
					if (projectileClass.SetDamageUsingEnergyCost)
					{
						AddStat("Efficiency", projectileTurretClass.EnergyToDamageConversion, 2);
					}
					else
					{
						AddStat("Damage", projectileClass.Damage);
					}
					AddStat("Speed", projectileClass.MoveSpeed);
				}
				AddStat("Charge Time", GetChargeTimeText(projectileTurretClass));
				AddStat("Range", projectileTurretClass.MaxFiringRange);
			}
			else if (Item is PassengerModuleClass)
			{
				PassengerModuleClass passengerModuleClass = (PassengerModuleClass)Item;
				AddStat("Capacity", passengerModuleClass.PassengerCapacity);
			}
			else if (Item is CapacitorClass)
			{
				CapacitorClass capacitorClass = (CapacitorClass)Item;
				AddStat("Capacity", capacitorClass.Capacity);
			}
			else if (Item is CloakComponentClass)
			{
				CloakComponentClass cloakComponentClass = (CloakComponentClass)Item;
				AddStat("Energy Cost", cloakComponentClass.EnergyCost, 1);
				AddStat("Cloak Time", cloakComponentClass.CloakTime, 1);
				AddStat("Decloak Time", cloakComponentClass.CloakTime, 1);
			}
			else if (Item is ShieldClass)
			{
				ShieldClass shieldClass = (ShieldClass)Item;
				AddStat("Shield Capacity", shieldClass.Capacities[0]);
				AddStat("Regen Rate", GameController.Instance.GameSettings.ShieldRelativeRegenRate * shieldClass.Capacities[0] * shieldClass.RegenRateMultiplier, 2);
			}
			else if (Item is UnitEngineClass)
			{
				UnitEngineClass unitEngineClass = (UnitEngineClass)Item;
				AddStat("Engine Power", unitEngineClass.EnginePower, 1);
			}
		}

		private string GetChargeTimeText(TurretClass p)
		{
			return $"{p.FireInterval:n2}s";
		}
	}
}
