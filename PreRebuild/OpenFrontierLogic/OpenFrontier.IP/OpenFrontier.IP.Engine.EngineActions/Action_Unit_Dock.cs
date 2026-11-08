using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Unit_Dock : EngineAction
	{
		public Unit Dock;

		public List<Unit> Units = new List<Unit>();

		public override ActionType Type => ActionType.Unit_Dock;

		public override void Execute()
		{
			base.Execute();
			if (!(Dock != null))
			{
				return;
			}
			for (int i = 0; i < Units.Count; i++)
			{
				Unit unit = Units[i];
				if (unit != null && unit.Components != null)
				{
					unit.Components.TryDockInUnit(Dock);
				}
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(Dock);
			writer.Write(Units.Count);
			for (int i = 0; i < Units.Count; i++)
			{
				writer.WriteUnitId(Units[i]);
			}
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Dock = reader.ReadUnitFromId(engine);
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				Unit unit = reader.ReadUnitFromId(engine);
				if (unit != null)
				{
					Units.Add(unit);
				}
			}
		}
	}
}
