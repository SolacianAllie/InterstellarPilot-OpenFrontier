using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Unit_ChangeFleet : EngineAction
	{
		[FormerlySerializedAs("TargetGroup")]
		public Fleet TargetFleet;

		public Unit TargetUnit;

		public override ActionType Type => ActionType.Unit_ChangeFleet;

		public override void Execute()
		{
			base.Execute();
			if (TargetUnit != null && TargetFleet != null)
			{
				NpcPilot npcPilot = TargetUnit.NpcPilot;
				if (npcPilot != null)
				{
					npcPilot.Fleet = TargetFleet;
				}
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(TargetUnit);
			writer.WriteAIGroupId(TargetFleet);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			TargetUnit = reader.ReadUnitFromId(engine);
			TargetFleet = reader.ReadFleetFromId(engine);
		}
	}
}
