using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Fleet_Activate : EngineAction
	{
		public bool Active = true;

		public Fleet Target;

		public override ActionType Type => ActionType.Fleet_Activate;

		public override void Execute()
		{
			base.Execute();
			if (Target != null)
			{
				Target.gameObject.SetActive(value: true);
			}
			else
			{
				Debug.LogError($"{this}: No target is defined", this);
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.Write(Active);
			writer.WriteAIGroupId(Target);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Active = reader.ReadBoolean();
			Target = reader.ReadFleetFromId(engine);
		}
	}
}
