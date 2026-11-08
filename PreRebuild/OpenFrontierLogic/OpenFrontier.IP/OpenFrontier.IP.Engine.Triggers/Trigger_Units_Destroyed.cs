using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class Trigger_Units_Destroyed : TriggerBase
	{
		public int MinCount = -1;

		public List<Unit> Units = new List<Unit>();

		public override TriggerType Type => TriggerType.Units_Destroyed;

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.Write(Units.Count);
			for (int i = 0; i < Units.Count; i++)
			{
				writer.WriteUnitId(Units[i]);
			}
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Units.Clear();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				Units.Add(reader.ReadUnitFromId(engine));
			}
		}

		protected override bool evaluate(EngineASX engine)
		{
			int num = 0;
			int num2 = ((MinCount > -1) ? MinCount : Units.Count);
			for (int i = 0; i < Units.Count; i++)
			{
				if (Units[i] == null || Units[i].IsDestroyed)
				{
					num++;
				}
			}
			return num >= num2;
		}
	}
}
