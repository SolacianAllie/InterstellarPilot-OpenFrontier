using System.Collections.Generic;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Dialog
{
	public class DialogEventHandler : MonoBehaviour, IDialogEventHandler
	{
		public DialogEvent Event;

		public List<string> Messages;

		public string GetMessage(DialogRequestArguments? dialogRequestArguments)
		{
			return Messages.GetRandom();
		}
	}
}
