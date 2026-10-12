using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class QuickMsgUI : MonoBehaviour
	{
		public Image Bg;

		private float defaultBgAlpha;

		private float defaultLabelAlpha;

		private bool hasSetDefaults;

		public TextMeshProUGUI Label;

		public HudQuickMsg.QuickMsg Message;

		public void Refresh()
		{
			Label.text = Message.Text;
		}

		public void SetAlpha(float alpha)
		{
			if (!hasSetDefaults)
			{
				SetDefaults();
			}
			Label.SetAlpha(defaultLabelAlpha * alpha);
			Bg.SetAlpha(defaultBgAlpha * alpha);
		}

		private void Awake()
		{
			SetDefaults();
		}

		private void SetDefaults()
		{
			hasSetDefaults = true;
			defaultLabelAlpha = Label.GetAlpha();
			defaultBgAlpha = Bg.GetAlpha();
		}

		private void OnClick()
		{
			if (Message != null && Message.DockMenuRequestData != null && Message.DockMenuRequestData.RelatedObject != null)
			{
				_ = EngineASX.Instance.GameSettings.QuickMsgLinksEnabled;
			}
		}
	}
}
