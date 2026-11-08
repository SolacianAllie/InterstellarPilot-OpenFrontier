using System.IO;
using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Player_NearSectorTarget : TriggerBase
	{
		public float Distance = 200f;

		public SectorTarget Target;

		public override TriggerType Type => TriggerType.Player_NearSectorTarget;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.Write(Target != null);
			if (Target != null)
			{
				Target.WriteBinary(writer, engine);
			}
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			if (reader.ReadBoolean())
			{
				Target = new SectorTarget();
				Target.ReadBinary(reader, engine);
			}
		}

		protected override bool evaluate(EngineASX engine)
		{
			Unit playerUnit = engine.PlayerUnit;
			if ((bool)playerUnit && Target != null && Target.IsValid() && playerUnit.Sector == Target.GetTargetSector())
			{
				return Vector3.Distance(playerUnit.SectorPosition, Target.GetTargetSectorPosition()) < Distance;
			}
			return false;
		}
	}
}
