using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class DestroyedUnitInfo
	{
		public Faction AttackerFaction;

		public Unit Attacker;

		public UnitClass UnitClass;

		public double TimeOfDestruction;

		public Sector Sector;

		public Vector3 SectorPosition;

		public DestroyedUnitInfo(UnitClass unitClass, Sector sector, Vector3 sectorPosition, Unit attacker, Faction attackerFaction, double timeOfDestruction)
		{
			UnitClass = unitClass;
			Sector = sector;
			SectorPosition = sectorPosition;
			Attacker = attacker;
			AttackerFaction = attackerFaction;
			TimeOfDestruction = timeOfDestruction;
		}
	}
}
