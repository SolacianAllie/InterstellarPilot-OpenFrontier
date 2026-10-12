using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_LegacyPlayerWithinDistOfUnit : TriggerBase
	{
		public float Distance = 200f;

		public Unit TargetUnit;

		public override TriggerType Type => TriggerType.Player_NearUnit;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(TargetUnit);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			TargetUnit = reader.ReadUnitFromId(engine);
		}

		protected override bool evaluate(EngineASX engine)
		{
			Unit playerUnit = engine.PlayerUnit;
			if ((bool)playerUnit && (bool)TargetUnit)
			{
				return Vector3.Distance(playerUnit.transform.position, TargetUnit.transform.position) < Distance;
			}
			return false;
		}
	}
}
