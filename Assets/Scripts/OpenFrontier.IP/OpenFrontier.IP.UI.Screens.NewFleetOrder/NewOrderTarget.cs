using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;

namespace OpenFrontier.IP.UI.Screens.NewFleetOrder
{
	public class NewOrderTarget
	{
		private bool anyUnitHasMiningEquipment;

		private bool anyFleetHasHomeBase;

		private HashSet<int> allOrderedUnitIds = new HashSet<int>(8);

		private List<Unit> allUnits = new List<Unit>(8);

		private List<Unit> orderedUnits = new List<Unit>();

		private List<Fleet> orderedFleets = new List<Fleet>();

		private List<Unit> unitsWithoutFleet = new List<Unit>();

		private List<Sector> sectors = new List<Sector>();

		private bool allUnitsMobile = true;

		private bool allUnitsCanDock = true;

		private bool anyUnitCanTransportPassengers;

		private bool anyUnitArmed;

		private bool anyUnitIsDocked;

		private bool anyUnitHasTractorBeam;

		private Faction faction;

		public bool AnyUnitHasTractorBeam => anyUnitHasTractorBeam;

		public bool AnyUnitHasMiningEquipment => anyUnitHasMiningEquipment;

		public bool AnyUnitCanTransportPassengers => anyUnitCanTransportPassengers;

		public bool AnyUnitDocked => anyUnitIsDocked;

		public bool AnyFleetHasHomeBase => anyFleetHasHomeBase;

		public bool AnyUnitArmed => anyUnitArmed;

		public bool AllUnitsMobile => allUnitsMobile;

		public bool AllUnitsCanDock => allUnitsCanDock;

		public IEnumerable<Fleet> OrderedFleets => orderedFleets;

		public IEnumerable<Unit> OrderedUnits => orderedUnits;

		public IEnumerable<Unit> UnitsWithoutFleet => unitsWithoutFleet;

		public IEnumerable<Unit> AllUnits => allUnits;

		public bool IsSingleFleet
		{
			get
			{
				if (orderedFleets.Count == 1)
				{
					if (orderedUnits.Count != 0)
					{
						return unitsWithoutFleet.Count == 0;
					}
					return true;
				}
				return false;
			}
		}

		public bool IsSingleUnitWithoutFleet
		{
			get
			{
				if (orderedFleets.Count == 0)
				{
					return orderedUnits.Count == 1;
				}
				return false;
			}
		}

		public Unit SingleUnitWithoutFleet
		{
			get
			{
				if (IsSingleUnitWithoutFleet)
				{
					return orderedUnits[0];
				}
				return null;
			}
		}

		public Fleet SingleFleet
		{
			get
			{
				if (IsSingleFleet)
				{
					return orderedFleets[0];
				}
				return null;
			}
		}

		public bool HasMultipleFleets => orderedFleets.Count > 1;

		public bool MixedUnitsAndFleets
		{
			get
			{
				if (orderedFleets.Count > 0)
				{
					return unitsWithoutFleet.Count > 0;
				}
				return false;
			}
		}

		public Sector SingleSector
		{
			get
			{
				if (sectors.Count == 1)
				{
					return sectors[0];
				}
				return null;
			}
		}

		public List<Sector> Sectors => sectors;

		public bool IsValid => allOrderedUnitIds.Count > 0;

		public Faction Faction => faction;

		public static NewOrderTarget CreateFromUnits(IEnumerable<Unit> units)
		{
			NewOrderTarget newOrderTarget = new NewOrderTarget();
			foreach (Unit unit in units)
			{
				newOrderTarget.orderedUnits.Add(unit);
				Fleet fleet = unit.GetFleet();
				if (fleet != null)
				{
					if (!newOrderTarget.orderedFleets.Contains(fleet))
					{
						newOrderTarget.orderedFleets.Add(fleet);
					}
				}
				else
				{
					newOrderTarget.unitsWithoutFleet.Add(unit);
				}
			}
			newOrderTarget.faction = newOrderTarget.orderedUnits[0].Faction;
			newOrderTarget.CompileAllOrderedUnits();
			return newOrderTarget;
		}

		public static NewOrderTarget CreateFromFleets(Fleet fleet)
		{
			NewOrderTarget newOrderTarget = new NewOrderTarget
			{
				orderedFleets = { fleet }
			};
			newOrderTarget.faction = newOrderTarget.orderedFleets[0].Faction;
			newOrderTarget.CompileAllOrderedUnits();
			return newOrderTarget;
		}

		public static NewOrderTarget CreateFromFleets(IEnumerable<Fleet> fleets)
		{
			NewOrderTarget newOrderTarget = new NewOrderTarget();
			foreach (Fleet fleet in fleets)
			{
				newOrderTarget.orderedFleets.Add(fleet);
			}
			newOrderTarget.faction = newOrderTarget.orderedFleets[0].Faction;
			newOrderTarget.CompileAllOrderedUnits();
			return newOrderTarget;
		}

