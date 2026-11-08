using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FactionOnFactionIntelSeeder : MonoBehaviour
	{
		public List<FactionType> ExcludeFactionTypes = new List<FactionType>();

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding faction intel of other factions", this, 1);
			}
			foreach (Faction faction in world.Engine.Factions)
			{
				if (!(faction.HomeSector != null) || !faction.IsAIFactionType)
				{
					continue;
				}
				foreach (Faction faction2 in world.Engine.Factions)
				{
					if (ShouldFactionKnowOfOtherFaction(faction, faction2))
					{
						EngineASX.Instance.DebugInfo.Seeding_NumFactionsSeededDiscoveryOfOtherFaction++;
						faction.CreateAttitudeIfNone(faction2);
					}
				}
			}
		}

		public bool ShouldFactionKnowOfOtherFaction(Faction f1, Faction f2)
		{
			if (f1 == f2)
			{
				return false;
			}
			if (f2.HomeSector == null)
			{
				return false;
			}
			if (f1.HomeSector.ControllingFaction == f2)
			{
				return true;
			}
			float num = 1f;
			if (f1.IsCivilianFromFactionType && f2.IsBanditOrOutlaw())
			{
				num *= 1.8f;
			}
			if (f2.HomeSector.ControllingFaction == f1)
			{
				if (f2.IsCivilianFromFactionType)
				{
					return true;
				}
				if (!f2.IsMinor)
				{
					return Random.value < 0.9f * num;
				}
				return Random.value < 0.5f * num;
			}
			if (f2.HomeSector == f1.HomeSector)
			{
				if (!f2.IsMinor)
				{
					return true;
				}
				return Random.value < 0.5f * num;
			}
			int jumpDistanceTo = f1.HomeSector.GetJumpDistanceTo(f2.HomeSector);
			if (jumpDistanceTo < 4)
			{
				return Random.value < 0.5f * num - (float)jumpDistanceTo * 0.1f;
			}
			return false;
		}
	}
}
