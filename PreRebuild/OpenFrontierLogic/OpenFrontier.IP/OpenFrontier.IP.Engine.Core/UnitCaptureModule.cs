using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class UnitCaptureModule
	{
		private Queue<PossibleUnitToCapture> captureQueue = new Queue<PossibleUnitToCapture>(10);

		public void Update()
		{
			if (captureQueue.Count <= 0)
			{
				return;
			}
			PossibleUnitToCapture possibleUnitToCapture = captureQueue.Dequeue();
			if (CanUnitBeCapturedBy(possibleUnitToCapture.Unit, possibleUnitToCapture.CapturingFaction))
			{
				float num = possibleUnitToCapture.DamageDealt;
				if (num > 100f)
				{
					num = 100f;
				}
				if (Random.value < num * possibleUnitToCapture.Unit.UnitClass.CaptureLikelihood * GameController.Instance.GameSettings.GameplaySettings.CaptureProbabilityMultiplier && possibleUnitToCapture.CapturingFaction.RequestCapture(possibleUnitToCapture.Unit))
				{
					possibleUnitToCapture.CapturingFaction.CaptureUnit(possibleUnitToCapture.Unit);
				}
			}
		}

		public void ConsiderForCapture(Unit unit, Faction sourceFaction, float baseDamage)
		{
			if (!(unit.Components == null) && !(unit.Components.CaptureCooldownTime > EngineASX.Instance.ScenarioElapsedTime))
			{
				captureQueue.Enqueue(new PossibleUnitToCapture
				{
					CapturingFaction = sourceFaction,
					DamageDealt = baseDamage,
					Unit = unit
				});
			}
		}

		public static bool CanUnitBeCapturedBy(Unit unit, Faction faction)
		{
			if (!EngineASX.Instance.World.ScenarioOptions.AllowStationCapture)
			{
				return false;
			}
			if (unit == null || unit.IsDestroyed)
			{
				return false;
			}
			if (unit.IsMinorStation())
			{
				return false;
			}
			if (unit.IsUnderConstructionOrDismantling)
			{
				return false;
			}
			if (unit.Destructable.HealthNormalized > 0.25f)
			{
				return false;
			}
			if (unit.UnitType != UnitType.Station)
			{
				return false;
			}
			if (faction == null)
			{
				return false;
			}
			if (unit.Faction == faction)
			{
				return false;
			}
			if (unit.UnitClass.CaptureLikelihood <= 0f)
			{
				return false;
			}
			return true;
		}
	}
}
