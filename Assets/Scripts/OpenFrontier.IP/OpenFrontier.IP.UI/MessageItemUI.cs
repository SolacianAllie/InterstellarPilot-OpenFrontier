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

			LetTheTextWrap();
		}

		/// <summary>
		/// Stops the From and Subject columns being truncated to nothing.
		/// </summary>
		/// <remarks>
		/// With word wrapping off and overflow set to truncate or ellipsis, a
		/// line wider than its rect produces NO characters at all - not a
		/// shortened line, none. Measured, not guessed:
		///
		///     TextFrom   chars=0   wrap=NoWrap
		///     TextHeader chars=0   wrap=NoWrap
		///     TextDate   chars=16  wrap=Normal
		///
		/// A message is far wider than its rect; a date is not. That is the
		/// whole difference between two empty columns and one working column
		/// on the same row, with the same font and the same component.
		///
		/// Set here rather than only in the prefab because the prefab edit was
		/// correct on disk and still not in effect - Unity had not reimported
		/// it, and a build has it baked in. AGENTS.md already records
		/// hand-edited YAML being stripped by tooling in this project; a
		/// value that has to survive a reimport to keep working is a value
		/// that will eventually stop working.
		///
		/// Written only when it is wrong, so a list that refreshes every frame
		/// does not rebuild its text mesh every frame.
		/// </remarks>
		/// <summary>
		/// Whether to force the wrapping at runtime rather than trusting the
		/// prefab to carry it.
		/// </summary>
		/// <remarks>
		/// Off, so the prefab can be tested on its own.
		///
		/// The runtime version was added because the prefab edit was correct
		/// on disk and still had no effect - Unity had not reimported it, and
		/// a build bakes it in. That was a reason to suspect the prefab route
		/// does not work, not proof that it does not. Leaving both in place
		/// means a run cannot tell which one fixed anything, and a fix that
		/// cannot be attributed cannot be trusted to be the one that is
		/// needed.
		///
		/// So: prefab only for now. If reimporting fixes it, the prefab was
		/// always the right place and the runtime version is not needed. If
		/// it does not, this is one line and the evidence is clean.
		/// </remarks>
		private const bool ForceWrappingInCode = false;

		private void LetTheTextWrap()
		{
			if (!ForceWrappingInCode)
			{
				return;
			}

			Wrap(FromLabel);
			Wrap(SubjectText);
		}

		private static void Wrap(TextMeshProUGUI label)
		{
			if (label != null && label.textWrappingMode != TextWrappingModes.Normal)
			{
				label.textWrappingMode = TextWrappingModes.Normal;
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
