using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Faction_SetRpMultiplier : EngineAction
	{
		public Faction Faction;

		public float RpMultiplier = 1f;

		public override ActionType Type => ActionType.Faction_SetRpMultiplier;

		public override void Execute()
		{
			base.Execute();
			if (Faction != null)
			{
				Faction.RequisitionPointMultiplier = RpMultiplier;
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteFactionId(Faction);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Faction = reader.ReadFactionFromId(engine);
		}
	}
}
