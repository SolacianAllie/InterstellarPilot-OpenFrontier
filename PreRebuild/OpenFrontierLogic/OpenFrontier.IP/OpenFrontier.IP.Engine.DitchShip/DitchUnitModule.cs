using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;

namespace OpenFrontier.IP.Engine.DitchShip
{
	public class DitchUnitModule
	{
		private struct PossibleUnitToDitch
		{
			public Unit Unit;

			public Faction DamageSourceFaction;

			public Unit DamageSourceUnit;

			public DamageDirectType DamageDirectType;

			public PossibleUnitToDitch(Unit unit, Faction damageSourceFaction, Unit damageSourceUnit, DamageDirectType damageDirectType)
			{
				this = default;
				DamageSourceFaction = damageSourceFaction;
				DamageSourceUnit = damageSourceUnit;
				Unit = unit;
				DamageDirectType = damageDirectType;
			}
		}

		private Queue<PossibleUnitToDitch> queue = new Queue<PossibleUnitToDitch>(10);

		private DitchUnitSettings settings;

		public DitchUnitModule()
		{
			settings = GameController.Instance.GameSettings.DitchUnitSettings;
		}

		public bool CouldDitch(Unit unit)
		{
			if (!EngineASX.Instance.World.ScenarioOptions.AllowAbandonShip)
			{
				return false;
			}
			if (unit == null || unit.Destructable == null || unit.Destructable.HealthNormalized > settings.MaxHullThreshold)
			{
				return false;
			}
			if (unit.UnitType != UnitType.Ship)
			{
				return false;
			}
			if (unit.Faction == null)
			{
				return false;
			}
			Person pilot = unit.GetPilot();
			if (pilot != null && pilot.Faction != null)
			{
				return pilot.NpcPilot != null;
			}
			return false;
		}

		public void ConsiderForDitching(Unit unit, Faction damageSourceFaction, Unit damageSourceUnit, DamageDirectType damageDirectType)
		{
			queue.Enqueue(new PossibleUnitToDitch(unit, damageSourceFaction, damageSourceUnit, damageDirectType));
		}

		public void Update()
		{
			if (queue.Count <= 0)
			{
				return;
			}
			PossibleUnitToDitch possibleUnitToDitch = queue.Dequeue();
			if (!CouldDitch(possibleUnitToDitch.Unit))
			{
				return;
			}
			float num = (settings.MaxHullThreshold - possibleUnitToDitch.Unit.Destructable.HealthNormalized) * settings.ProbabilityMultiplier;
			if (Random.value < num && (!possibleUnitToDitch.Unit.IsOwnedByPlayer || GameController.Instance.GameSettings.DitchUnitSettings.PlayerNpcCanDitchShip))
			{
				Person pilot = possibleUnitToDitch.Unit.GetPilot();
				if (!pilot.IsAutoPilot && pilot.NpcPilot.Settings.AllowDitchShip)
				{
					DitchUnit(possibleUnitToDitch.Unit, possibleUnitToDitch.DamageSourceFaction, possibleUnitToDitch.DamageSourceUnit, possibleUnitToDitch.DamageDirectType);
				}
			}
		}

		public void DitchUnit(Unit unit, Faction damageSourceFaction, Unit damageSourceUnit, DamageDirectType damageDirectType)
		{
			Faction faction = unit.Faction;
			if (Random.value < settings.ProbabilityOfKill)
			{
				DitchAndKill(unit, damageSourceFaction);
			}
			else
			{
				DitchAndAbandon(unit, damageSourceFaction);
			}
			PostDitchInternal(unit, damageSourceFaction, damageSourceUnit, damageDirectType, faction);
		}

		public void DitchUnitWithKill(Unit unit, Faction damageSourceFaction, Unit damageSourceUnit, DamageDirectType damageDirectType)
		{
			Faction faction = unit.Faction;
			DitchAndKill(unit, damageSourceFaction);
			PostDitchInternal(unit, damageSourceFaction, damageSourceUnit, damageDirectType, faction);
		}

		public void DitchUnitWithAbandon(Unit unit, Faction damageSourceFaction, Unit damageSourceUnit, DamageDirectType damageDirectType)
		{
			Faction faction = unit.Faction;
			DitchAndAbandon(unit, damageSourceFaction);
			PostDitchInternal(unit, damageSourceFaction, damageSourceUnit, damageDirectType, faction);
		}

		private static void DitchAndAbandon(Unit unit, Faction damageSourceFaction)
		{
			Person pilot = unit.GetPilot();
			EngineASX.Instance.ProcessBounties(pilot, damageSourceFaction);
			pilot.CurrentUnit = null;
			unit.Faction = null;
			EngineASX.Instance.DebugInfo.NumTimesPilotDitchedShipAndLived++;
		}

		private static void DitchAndKill(Unit unit, Faction damageSourceFaction)
		{
			Person pilot = unit.GetPilot();
			EngineASX.Instance.NotifyPilotAboutToBeKilled(pilot, damageSourceFaction);
			pilot.CurrentUnit = null;
			pilot.TryKill();
			unit.Faction = null;
			EngineASX.Instance.DebugInfo.NumTimesPilotDitchedShipAndKilled++;
		}

		private static void PostDitchInternal(Unit unit, Faction damageSourceFaction, Unit damageSourceUnit, DamageDirectType damageDirectType, Faction ourFaction)
		{
			if (damageSourceUnit != null && ourFaction != null && damageSourceUnit.Faction != ourFaction)
			{
				ourFaction.HandleOwnedUnitKilled(unit, unit.UnitClass, damageSourceUnit, damageSourceFaction, damageDirectType);
				Person pilot = damageSourceUnit.GetPilot();
				if (pilot != null && pilot.Faction == damageSourceFaction)
				{
					pilot.AddKill(unit);
				}
			}
			if (unit.Components != null)
			{
				unit.Components.ClaimCooldownTime = Time.time + 2f;
			}
			EngineASX.Instance.DitchUnitCleanupModule.QueueForCleanup(unit);
		}
	}
}
