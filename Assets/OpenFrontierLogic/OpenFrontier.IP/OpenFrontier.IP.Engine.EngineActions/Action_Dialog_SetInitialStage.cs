using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.SaveGame;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Dialog_SetInitialStage : EngineAction
	{
		public DialogBase Dialog;

		public DialogStage EntryStage;

		public override ActionType Type => ActionType.Dialog_SetInitialStage;

		public override void Execute()
		{
			base.Execute();
			if (Dialog != null)
			{
				Dialog.EntryDialogStage = EntryStage;
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteDialogId(Dialog);
			writer.WriteDialogStageId(EntryStage);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			Dialog = reader.ReadDialogFromId(engine);
			EntryStage = reader.ReadDialogStageFromId(engine);
		}
	}
}
