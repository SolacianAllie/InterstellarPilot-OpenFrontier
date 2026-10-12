using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Player_DiscoverStaticUnitsInSectors : EngineAction
	{
		public bool AllScenes;

		[FormerlySerializedAs("Scenes")]
		public List<Sector> Sectors = new List<Sector>();

		public override ActionType Type => ActionType.Player_DiscoverStaticUnitsInSectors;

		public override void Execute()
		{
			base.Execute();
			GamePlayer localPlayer = engine.LocalPlayer;
			if (!(localPlayer != null))
			{
				return;
			}
			if (AllScenes)
			{
				localPlayer.Faction.Intel.DiscoverAllUnits(staticOnly: true);
				return;
			}
			for (int i = 0; i < Sectors.Count; i++)
			{
				Sector sector = Sectors[i];
				if (sector != null)
				{
					localPlayer.Faction.Intel.DiscoverUnitsInSector(sector);
				}
			}
		}
	}
}
