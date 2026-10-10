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
		/// What to show and copy.
		/// </summary>
		/// <remarks>
		/// Set before the screen is made current, so refresh() can pick it up.
		/// Null means there is nothing to share, which is a real state rather
		/// than an error: the player is not hosting anything.
		/// </remarks>
		public string CodeText { get; set; }

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
		/// Deliberately explains what to do rather than just saying there is
		/// nothing - the button is on the screen that manages hosts, so the
		/// reason it is empty is not obvious from there.
		/// </remarks>
		public const string NoSessionText =
			"You are not hosting a game.\n\nStart hosting first, then come back for your join code.";

		/// <summary>Raised after a successful copy, so the screen can confirm it.</summary>
		public event Action Copied;

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

			if (CodeLabel == null)
			{
				return;
			}

			CodeLabel.text = string.IsNullOrWhiteSpace(CodeText) ? NoSessionText : CodeText;

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

		private void Dismiss()
		{
			NavigateBack();
		}
	}
}
