using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class MessageItemUI : ScrollListItem<PlayerActiveMessage>
	{
		public TextMeshProUGUI FromLabel;

		public TextMeshProUGUI SubjectText;

		public TextMeshProUGUI DateLabel;

		public MessagesUI MessagesUI => ((MessageListUI)ParentList).MessagesUI;

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				DateLabel.text = EngineASX.Instance.DateTimeUtils.GetFormattedEngineTimeStampFromRealElapsedSeconds(Item.EngineTimeStamp);
				FromLabel.text = Item.GetFriendlyFromText();
				SubjectText.text = Item.GetFriendlySubjectText();
				SubjectText.fontStyle = ((!Item.Opened) ? FontStyles.Bold : FontStyles.Normal);
				DateLabel.fontStyle = SubjectText.fontStyle;
			}
		}

		private static bool diagged;

		private void LateUpdate()
		{
			// Asked a frame late, on purpose.
			//
			// The first reading of this reported 0 glyphs on all three labels,
			// including the date, which visibly renders - because a text mesh
			// is built later than the moment the string is assigned. Reading
			// it in Refresh measures when the probe ran, not what was drawn.
			//
			// Read here, the count is the number of characters TMP decided to
			// put on screen. If From and Subject come back 0 while Date comes
			// back with a number, then the strings are fine and the truncation
			// is eating them - which is what Ellipsis with word wrapping off
			// does to a line wider than its rect, and which the two failing
			// labels share and the working one does not.
			if (diagged || FromLabel == null || DateLabel == null)
			{
				return;
			}

			diagged = true;

			Debug.Log("DiagMsg: " + Describe(FromLabel) + " | " + Describe(SubjectText)
				+ " | " + Describe(DateLabel));
		}

		private static string Describe(TextMeshProUGUI label)
		{
			if (label == null)
			{
				return "<null>";
			}

			var info = label.textInfo;

			return (label.name + " '" + label.text + "'")
				+ " chars=" + (info == null ? "?" : info.characterCount.ToString())
				+ " wrap=" + label.textWrappingMode
				+ " overflow=" + label.overflowMode
				+ " rect=" + (label.rectTransform == null
					? "?"
					: label.rectTransform.rect.width.ToString("0") + "x"
						+ label.rectTransform.rect.height.ToString("0"))
				+ " lines=" + (info == null || info.lineInfo == null
					? "?"
					: info.lineInfo.Length.ToString());
		}

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			if (MessagesUI != null)
			{
				MessageUI.ShowMessage = Item;
				UIController.Instance.ScreenNavigator.NavigateToScreen<MessageUI>();
			}
		}
	}
}
