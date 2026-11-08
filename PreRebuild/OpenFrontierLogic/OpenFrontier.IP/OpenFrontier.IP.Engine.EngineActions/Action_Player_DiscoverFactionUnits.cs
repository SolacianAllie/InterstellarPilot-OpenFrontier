using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Player_DiscoverFactionUnits : EngineAction
	{
		public List<Faction> Factions = new List<Faction>();

		public override ActionType Type => ActionType.Player_DiscoverFactionUnits;

		public override void Execute()
		{
			base.Execute();
			if (!(engine.LocalPlayer != null))
			{
				return;
			}
			for (int i = 0; i < Factions.Count; i++)
			{
				Faction faction = Factions[i];
				if (!(faction != null))
				{
					continue;
				}
				for (int j = 0; j < faction.Units.Count; j++)
				{
					Unit unit = faction.Units[j];
					if (unit != null)
					{
						engine.LocalPlayer.Faction.Intel.DiscoverUnit(unit);
					}
				}
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.Write(Factions.Count);
			for (int i = 0; i < Factions.Count; i++)
			{
				writer.WriteFactionId(Factions[i]);
			}
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				Factions.Add(reader.ReadFactionFromId(engine));
			}
		}
	}
}
