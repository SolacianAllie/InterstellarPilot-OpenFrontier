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
		/// A small line under the code, used for the "how to turn this off
		/// again" note.
		/// </summary>
		/// <remarks>
		/// The prompt's character counter, repurposed. It was hidden when the
		/// input field was, and it is already a small single line under the
		/// text area - which is exactly the shape this wants.
		/// </remarks>
		public Text HintLabel;

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

		/// <summary>Raised after a successful copy, so the screen can confirm it.</summary>
		public event Action Copied;

		/// <summary>Title while there is no code yet.</summary>
		public const string TitleOff = "Your join code - multiplayer is off";

		/// <summary>Title once hosting.</summary>
		public const string TitleOn = "Your join code - multiplayer is on";

		/// <summary>Body while there is no code yet.</summary>
		public const string NoSessionText = "Turn it on below.";

		/// <summary>
		/// Shown once hosting, because the switch is hidden at that point and
		/// the player would otherwise have no idea where to turn it off.
		/// </summary>
		public const string HowToDisableText =
			"To turn multiplayer off again, use your menu, Options, then General.";

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

			// Subscribed rather than polled from update(). ScreenBase only calls
			// update() for the current screen while it is in the navigation
			// stack and not fading out, so a poll there silently stops running
			// in several perfectly ordinary situations - and does so without a
			// word, leaving a dialog showing "off" while a code sits right
			// there waiting to be shown.
			MultiplayerHostService.HostingStarted += OnHostingStarted;
		}

		private void OnHostingStarted(string joinCode)
		{
			// Only while there is nothing to show. A code arriving while one is
			// already displayed means hosting restarted underneath us, and the
			// new one is the one that is actually current.
			CodeText = joinCode;

			refresh();
		}

		protected override void onDestroy()
		{
			// The service outlives this screen, so leaving the subscription
			// behind would keep this object alive and refresh a dialog nobody
			// can see.
			MultiplayerHostService.HostingStarted -= OnHostingStarted;

			base.onDestroy();
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

			// Nothing to show yet either way: hosting starts on the service's
			// next frame, and OnHostingStarted brings the code in when it does.
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

			// One flag drives every part of the layout, so the title, the body,
			// the switch and the hint can never disagree about which state they
			// are in.
			bool hosting = !string.IsNullOrWhiteSpace(CodeText);

			// A specific failure reason replaces the generic prompt - "could not
			// open a port" needs a different response from "you have not turned
			// it on", and only the former is a problem to go and fix.
			string body = hosting
				? CodeText
				: (string.IsNullOrWhiteSpace(Explanation) ? NoSessionText : Explanation);

			TitleText = hosting ? TitleOn : TitleOff;

			// The code is the thing to be read aloud and copied, so it gets the
			// large size the label was authored for. An explanation is a
			// paragraph and reads as a wall at the same size - and overflows the
			// panel, which is exactly what it did before this.
			//
			// SetFontSize rather than assigning fontSize directly: best-fit would
			// otherwise override the size on the next layout pass and the two
			// settings would fight, with whichever ran last winning.
			SetFontSize(CodeLabel, hosting ? CodeFontSize : ExplanationFontSize);
			SetFontSize(LegacyCodeLabel, hosting ? CodeFontSize : ExplanationFontSize);

			if (CodeLabel != null)
			{
				CodeLabel.text = body;
			}

			if (LegacyCodeLabel != null)
			{
				LegacyCodeLabel.text = body;
			}

			// Copying nothing would look like it worked. The button is disabled
			// rather than hidden so the row does not reflow when it changes.
			if (CopyButton != null)
			{
				CopyButton.interactable = hosting;
			}

			// The switch is offered only while there is nothing to show. Once
			// there is a code it has done its job, and the space reads better
			// given to the code than to a control that would only turn it off.
			if (EnableMultiplayerToggle != null)
			{
				EnableMultiplayerToggle.gameObject.SetActive(!hosting);

				if (!hosting && OpenFrontier.IP.Engine.GameController.Instance != null)
				{
					EnableMultiplayerToggle.SetIsOnWithoutNotify(
						OpenFrontier.IP.Engine.GameController.Instance.PlayerOptions.General_EnableMultiplayer);
				}
			}

			// With the switch gone there is nothing on screen saying where to
			// turn multiplayer off, so the hint takes its place. Without this the
			// only way back is to go and find Options again.
			if (HintLabel != null)
			{
				HintLabel.gameObject.SetActive(hosting);
				HintLabel.text = HowToDisableText;
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
