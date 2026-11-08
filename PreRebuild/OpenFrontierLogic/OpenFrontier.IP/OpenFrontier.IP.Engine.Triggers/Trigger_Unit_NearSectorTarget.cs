using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Unit_NearSectorTarget : TriggerBase
	{
		public float Distance = 200f;

		public Unit Unit1;

		public Unit Unit2;

		public SectorTarget SectorTarget;

		public override TriggerType Type => TriggerType.Unit_NearSectorTarget;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(Unit1);
			writer.WriteUnitId(Unit2);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Unit1 = reader.ReadUnitFromId(engine);
			Unit2 = reader.ReadUnitFromId(engine);
		}

		protected override bool evaluate(EngineASX engine)
		{
			if ((bool)Unit1 && (bool)Unit2 && Unit1.Sector == Unit2.Sector)
			{
				return Vector3.Distance(Unit1.transform.position, Unit2.transform.position) < Distance;
			}
			return false;
		}
	}
}
