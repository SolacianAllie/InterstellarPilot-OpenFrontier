using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Faction_SetNeutralityWith : EngineAction
	{
		public Neutrality Neutrality;

		public bool PermanentPeace;

		public bool PermanentWar;

		public Faction SourceFaction;

		public List<Faction> TargetFactions;

		public bool TwoWay = true;

		public override ActionType Type => ActionType.Faction_SetNeutralityWith;

		public override void Execute()
		{
			base.Execute();
			if (!(SourceFaction != null) || TargetFactions == null)
			{
				return;
			}
			for (int i = 0; i < TargetFactions.Count; i++)
			{
				Faction faction = TargetFactions[i];
				if (faction != null)
				{
					FactionAttitude orCreateAttitude = SourceFaction.GetOrCreateAttitude(faction);
					orCreateAttitude.Neutrality = Neutrality;
					orCreateAttitude.PermanentPeace = PermanentPeace;
					orCreateAttitude.RestrictHostilityTimeout = PermanentWar;
					if (TwoWay)
					{
						FactionAttitude orCreateAttitude2 = faction.GetOrCreateAttitude(SourceFaction);
						orCreateAttitude2.Neutrality = Neutrality;
						orCreateAttitude2.PermanentPeace = PermanentPeace;
						orCreateAttitude2.RestrictHostilityTimeout = PermanentWar;
					}
				}
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteFactionId(SourceFaction);
			writer.Write(TargetFactions.Count);
			for (int i = 0; i < TargetFactions.Count; i++)
			{
				writer.WriteFactionId(TargetFactions[i]);
			}
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			SourceFaction = reader.ReadFactionFromId(engine);
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				Faction faction = reader.ReadFactionFromId(engine);
				if (faction != null)
				{
					TargetFactions.Add(faction);
				}
			}
		}
	}
}
