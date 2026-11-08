using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core
{
	public class NeutralitySyncer
	{
		private int currentFactionRelationIndex = -1;

		public void SyncFor(Faction faction)
		{
			if (faction.Relations.Count == 0)
			{
				return;
			}
			int num = Mathf.Min(faction.Relations.Count, 15);
			for (int i = 0; i < num; i++)
			{
				currentFactionRelationIndex++;
				if (currentFactionRelationIndex >= faction.Relations.Count)
				{
					currentFactionRelationIndex = 0;
				}
				FactionAttitude factionAttitude = faction.Relations[currentFactionRelationIndex];
				if (factionAttitude != null && factionAttitude.TargetFaction != null)
				{
					faction.SyncNeutrality(factionAttitude.TargetFaction);
				}
			}
		}
	}
}
