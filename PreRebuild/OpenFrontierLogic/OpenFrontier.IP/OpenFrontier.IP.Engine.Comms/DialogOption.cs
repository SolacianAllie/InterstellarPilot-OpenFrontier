using System.IO;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms
{
	public class DialogOption : MonoBehaviour, ICommsStageOption
	{
		public int CreditsCost;

		private DialogStage dialogStage;

		public DialogStage TargetDialogStage;

		public string Text;

		public int TimesEntered;

		public int UniqueId = -1;

		public DialogStage DialogStage => dialogStage;

		public DialogBase Dialog => dialogStage.Dialog;

		public string OptionText
		{
			get
			{
				if (CreditsCost > 0)
				{
					return $"{Text} [Pay {TextFormattingHelper.FormatCredits(CreditsCost)} Credits]";
				}
				return Text;
			}
		}

		public int NumberTimesSelected
		{
			get
			{
				return TimesEntered;
			}
			set
			{
				TimesEntered = value;
			}
		}

		public ICommsStageOptionGroup OptionGroup { get; set; }

		public virtual void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			writer.Write(TimesEntered);
		}

		public virtual void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			TimesEntered = reader.ReadInt32();
		}

		private void Awake()
		{
			dialogStage = UnityObjectHelper.FindInParentsOrSelf<DialogStage>(gameObject);
		}

		public void Select(ICommsController commsController)
		{
			bool flag = false;
			if (CreditsCost > 0 && EngineASX.Instance.LocalPlayer.Credits < CreditsCost)
			{
				flag = true;
				UIController.Instance.ShowMessageBox("Insufficient credits", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			if (!flag)
			{
				TimesEntered++;
				BroadcastMessage("Execute", SendMessageOptions.DontRequireReceiver);
				if (CreditsCost > 0)
				{
					EngineASX.Instance.AddCreditsToPlayerFactionWithMsg(-CreditsCost);
				}
				commsController.ChangeStage(TargetDialogStage);
			}
		}
	}
}
