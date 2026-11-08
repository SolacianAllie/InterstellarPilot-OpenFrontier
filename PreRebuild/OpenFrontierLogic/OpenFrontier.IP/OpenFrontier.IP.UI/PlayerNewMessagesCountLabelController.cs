using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class PlayerNewMessagesCountLabelController : MonoBehaviour
	{
		public TextMeshProUGUI Label;

		public bool IncludeParenthesis = true;

		private int lastUnreadMessageCount = -1;

		private void Awake()
		{
			Label.enabled = false;
			RefreshLabel();
		}

		private void Update()
		{
			RefreshLabel();
		}

		private void OnDisable()
		{
			lastUnreadMessageCount = -1;
			Label.enabled = false;
		}

		private void RefreshLabel()
		{
			int num = 0;
			EngineASX instance = EngineASX.Instance;
			if (instance != null && instance.LocalPlayer != null)
			{
				num = instance.LocalPlayer.UnreadMessageCount;
			}
			if (num != lastUnreadMessageCount)
			{
				Label.enabled = num > 0;
				if (num > 0)
				{
					Label.text = (IncludeParenthesis ? $"({num})" : num.ToString());
				}
				lastUnreadMessageCount = num;
			}
		}
	}
}
