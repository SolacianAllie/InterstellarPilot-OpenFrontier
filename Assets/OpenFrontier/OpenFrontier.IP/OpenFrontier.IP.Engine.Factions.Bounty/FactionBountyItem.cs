using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions.Bounty
{
	public class FactionBountyItem
	{
		public Faction Source { get; set; }

		public Person Person { get; set; }

		public int Bounty { get; set; }

		public bool IsValid
		{
			get
			{
				if (Person != null && Person.IsActiveInGame)
				{
					return Source != null;
				}
				return false;
			}
		}

		public Sector LastKnownSector { get; set; }

		public Vector3? LastKnownSectorPosition { get; set; }

		public Unit LastKnownPilottedShip { get; set; }

		public double? TimeOfLastSighting { get; set; }

		public void UpdateLastKnownPilottedShipAndPosition(Unit unit)
		{
			if (unit != null)
			{
				LastKnownSector = unit.Sector;
				UpdateLastKnownPosition(unit.Sector, unit.SectorPosition);
			}
			else
			{
				LastKnownSector = null;
				LastKnownPilottedShip = null;
				LastKnownSectorPosition = null;
			}
		}

		public void UpdateLastKnownPosition(Sector sector, Vector3 position)
		{
			LastKnownSector = sector;
			LastKnownSectorPosition = position;
		}
	}
}
