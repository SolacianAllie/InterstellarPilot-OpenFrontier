using System;
using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	[Serializable]
	public abstract class FactionAttitudeSetup
	{
		public float HostilityDuration;

		public Neutrality Neutrality;

		public float Opinion = 0.5f;

		public bool PermanentPeace;

		public bool PermanentWar;

		public void Apply(EngineASX engine, Faction sourceFaction, bool forceTwoWay)
		{
			if (engine == null)
			{
				throw new NullReferenceException("engine");
			}
			apply(engine, sourceFaction, forceTwoWay);
		}

		public void ApplyToFaction(EngineASX engine, Faction sourceFaction, Faction targetFaction)
		{
			ApplyToFaction(engine, sourceFaction, targetFaction, forceTwoWay: false);
		}

		public void ApplyToFaction(EngineASX engine, Faction sourceFaction, Faction targetFaction, bool forceTwoWay)
		{
			sourceFaction = engine.GetFactionByid(sourceFaction.UniqueId);
			targetFaction = engine.GetFactionByid(targetFaction.UniqueId);
			if (targetFaction != null)
			{
				if (sourceFaction != targetFaction)
				{
					if (Neutrality == Neutrality.Hostile && PermanentPeace)
					{
						Debug.LogWarning($"Invalid FactionAttitude. Marked as Hostile & PermaPeace. SourceFaction: {sourceFaction} TargetFaction: {targetFaction}");
					}
					if (Neutrality == Neutrality.Allied && PermanentWar)
					{
						Debug.LogWarning($"Invalid FactionAttitude. Marked as Allied & PermaPeace. SourceFaction: {sourceFaction} TargetFaction: {targetFaction}");
					}
					if (sourceFaction == targetFaction)
					{
						Debug.LogWarning("FactionSetup: Source and Target are the same: " + sourceFaction.ToString());
						return;
					}
					applyToFaction(engine, sourceFaction, targetFaction);
					if ((Neutrality != Neutrality.Neutral) | forceTwoWay)
					{
						applyToFaction(engine, targetFaction, sourceFaction);
					}
				}
				else
				{
					Debug.LogWarning($"Cannot apply attitude setup for target faction \"{targetFaction}\". Source and target factions are the same", sourceFaction);
				}
			}
			else
			{
				Debug.Log($"FactionAttitudeSetup cannot apply an attitude for target faction {targetFaction}. It does not exist in the scenario", sourceFaction);
			}
		}

		protected abstract void apply(EngineASX engine, Faction sourceFaction, bool forceTwoWay);

		private void applyToFaction(EngineASX engine, Faction sourceFaction, Faction targetFaction)
		{
			FactionAttitude factionAttitude = sourceFaction.CreateOrSetAttitude(targetFaction, Opinion, Neutrality);
			factionAttitude.SetOpinionAndRecordTimeOfChange(Opinion, sourceFaction);
			factionAttitude.Neutrality = Neutrality;
			factionAttitude.PermanentPeace = PermanentPeace;
			factionAttitude.RestrictHostilityTimeout = PermanentWar;
			if (Neutrality == Neutrality.Hostile)
			{
				factionAttitude.HostilityEndTime = engine.ScenarioElapsedTime + (double)HostilityDuration;
			}
		}
	}
}
