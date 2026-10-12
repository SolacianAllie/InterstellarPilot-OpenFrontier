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

			// A client builds fleets here, like everywhere else.
			//
			// It used not to, on the reasoning that a fleet built on a client
			// is an id the host has never issued. That is true, and it is why
			// the fleet is never SENT - but refusing to build one was the
			// wrong cure, because the property screen, the cargo view and the
			// orders screen are all built around a fleet. A client without
			// one could not open its own ship's cargo, change its settings,
			// or press New Order. The world stopped disagreeing at the price
			// of the game not working.
			//
			// So the fleet is built, the UI works, and the ships in it are
			// named on the order instead of the fleet: OneFleet drops the
			// fleet id on the way out and these go instead. The host resolves
			// each ship against the world it actually owns.
			//
			// Nothing here simulates anything. These ships belong to this
			// player's own faction, and the authority gate declines every
			// ship a client is not flying - so no AI runs over any of it,
			// here or anywhere else. This is a model of a fleet, not a
			// fleet.
			if (Authority.IsThisCopyAClient)
			{
				NoteEverySelectedShipForTheHost(list);

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
		/// Hands over every selected ship, not only the ones without a fleet.
		/// </summary>
		/// <remarks>
		/// Every one, because on a client the fleet ids are being dropped on
		/// the way out and the ships are all that is left to send. Handing
		/// over only the fleet-less ones would leave an order naming nothing
		/// - which the sender reads as 'the player selected nothing' and
		/// discards in silence, so the order would vanish without a word.
		///
		/// Drawn from both halves of the selection, because a player can pick
		/// a ship inside a fleet, a ship outside one, or several of each at
		/// once, and the host is owed all of them.
		/// </remarks>
		private void NoteEverySelectedShipForTheHost(List<Fleet> alreadyHave)
		{
			List<int> ids = new List<int>();

			for (int i = 0; i < orderedFleets.Count; i++)
			{
				NoteShipsOf(orderedFleets[i], ids);
			}

			for (int i = 0; i < unitsWithoutFleet.Count; i++)
			{
				Unit ship = unitsWithoutFleet[i];

				if (ship != null && !ship.IsDestroyed)
				{
					ids.Add(ship.UniqueId);
				}
			}

			FleetOrderIntents.NoteLooseUnits(ids);

			// Cleared so a second order does not carry this selection's ships
			// as well as its own. The fleets stay: they are what the UI is
			// built on, and they were never sent.
			unitsWithoutFleet.Clear();
		}

		private static void NoteShipsOf(Fleet fleet, List<int> into)
		{
			if (fleet == null)
			{
				return;
			}

			foreach (UnitComponentHolder holder in fleet.Ships)
			{
				Unit ship = holder == null ? null : holder.GetComponent<Unit>();

				if (ship != null && !ship.IsDestroyed)
				{
					into.Add(ship.UniqueId);
				}
			}
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
