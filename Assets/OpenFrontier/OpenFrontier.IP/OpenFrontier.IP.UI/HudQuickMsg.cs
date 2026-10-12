using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class HudQuickMsg : MonoBehaviour
	{
		public enum ShowMessageState
		{
			None,
			FadeIn,
			Show,
			FadeOut
		}

		public class QuickMsg
		{
			public DocKMenuRequestData DockMenuRequestData;

			public string Text;
		}

		private struct DelayedMsg
		{
			public QuickMsg Message;

			public float ShowTime;
		}

		private struct MessageInfo
		{
			public ShowMessageState State;

			public string Message;

			public float CurExpiryTime;

			public QuickMsgUI ItemUI;
		}

		public Vector3 DefaultLabelPosition = Vector3.zero;

		private List<DelayedMsg> delayedMsgs = new List<DelayedMsg>();

		public float FadeInTime;

		public float FadeOutTime;

		private List<QuickMsgUI> itemPool = new List<QuickMsgUI>();

		private float lastRealTime;

		public int MaxQueuedMessages = 5;

		public QuickMsgUI[] MessageItems;

		public float MessageMoveRate = 10f;

		private List<QuickMsg> messageQueue = new List<QuickMsg>();

		public float MessageSpacing = 35f;

		private List<MessageInfo> shownMessages = new List<MessageInfo>();

		public float ShowTime = 3f;

		public float ShowTimePerCharacter = 0.05f;

		public int MaxShownMessages => MessageItems.Length;

		public int ShownMessageCount => shownMessages.Count;

		public int QueuedMessageCount => messageQueue.Count;

		public void ClearMessages()
		{
			shownMessages.Clear();
			SetLabelsActive(active: false);
			messageQueue.Clear();
			FillPool();
		}

		public void AddChangeInCreditsMessage(int changeInCredits)
		{
			if (changeInCredits != 0)
			{
				AddMessage(string.Format("{0} Credit{1} {2}", TextFormattingHelper.FormatCredits(Mathf.Abs(changeInCredits)), (changeInCredits != 1) ? "s" : string.Empty, (changeInCredits > 0) ? "Added" : "Removed"));
			}
		}

		public void AddInsufficientCreditsMessage()
		{
			AddMessage("Insufficient Credits");
		}

		public void AddMessage(string msg, float waitTime = 0f, DocKMenuRequestData dockMenuRequestData = null)
		{
			QuickMsg quickMsg = new QuickMsg
			{
				Text = msg,
				DockMenuRequestData = dockMenuRequestData
			};
			if (waitTime > 0f)
			{
				DelayedMsg item = new DelayedMsg
				{
					Message = quickMsg,
					ShowTime = Time.realtimeSinceStartup + waitTime
				};
				delayedMsgs.Add(item);
			}
			else if (messageQueue.Count < MaxQueuedMessages)
			{
				AddMessage(quickMsg);
			}
			else
			{
				Debug.LogWarning($"Ignoring message. Queue full: \"{msg}\"", this);
			}
		}

		private void Start()
		{
			FillPool();
			SetLabelsActive(active: false);
		}

		private void FillPool()
		{
			itemPool.Clear();
			QuickMsgUI[] messageItems = MessageItems;
			foreach (QuickMsgUI item in messageItems)
			{
				itemPool.Add(item);
			}
		}

		private void SetLabelsActive(bool active)
		{
			for (int i = 0; i < MaxShownMessages; i++)
			{
				MessageItems[i].gameObject.SetActive(active);
			}
		}

		private Vector3 GetMessageDesiredPosition(int index)
		{
			return DefaultLabelPosition + new Vector3(0f, (0f - MessageSpacing) * (float)index, 0f);
		}

		private void Update()
		{
			_ = Time.realtimeSinceStartup;
			_ = lastRealTime;
			lastRealTime = Time.realtimeSinceStartup;
			for (int i = 0; i < delayedMsgs.Count; i++)
			{
				if (Time.realtimeSinceStartup > delayedMsgs[i].ShowTime)
				{
					AddMessage(delayedMsgs[i].Message);
					delayedMsgs.RemoveAt(i);
				}
			}
			CheckMessageQueue();
			for (int j = 0; j < shownMessages.Count; j++)
			{
				MessageInfo value = shownMessages[j];
				switch (value.State)
				{
				case ShowMessageState.FadeIn:
					if (Time.realtimeSinceStartup > value.CurExpiryTime)
					{
						value.ItemUI.SetAlpha(1f);
						value.CurExpiryTime = Time.realtimeSinceStartup + ShowTime + ShowTimePerCharacter * (float)value.Message.Length;
						value.State = ShowMessageState.Show;
					}
					else
					{
						float num = Time.realtimeSinceStartup - (value.CurExpiryTime - FadeInTime);
						value.ItemUI.SetAlpha(num / FadeInTime);
					}
					break;
				case ShowMessageState.FadeOut:
				{
					if (Time.realtimeSinceStartup > value.CurExpiryTime)
					{
						value.State = ShowMessageState.None;
						break;
					}
					float num2 = Time.realtimeSinceStartup - (value.CurExpiryTime - FadeOutTime);
					value.ItemUI.SetAlpha(1f - num2 / FadeOutTime);
					break;
				}
				case ShowMessageState.Show:
					if (Time.realtimeSinceStartup > value.CurExpiryTime)
					{
						value.CurExpiryTime = Time.realtimeSinceStartup + FadeOutTime;
						value.State = ShowMessageState.FadeOut;
					}
					break;
				}
				if (value.State == ShowMessageState.None)
				{
					value.ItemUI.gameObject.SetActive(value: false);
					itemPool.Add(value.ItemUI);
					shownMessages.RemoveAt(j);
					j--;
				}
				else
				{
					shownMessages[j] = value;
				}
			}
		}

		private void CheckMessageQueue()
		{
			if (messageQueue.Count > 0 && shownMessages.Count < MaxShownMessages)
			{
				QuickMsg quickMsg = messageQueue[0];
				string text = quickMsg.Text;
				_ = shownMessages.Count;
				QuickMsgUI quickMsgUI = itemPool[0];
				itemPool.RemoveAt(0);
				quickMsgUI.gameObject.SetActive(value: true);
				quickMsgUI.Message = quickMsg;
				quickMsgUI.Refresh();
				shownMessages.Add(new MessageInfo
				{
					State = ShowMessageState.FadeIn,
					Message = text,
					CurExpiryTime = Time.realtimeSinceStartup + FadeInTime,
					ItemUI = quickMsgUI
				});
				messageQueue.RemoveAt(0);
			}
		}

		private void AddMessage(QuickMsg msg)
		{
			messageQueue.Add(msg);
		}
	}
}
