using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.Dialog;
using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class DialogEventHandlerLegacy : IDialogEventHandler
	{
		public DialogEvent Event;

		public List<string> Messages;

		public string GetMessage(DialogRequestArguments? dialogRequestArguments)
		{
			return Messages.GetRandom();
		}
	}
}
