using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Faction_SetOpinionWith : EngineAction
	{
		public Faction Faction1;

		public Faction Faction2;

		public float Opinion = 1f;

		public bool TwoWay = true;

		public override ActionType Type => ActionType.Faction_SetOpinionWith;

		public override void Execute()
		{
			base.Execute();
			if (Faction1 != null && Faction2 != null)
			{
				Faction1.GetOrCreateAttitude(Faction2).SetOpinionAndRecordTimeOfChange(Opinion, Faction1);
				if (TwoWay)
				{
					Faction2.GetOrCreateAttitude(Faction1).SetOpinionAndRecordTimeOfChange(Opinion, Faction2);
				}
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteFactionId(Faction1);
			writer.WriteFactionId(Faction2);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Faction1 = reader.ReadFactionFromId(engine);
			Faction2 = reader.ReadFactionFromId(engine);
		}
	}
}
