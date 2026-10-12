using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;

namespace OpenFrontier.IP.UI
{
	public class MessageListUI : ScrollList<PlayerActiveMessage>
	{
		public MessagesUI MessagesUI;

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<PlayerActiveMessage> displayableMessages = PlayerMessagesHelper.GetDisplayableMessages();
			displayableMessages.Sort(MessagesUI.ItemComparer);
			SetItems(displayableMessages);
		}
	}
}
