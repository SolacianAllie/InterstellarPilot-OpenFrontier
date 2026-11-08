using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Player_DockedAtUnit : TriggerBase
	{
		public Unit Target;

		public override TriggerType Type => TriggerType.Player_DockedAtUnit;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(Target);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Target = reader.ReadUnitFromId(engine);
		}

		protected override bool evaluate(EngineASX engine)
		{
			if (Target != null && !Target.IsDestroyed && engine.LocalPlayer != null)
			{
				Unit currentUnit = engine.LocalPlayer.Person.CurrentUnit;
				if (currentUnit == Target || (currentUnit.IsDocked && currentUnit.Components.DockUnit == Target))
				{
					return true;
				}
			}
			return false;
		}
	}
}
