using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Hud_SetTarget : EngineAction
	{
		public Unit Unit;

		public override ActionType Type => ActionType.Hud_SetTarget;

		public override void Execute()
		{
			base.Execute();
			engine.Hud.CurrentTarget = Unit;
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(Unit);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Unit = reader.ReadUnitFromId(engine);
		}
	}
}
