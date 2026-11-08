using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.CargoTrade;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class MessagesUI : EngineScreen
	{
		public enum SortMode
		{
			FromText,
			Timestamp
		}

		public enum TraderFilterCargoType
		{
			All,
			Cargo,
			Equipment
		}

		public class MessageComparer : IComparer<PlayerActiveMessage>
		{
			public SortMode Mode = SortMode.Timestamp;

			public int SortDirection = 1;

			public int Compare(PlayerActiveMessage x, PlayerActiveMessage y)
			{
				switch (Mode)
				{
				case SortMode.FromText:
				{
					string fromText = x.GetFromText();
					string fromText2 = y.GetFromText();
					return string.Compare(fromText, fromText2);
				}
				case SortMode.Timestamp:
					return y.EngineTimeStamp.CompareTo(x.EngineTimeStamp);
				default:
					return 0;
				}
			}
		}

		public Button MarkAllAsReadButton;

		public Button DeleteUnimportantButton;

		private Dictionary<SortMode, TradeMenuColumnHeader> columnHeaders = new Dictionary<SortMode, TradeMenuColumnHeader>();

		public MessageComparer ItemComparer;

		public MessageListUI ItemList;

		private GamePlayer localPlayer;

		public GamePlayer LocalPlayer
		{
			get
			{
				return localPlayer;
			}
			set
			{
				if (localPlayer != value)
				{
					if (localPlayer != null)
					{
						localPlayer.MessagedAddRemoved -= LocalPlayer_MessagedAddRemoved;
					}
					localPlayer = value;
					if (localPlayer != null)
					{
						Eng.LocalPlayer.MessagedAddRemoved += LocalPlayer_MessagedAddRemoved;
					}
				}
			}
		}

		public List<PlayerActiveMessage> ActiveItems => ItemList.ActiveItems;

		public void RegisterColumnHeader(SortMode sortMode, TradeMenuColumnHeader t)
		{
			columnHeaders[sortMode] = t;
		}

		public void SortItems(SortMode sortMode)
		{
			if (ItemComparer.Mode != sortMode)
			{
				ItemComparer.Mode = sortMode;
			}
			else
			{
				ItemComparer.SortDirection *= -1;
			}
			Refresh();
		}

		public PlayerActiveMessage SwitchItem(PlayerActiveMessage curCargoClass, int movement)
		{
			int num = ActiveItems.IndexOf(curCargoClass);
			num += movement;
			num = Maths.WrapValue(num, 0, ActiveItems.Count);
			if (num >= 0 && num < ActiveItems.Count)
			{
				return ActiveItems[num];
			}
			return null;
		}

		protected override void awake()
		{
			ItemComparer = new MessageComparer();
			base.awake();
			DeleteUnimportantButton.onClick.AddListener(DeleteUnimportantButtonClick);
			MarkAllAsReadButton.onClick.AddListener(MarkAllAsReadButtonClick);
		}

		private void MarkAllAsReadButtonClick()
		{
			if (Eng.LocalPlayer != null)
			{
				PlayerActiveMessage[] array = Eng.LocalPlayer.Messages.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Opened = true;
				}
				Refresh();
			}
		}

		private void DeleteUnimportantButtonClick()
		{
			if (!(Eng.LocalPlayer != null))
			{
				return;
			}
			PlayerActiveMessage[] array = Eng.LocalPlayer.Messages.ToArray();
			foreach (PlayerActiveMessage playerActiveMessage in array)
			{
				if (playerActiveMessage.AllowDelete)
				{
					Eng.LocalPlayer.RemoveMessage(playerActiveMessage);
				}
			}
			RefreshIfAnyMessagesElseNavigateBack();
		}

		private void RefreshIfAnyMessagesElseNavigateBack()
		{
			if (PlayerMessagesHelper.HasAnyMessages())
			{
				Refresh();
			}
			else
			{
				NavigateBack();
			}
		}

		protected override void update()
		{
			base.update();
			if (Eng.LocalPlayer != null)
			{
				MarkAllAsReadButton.interactable = Eng.LocalPlayer.Messages.Count((PlayerActiveMessage e) => !e.Opened) > 0;
				DeleteUnimportantButton.interactable = Eng.LocalPlayer.Messages.Count((PlayerActiveMessage e) => e.AllowDelete) > 0;
			}
		}

		protected override void start()
		{
			base.start();
			if (columnHeaders.ContainsKey(ItemComparer.Mode))
			{
				columnHeaders[ItemComparer.Mode].PositionFilterGraphic();
			}
			LocalPlayer = Eng.LocalPlayer;
		}

		protected override void onEnable()
		{
			base.onEnable();
			LocalPlayer = Eng.LocalPlayer;
		}

		protected override void onDisable()
		{
			base.onDisable();
			LocalPlayer = null;
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			LocalPlayer = null;
		}

		protected override void refresh()
		{
			base.refresh();
			if (DockUI != null)
			{
				ItemList.Refresh();
			}
		}

		private void LocalPlayer_MessagedAddRemoved(GamePlayer sender, PlayerActiveMessage m, bool added)
		{
			ItemList.Refresh();
		}
	}
}