		private void CompileAllOrderedUnits()
		{
			anyUnitCanTransportPassengers = false;
			sectors.Clear();
			allUnitsMobile = true;
			anyUnitArmed = false;
			allUnitsCanDock = true;
			anyUnitIsDocked = false;
			anyFleetHasHomeBase = false;
			anyUnitHasMiningEquipment = false;
			anyUnitHasTractorBeam = false;
			allUnits.Clear();
			foreach (Unit orderedUnit in orderedUnits)
			{
				if (!allOrderedUnitIds.Contains(orderedUnit.UniqueId))
				{
					allOrderedUnitIds.Add(orderedUnit.UniqueId);
				}
				if (orderedUnit.Sector != null && !sectors.Contains(orderedUnit.Sector))
				{
					sectors.Add(orderedUnit.Sector);
				}
				allUnits.Add(orderedUnit);
			}
			foreach (Fleet orderedFleet in orderedFleets)
			{
				if (orderedFleet.IsHomeBaseValid)
				{
					anyFleetHasHomeBase = true;
				}
				foreach (UnitComponentHolder ship in orderedFleet.Ships)
				{
					if (ship != null)
					{
						Unit unit = ship.Unit;
						if (!allOrderedUnitIds.Contains(ship.Unit.UniqueId))
						{
							allOrderedUnitIds.Add(ship.Unit.UniqueId);
						}
						allUnits.Add(unit);
					}
				}
				if (orderedFleet.Sector != null && !sectors.Contains(orderedFleet.Sector))
				{
					sectors.Add(orderedFleet.Sector);
				}
			}
			foreach (Unit allUnit in allUnits)
			{
				if (!allUnit.IsMobile)
				{
					allUnitsMobile = false;
				}
				if (allUnit.IsArmed)
				{
					anyUnitArmed = true;
				}
				if (!allUnit.CanDock)
				{
					allUnitsCanDock = false;
				}
				if (allUnit.Components.PassengerCapacity > 0)
				{
					anyUnitCanTransportPassengers = true;
				}
				if (allUnit.IsDocked)
				{
					anyUnitIsDocked = true;
				}
				if (!anyUnitHasMiningEquipment && allUnit.HasMiningLaser() && allUnit.HasTractorBeam())
				{
					anyUnitHasMiningEquipment = true;
				}
				if (!anyUnitHasTractorBeam && allUnit.HasTractorBeam())
				{
					anyUnitHasTractorBeam = true;
				}
			}
		}

		public bool IsUnitOrdered(Unit unit)
		{
			if (unit != null)
			{
				return allOrderedUnitIds.Contains(unit.UniqueId);
			}
			return false;
		}

		public bool IsFleetOrdered(Fleet fleet)
		{
			return orderedFleets.Contains(fleet);
		}

		public IEnumerable<Fleet> CreateAndReturnFleets()
		{
			List<Fleet> list = new List<Fleet>();
			list.AddRange(orderedFleets);

			// A copy that does not own the world does not build fleets.
			//
			// It used to build them here, unconditionally, which is how a
			// client came to hold a fleet the host had never heard of - the
			// order that followed named it and was refused, and the world
			// quietly carried a ship, an NPC and a fleet that existed in one
			// place and nowhere else.
			//
			// The ships are named instead, and the world that owns them builds
			// the fleet when it carries the order out.
			if (Authority.IsThisCopyAClient)
			{
				NoteLooseShipsWithoutFleets(list);

				return list;
			}

			foreach (Unit item in unitsWithoutFleet)
			{
				Fleet fleet = item.GetFleet();
				if (fleet == null)
				{
					fleet = OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(item);
					list.Add(fleet);
				}
			}
			return list;
		}

		/// <summary>
		/// Hands over the selected ships that have no fleet, and clears them
		/// from the selection so a second order does not carry them again.
		/// </summary>
		private void NoteLooseShipsWithoutFleets(List<Fleet> alreadyHave)
		{
			List<int> loose = null;

			foreach (Unit item in unitsWithoutFleet)
			{
				if (item == null || item.IsDestroyed || item.GetFleet() != null)
				{
					continue;
				}

				if (FleetContaining(alreadyHave, item) != null)
				{
					continue;
				}

				if (loose == null)
				{
					loose = new List<int>();
				}

				loose.Add(item.UniqueId);
			}

			FleetOrderIntents.NoteLooseUnits(loose);

			unitsWithoutFleet.Clear();
		}

		/// <summary>
		/// Which of these fleets already contains this ship, if any.
		/// </summary>
		/// <remarks>
		/// A ship can be both selected on its own and inside a selected fleet,
		/// and naming it twice would have the host build a second fleet around
		/// a ship that has one.
		/// </remarks>
		private static Fleet FleetContaining(List<Fleet> fleets, Unit unit)
		{
			if (fleets == null || unit == null)
			{
				return null;
			}

			foreach (Fleet fleet in fleets)
			{
				if (fleet != null && fleet.ContainsUnit(unit))
				{
					return fleet;
				}
			}

			return null;
		}

		public bool AnyUnitNeedsRepair(float hullConditionThreshold, float componentsConditionThreshold, float shieldConditionThreshold)
		{
			foreach (Unit allUnit in allUnits)
			{
				if (allUnit != null && ActiveWaitForAutoRepairOrder.DoesUnitNeedRepair(allUnit, hullConditionThreshold, componentsConditionThreshold, shieldConditionThreshold))
				{
					return true;
				}
			}
			return false;
		}

		public List<CargoClass> GetAllCargoTypes()
		{
			List<CargoClass> list = new List<CargoClass>();
			foreach (Unit allUnit in allUnits)
			{
				if (!(allUnit.CargoBayComponent != null))
				{
					continue;
				}
				foreach (CargoClass cargoClass in allUnit.CargoBayComponent.CargoClasses)
				{
					if (!cargoClass.IsReserved && !list.Contains(cargoClass))
					{
						list.Add(cargoClass);
					}
				}
			}
			return list;
		}
	}
}
