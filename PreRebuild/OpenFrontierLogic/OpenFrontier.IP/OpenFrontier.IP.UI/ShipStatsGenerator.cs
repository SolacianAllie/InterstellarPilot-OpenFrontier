using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI
{
	public class ShipStatsGenerator : StatGeneratorUI<UnitClass>
	{
		protected override void refresh()
		{
			base.refresh();
			AddStat("Manufacturer", (Item.Manufacturer != null) ? Item.Manufacturer.GetShortNameElseLong() : "-");
			AddStat("Avg. Price", Item.SaleCost);
			ComponentBay[] componentsInChildren = Item.UnitPrefab.GetComponentsInChildren<ComponentBay>(includeInactive: true);
			AddStat("Hull Strength", Item.maxHealth);
			ComponentBay componentBay = componentsInChildren.FirstOrDefault((ComponentBay e) => e.InitialComponentClass is ShieldClass);
			ShieldClass shieldClass = ((componentBay != null) ? ((ShieldClass)componentBay.InitialComponentClass) : null);
			if (shieldClass != null)
			{
				AddStat("Shields", shieldClass.Capacities[0], 0);
			}
			AddStat("Cost to Repair", $"{Item.RepairCostMultiplier:P1}");
			if (Item.UnitType == UnitType.Ship)
			{
				ComponentBay componentBay2 = componentsInChildren.FirstOrDefault((ComponentBay e) => e.InitialComponentClass is UnitEngineClass);
				UnitEngineClass unitEngineClass = ((componentBay2 != null) ? ((UnitEngineClass)componentBay2.InitialComponentClass) : null);
				AddStat("Max Speed", (unitEngineClass != null) ? unitEngineClass.CalculateMaxSpeed(Item.Drag, Item.UnitPrefab.Mass) : 0f, 1);
				AddStat("Turn Rate", Item.turnRate, 1);
			}
			UnitComponentHolder component = Item.UnitPrefab.GetComponent<UnitComponentHolder>();
			if (component != null)
			{
				AddStat("Cargo Space", component.CargoCapacity, 0);
			}
			ComponentBay componentBay3 = componentsInChildren.FirstOrDefault((ComponentBay e) => e.InitialComponentClass is CapacitorClass);
			CapacitorClass capacitorClass = ((componentBay3 != null) ? ((CapacitorClass)componentBay3.InitialComponentClass) : null);
			if (capacitorClass != null)
			{
				AddStat("Capacitor", capacitorClass.Capacity, 0);
			}
			ComponentBay componentBay4 = componentsInChildren.FirstOrDefault((ComponentBay e) => e.InitialComponentClass is PwrGeneratorClass);
			PwrGeneratorClass pwrGeneratorClass = ((componentBay4 != null) ? ((PwrGeneratorClass)componentBay4.InitialComponentClass) : null);
			if (pwrGeneratorClass != null)
			{
				AddStat("Pwr Gen Rate", pwrGeneratorClass.PowerGenRate, 0);
			}
			PassengerModuleClass passengerModuleClass = (from e in componentsInChildren
				where e.InitialComponentClass is PassengerModuleClass
				select (PassengerModuleClass)e.InitialComponentClass).FirstOrDefault();
			AddStat("Passenger Capacity", (passengerModuleClass != null) ? passengerModuleClass.PassengerCapacity : 0);
			AddStat("Repair Facilities", Helper.BoolToYesNo(Item.HasRepairFacilities));
			if (Item.HasRepairFacilities)
			{
				AddStat("Repair Facilities Discount", $"{1f - Item.RepairFacilitiesCostMultiplier:P1}");
			}
			List<UnitHangarBay> list = new List<UnitHangarBay>();
			Item.UnitPrefab.transform.AddComponentsInChildrenToList(list);
			AddStat("Hangar Bays", list.Count);
		}
	}
}
