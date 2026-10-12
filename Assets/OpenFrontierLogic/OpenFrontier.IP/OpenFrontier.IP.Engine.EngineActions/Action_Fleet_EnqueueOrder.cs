using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Fleet_EnqueueOrder : EngineAction
	{
		[FormerlySerializedAs("Group")]
		public Fleet Fleet;

		public FleetOrder Objective;

		public override ActionType Type => ActionType.Fleet_EnqueueOrder;

		public override void Execute()
		{
			base.Execute();
			if (Fleet != null && Objective != null)
			{
				Fleet.EnqueueOrder(Objective);
			}
			else
			{
				Debug.LogWarning($"{this} Cannot Add objective because group or target is null. Group: {Fleet} Objective {Objective}", this);
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteAIGroupId(Fleet);
			writer.WriteAIObjectiveId(Objective);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Fleet = reader.ReadFleetFromId(engine);
			Objective = reader.ReadAIObjectiveFromId(engine);
		}
	}
}
