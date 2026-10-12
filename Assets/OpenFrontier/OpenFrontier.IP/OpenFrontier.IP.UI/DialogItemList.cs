using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Comms;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class DialogItemList : ScrollList<ICommsStageOption>
	{
		public DialogController DialogController;

		public Color ReadTextColor = Color.grey;

		public Color UnreadTextColor = Color.white;
	}
}
