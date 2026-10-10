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

				Diag();
			}
		}

		private static bool diagged;

		/// <summary>
		/// Says what this row actually holds, once per session.
		/// </summary>
		/// <remarks>
		/// The From and Subject columns render empty while Date renders fine,
		/// with the same font asset, the same component and the same row. Every
		/// offline explanation has been ruled out one at a time - the font
		/// asset is present and shared, the prefab references resolve, all
		/// three labels sit inside the row, and neither getter can return an
		/// empty string, one of them returning "[No Subject]" as a floor.
		///
		/// So the remaining question is what the labels hold at runtime, and
		/// that cannot be read out of a prefab. Once, because a list refreshes
		/// constantly and this is a diagnostic, not a feature.
		/// </remarks>
		private void Diag()
		{
			if (diagged)
			{
				return;
			}

			diagged = true;

			Debug.Log("DiagMsg: from='" + FromLabel.text + "' (" + FromLabel.text.Length
				+ " chars, " + (FromLabel.enabled ? "enabled" : "DISABLED")
				+ ", colour " + FromLabel.color + ", rect " + RectOf(FromLabel) + ")"
				+ " subject='" + SubjectText.text + "' (" + SubjectText.text.Length
				+ " chars, " + (SubjectText.enabled ? "enabled" : "DISABLED")
				+ ", colour " + SubjectText.color + ", rect " + RectOf(SubjectText) + ")"
				+ " date='" + DateLabel.text + "' (" + DateLabel.text.Length
				+ " chars, " + (DateLabel.enabled ? "enabled" : "DISABLED")
				+ ", colour " + DateLabel.color + ", rect " + RectOf(DateLabel) + ")"
				+ " item.FromText='" + Item.FromText + "' item.SubjectText='"
				+ Item.SubjectText + "' template="
				+ (Item.MessageTemplate == null ? "none" : "yes"));
		}

		private static string RectOf(TMP_Text label)
		{
			RectTransform rect = label.rectTransform;

			return rect == null
				? "no rect"
				: rect.rect.width.ToString("0") + "x" + rect.rect.height.ToString("0")
					+ (label.textInfo == null ? ", no textInfo" : ", " + label.textInfo.characterCount + " glyphs");
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
