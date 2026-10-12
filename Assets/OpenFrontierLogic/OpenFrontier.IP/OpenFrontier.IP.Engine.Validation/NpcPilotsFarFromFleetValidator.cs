using System.Linq;
using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Validation
{
	public static class NpcPilotsFarFromFleetValidator
	{
		public static void Validate()
		{
			foreach (Fleet fleet in EngineASX.Instance.Fleets)
			{
				foreach (UnitComponentHolder ship in fleet.Ships)
				{
					if (ship.Unit == null || !ship.Unit.IsValidAndNotDestroyed)
					{
						continue;
					}
					if (ship.Unit.Sector != fleet.Sector && (fleet.CurState != FleetState.EnteringGate || fleet.WormholeBeingEntered == null || (fleet.WormholeBeingEntered.Sector != ship.Unit.Sector && ship.Unit.Sector != fleet.WormholeBeingEntered.ActualTargetSector)))
					{
						Debug.LogWarning($"Ship is not in the same sector as fleet {fleet}. Fleet sector: {fleet.Sector}. Ship sector: {ship.Unit.Sector} Fleet state: {fleet.CurState}. Fleet gate entered: {fleet.WormholeBeingEntered}. Fleet gate target sector: {((fleet.WormholeBeingEntered != null) ? fleet.WormholeBeingEntered.ActualTargetSector : null)}", ship.Unit);
					}
					else
					{
						if (!(ship.Unit.Sector == fleet.Sector))
						{
							continue;
						}
						float num = Vector3.Distance(ship.Unit.SectorPosition, fleet.SectorPosition);
						if (num > 3000f)
						{
							float num2 = fleet.Ships.Min((UnitComponentHolder e) => Vector3.Distance(ship.Unit.SectorPosition, fleet.SectorPosition));
							float num3 = fleet.Ships.Max((UnitComponentHolder e) => Vector3.Distance(ship.Unit.SectorPosition, fleet.SectorPosition));
							float num4 = fleet.Ships.Average((UnitComponentHolder e) => Vector3.Distance(ship.Unit.SectorPosition, fleet.SectorPosition));
							string text = string.Join(", ", fleet.Ships.Select((UnitComponentHolder e) => e.Unit.UnitClass.GetClassAndSeriesName()));
							Debug.LogWarning($"Ship {ship.Unit} is far away from fleet {fleet}. Distance: {num:N0}. Fleet state: {fleet.CurState} Ship combat mode: {ship.PilotNpc.CombatMode}. Min ship distance from fleet: {num2} Max: {num3} average: {num4}. Ships: {text}", ship.Unit);
						}
					}
				}
			}
		}
	}
}
