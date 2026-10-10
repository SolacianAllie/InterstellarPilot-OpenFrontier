using System;
using OpenFrontier.IP.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// Shows this player's own join code so they can read it out, type it into
	/// another machine, or copy it to the clipboard.
	///
	/// Built from the same prefab as the rename prompt, minus the input field,
	/// because it is the same shape of dialog: a title, a line of text, and two
	/// buttons. The left button copies instead of cancelling, so nothing is lost
	/// by dismissing with the wrong one.
	/// </summary>
	public class JoinCodeScreen : ScreenBase
	{
		/// <summary>
		/// Typed as the TMP base rather than TextMeshProUGUI because the prefabs
		/// are a mix of TextMeshPro and TextMeshProUGUI labels, and the base is
		/// what both assign to.
		/// </summary>
		public TMP_Text TitleLabel;

		/// <summary>The code itself, or an explanation when there is not one.</summary>
		public TMP_Text CodeLabel;

		public Button OkButton;

		/// <summary>Copies the code to the clipboard, then closes.</summary>
		public Button CopyButton;

		/// <summary>
		/// The prompt's legacy label, kept only so its text can be replaced.
		/// </summary>
		/// <remarks>
		/// The source prefab mixes label types: TextScreenTitle is TextMeshPro,
		/// but LegacyTextSubTitle - the line the code would go in - is a legacy
		/// uGUI Text, which cannot be assigned to a TMP_Text field. Rather than
		/// swap the whole dialog to legacy labels, the legacy one is held as a
		/// plain Text and written to directly. Whichever kind is wired, the code
		/// shows.
		/// </remarks>
		public Text LegacyCodeLabel;

		/// <summary>
		/// What to show and copy.
		/// </summary>
		/// <remarks>
		/// Set before the screen is made current, so refresh() can pick it up.
		/// Null means there is nothing to share, which is a real state rather
		/// than an error: the player is not hosting anything.
		/// </remarks>
		public string CodeText { get; set; }

		/// <summary>
		/// Why there is no code, when there is a specific reason to give.
		/// </summary>
		/// <remarks>
		/// Separate from the "you are not hosting" default because "the listener
		/// could not start" needs a different response from the player than "you
		/// have not turned it on" - one is a problem to fix, the other is a
		/// setting to change.
		/// </remarks>
		public string Explanation { get; set; }

		public string TitleText
		{
			get
			{
				return TitleLabel == null ? null : TitleLabel.text;
			}
			set
			{
				if (TitleLabel != null)
				{
					TitleLabel.text = value;
				}
			}
		}

		/// <summary>
		/// What the player is told when there is no code to show.
		/// </summary>
		/// <remarks>
		/// Says what to do, because the button is on the screen that manages
		/// hosts and the reason it is empty is not obvious from there. Kept to
		/// one short line: this sits in a dialog sized for a 32-character code,
		/// and a paragraph spills out of the sides of it.
		/// </remarks>
		public const string NoSessionText =
			"Multiplayer is off.\nTurn it on in Options, then try again.";

		/// <summary>Raised after a successful copy, so the screen can confirm it.</summary>
		public event Action Copied;

		/// <summary>Size for the code itself, matching the label as authored.</summary>
		public const int CodeFontSize = 24;

		/// <summary>
		/// Size for an explanation instead of a code.
		/// </summary>
		/// <remarks>
		/// Smaller because it is prose rather than something to be transcribed,
		/// and because a sentence at the code's size does not fit the panel.
		/// </remarks>
		public const int ExplanationFontSize = 16;

		protected override void awake()
		{
			base.awake();

			// Null-checked rather than assumed: this screen is instantiated from
			// a prefab clone, and a half-wired one should say so rather than
			// throw on open.
			if (OkButton != null)
			{
				OkButton.onClick.AddListener(Dismiss);
			}

			if (CopyButton != null)
			{
				CopyButton.onClick.AddListener(CopyToClipboard);
			}
		}

		protected override void refresh()
		{
			base.refresh();

			if (CodeLabel == null && LegacyCodeLabel == null)
			{
				Debug.LogError(
					"JoinCodeScreen has neither a TextMeshPro nor a legacy label for the code, so "
						+ "there is nowhere to show it. Re-run Tools > Multiplayer > "
						+ "Transfer Screen References.", this);
				return;
			}

			string shown = string.IsNullOrWhiteSpace(CodeText)
				? (string.IsNullOrWhiteSpace(Explanation) ? NoSessionText : Explanation)
				: CodeText;

			// The code is the thing to be read aloud and copied, so it gets the
			// large size the label was authored for. An explanation is a
			// paragraph and reads as a wall at the same size - and overflows the
			// panel, which is exactly what it did before this.
			//
			// SetFontSize rather than assigning fontSize directly: best-fit would
			// otherwise override the size on the next layout pass and the two
			// settings would fight, with whichever ran last winning.
			int size = string.IsNullOrWhiteSpace(CodeText) ? ExplanationFontSize : CodeFontSize;

			SetFontSize(CodeLabel, size);
			SetFontSize(LegacyCodeLabel, size);

			if (CodeLabel != null)
			{
				CodeLabel.text = shown;
			}

			if (LegacyCodeLabel != null)
			{
				LegacyCodeLabel.text = shown;
			}

			// Copying nothing would look like it worked. The button is disabled
			// rather than hidden so the row does not reflow when it changes.
			if (CopyButton != null)
			{
				CopyButton.interactable = !string.IsNullOrWhiteSpace(CodeText);
			}
		}

		private void CopyToClipboard()
		{
			if (string.IsNullOrWhiteSpace(CodeText))
			{
				return;
			}

			// systemCopyBuffer is the cross-platform clipboard: it goes through
			// the Android clipboard on a device, so this is not a desktop-only
			// affordance.
			GUIUtility.systemCopyBuffer = CodeText;

			Copied?.Invoke();
		}

		/// <summary>
		/// Sets a label's size, without best-fit overriding it.
		/// </summary>
		private static void SetFontSize(TMP_Text label, int size)
		{
			if (label == null)
			{
				return;
			}

			// A best-fit label recomputes its own size on every layout pass, so
			// assigning fontSize to one is silently undone a moment later.
			label.enableAutoSizing = false;
			label.fontSize = size;
		}

		private static void SetFontSize(Text label, int size)
		{
			if (label == null)
			{
				return;
			}

			label.resizeTextForBestFit = false;
			label.fontSize = size;
		}

		private void Dismiss()
		{
			NavigateBack();
		}
	}
}
