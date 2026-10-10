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
		/// Turns multiplayer on without leaving this dialog.
		/// </summary>
		/// <remarks>
		/// The same row the player would otherwise have to find in Options,
		/// cloned across so the two cannot drift apart. Shown only while there
		/// is no code: once there is one, the switch has done its job and the
		/// space is better given to the code.
		/// </remarks>
		public Toggle EnableMultiplayerToggle;

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
		/// Smaller than the code because it is prose rather than something to be
		/// transcribed, but only a little: the panel is sized for a 32-character
		/// code, so an explanation at the code's own size sits in a lot of empty
		/// space and looks like an afterthought.
		/// </remarks>
		public const int ExplanationFontSize = 22;

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

			if (EnableMultiplayerToggle != null)
			{
				EnableMultiplayerToggle.onValueChanged.AddListener(EnableMultiplayerChanged);
			}
		}

		/// <summary>
		/// Watches for the host coming up once the switch is flipped.
		/// </summary>
		/// <remarks>
		/// Hosting does not start here. It is MultiplayerHostService's job, and
		/// it happens on the next frame - the service reads the same option, so
		/// there is one path into it and this dialog is not a second one that
		/// could fall out of step.
		///
		/// So all that is needed here is to notice when a code has appeared and
		/// repaint. Polling a null check is cheaper than wiring an event across
		/// assemblies for something that happens once.
		/// </remarks>
		protected override void update()
		{
			base.update();

			if (EnableMultiplayerToggle != null
				&& EnableMultiplayerToggle.gameObject.activeSelf
				&& EnableMultiplayerToggle.isOn
				&& string.IsNullOrWhiteSpace(CodeText)
				&& !string.IsNullOrWhiteSpace(MultiplayerHostService.JoinCode))
			{
				CodeText = MultiplayerHostService.JoinCode;
				refresh();
			}
		}

		private void EnableMultiplayerChanged(bool value)
		{
			// Written to PlayerOptions rather than to MultiplayerSettings: the
			// options screen writes there, and the host service reads from
			// there, so this is the same switch by another route rather than a
			// second setting that can disagree with it.
			//
			// The settings class lives in this assembly but the option does not,
			// and this screen can reach both - unlike OptionsScreen, which sits
			// on the other side of a one-way reference.
			if (OpenFrontier.IP.Engine.GameController.Instance != null)
			{
				OpenFrontier.IP.Engine.GameController.Instance.PlayerOptions.General_EnableMultiplayer = value;
			}

			// Nothing to show yet either way; the update() above waits for the
			// service to bring a code up, and this shows the "still working on
			// it" state in the meantime.
			refresh();
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

			// The switch is offered only while there is nothing to show. Once
			// there is a code it has done its job, and the space reads better
			// given to the code than to a control that would only turn it off.
			if (EnableMultiplayerToggle != null)
			{
				bool off = string.IsNullOrWhiteSpace(CodeText);

				EnableMultiplayerToggle.gameObject.SetActive(off);

				if (off && OpenFrontier.IP.Engine.GameController.Instance != null)
				{
					EnableMultiplayerToggle.SetIsOnWithoutNotify(
						OpenFrontier.IP.Engine.GameController.Instance.PlayerOptions.General_EnableMultiplayer);
				}
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

			// Close BEFORE confirming, never after. Raising the confirmation
			// first would make its message box the current screen, and the
			// NavigateBack below would then be refused by its own guard -
			// "only current panel can navigate back" - leaving this dialog open
			// with the confirmation stacked on top of it.
			//
			// Dismiss is synchronous, so by the time Copied is raised this
			// screen is off the stack and the message lands on whatever is
			// underneath.
			Dismiss();

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
