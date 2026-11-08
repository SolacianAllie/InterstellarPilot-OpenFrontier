using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public class DynamicCommsStage : ICommsStage
	{
		protected static List<ICommsStageOption> stageOptionCache = new List<ICommsStageOption>();

		private DynamicCommsHandler commsHandler;

		public DynamicCommsHandler DynamicCommsHandler
		{
			get
			{
				return commsHandler;
			}
			set
			{
				commsHandler = value;
			}
		}

		public ICommsHandler Handler => commsHandler;

		public ICommsMessage Message { get; set; }

		protected void AddNevermindOption()
		{
			CommsStageEventOption commsStageEventOption = new CommsStageEventOption
			{
				OptionText = CommsHelper.DefaultDismissText
			};
			commsStageEventOption.Selected += NevermindOption_Selected;
			stageOptionCache.Add(commsStageEventOption);
		}

		private void NevermindOption_Selected(CommsStageEventOption option, ICommsController commsController)
		{
			commsController.ChangeStage(commsController.CommsHandler.GetStage());
		}

		public void Finish(ICommsController commsController)
		{
			commsController.Dismiss();
		}

		public virtual IEnumerable<ICommsStageOption> GetOptions()
		{
			return stageOptionCache.ToArray();
		}

		public void SetVisible(bool visible)
		{
		}

		public virtual void Show(ICommsController commsController)
		{
		}
	}
}
