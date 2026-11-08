using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens
{
	public static class PlayerMessagesHelper
	{
		public static List<PlayerActiveMessage> GetDisplayableMessages()
		{
			List<PlayerActiveMessage> list = new List<PlayerActiveMessage>();
			if (EngineASX.Instance.LocalPlayer != null)
			{
				foreach (PlayerActiveMessage message in EngineASX.Instance.LocalPlayer.Messages)
				{
					list.Add(message);
				}
			}
			return list;
		}

		public static bool HasAnyMessages()
		{
			if (EngineASX.Instance.LocalPlayer != null)
			{
				return EngineASX.Instance.LocalPlayer.Messages.Any();
			}
			return false;
		}
	}
}
