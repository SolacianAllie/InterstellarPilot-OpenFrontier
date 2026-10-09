using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Multiplayer;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// The multiplayer screen: a list of saved hosts, the way LoadGameScreen is
	/// a list of saved games.
	///
	/// Built by adapting LoadGameScreen, so the list, the selection handling,
	/// the keyboard entry screen and the rename and delete flows are the ones
	/// the game already has. What changes here is the item type - a saved host
	/// rather than a save file - and what the primary button does: it joins the
	/// selected host instead of loading a scenario.
	///
	/// Import and export are removed: there is no file to move around, because
	/// a host is reached over the network rather than from disk. Rename and
	/// delete are kept, since a list of things the player has joined before
	/// needs both.
	/// </summary>
	public class MultiplayerScreen : ScreenBase
	{
		/// <summary>The saved-host list. The clone carries this already wired.</summary>
		public SavedHostItemsUI ItemsList;

		/// <summary>Joins the selected host. Sits where LoadGameScreen's load button is.</summary>
		public Button JoinButton;

		/// <summary>Adds a host by asking for a join code. Next to the join button.</summary>
		public Button AddHostButton;

		public Button DeleteHostButton;

		public Button RenameHostButton;

		public GameObject HostInfoRoot;

		public Transform MultipleItemsSelectedRoot;

		public Text HostNameLabel;

		public Text HostCodeLabel;

		public Text HostAddedLabel;

		public Text HostStatusLabel;

		public Text TitleLabel;

		private ClientSession client;

		protected override void awake()
		{
			base.awake();

			JoinButton.onClick.AddListener(JoinSelectedHost);
			AddHostButton.onClick.AddListener(AddHostButtonClick);
			DeleteHostButton.onClick.AddListener(DeleteHostButtonClick);
			RenameHostButton.onClick.AddListener(RenameHostButtonClick);

			ItemsList.SelectedItemsChanged += OnSelectedItemsChanged;
		}

		protected override void refresh()
		{
			base.refresh();

			SavedHostStore.InvalidateCache();
			ItemsList.Refresh();
			RefreshItemData();
		}

		protected override void start()
		{
			base.start();

			if (TitleLabel != null)
			{
				TitleLabel.text = "Multiplayer";
			}
		}

		// ---- adding a host -------------------------------------------------

		/// <summary>
		/// Asks for a join code using the same keyboard screen the rename and
		/// save-file name entry uses, so the input feels like the rest of the
		/// game rather than a bespoke prompt.
		/// </summary>
		private void AddHostButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen(screen =>
			{
				screen.TitleText = "Enter a join code...";
				screen.MinCharacters = 1;

				// 40 covers the 32-character code with room for the separators
				// the player might paste along with it.
				screen.MaxCharacters = 40;

				// Join codes are Base32 - A-Z and 2-7 - plus the dashes used for
				// grouping. Rejecting anything else here means an obvious typo is
				// caught while typing rather than as a failed connection.
				screen.RegexPatternValidation = "^[A-Za-z2-7\\-]*$";
				screen.RegexPatternValidationMessage = "That is not a join code. A code uses the letters A-Z, the digits 2-7, and dashes.";

				screen.NewName = string.Empty;
				screen.RenameConfirmed += (handler, confirmed, enteredCode) =>
				{
					if (!confirmed)
					{
						return;
					}

					if (string.IsNullOrWhiteSpace(enteredCode))
					{
						return;
					}

					// Confirm the code is at least well-formed before saving it,
					// so the list cannot fill up with rubbish typed by mistake.
					SessionCodeParseResult parsed = SessionCode.TryParse(enteredCode);

					if (!parsed.Success)
					{
						UIController.Instance.ShowMessageBox(
							parsed.Error, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
						return;
					}

					// Save the code exactly as typed. The parsed form is only used
					// for validation here - the player can still attempt to join a
					// host from a different version later and be told why, rather
					// than having the entry silently rejected at add time.
					SavedHost added = SavedHostStore.AddOrUpdate(null, enteredCode);

					if (added == null)
					{
						return;
					}

					ItemsList.Refresh();
					ItemsList.SetSelectedItems(new[] { added });
					ItemsList.ScrollToSelected();
					RefreshItemData();
				};
			});
		}

		// ---- joining --------------------------------------------------------

		private void JoinSelectedHost()
		{
			SavedHost host = SingleSelectedHost;

			if (host == null)
			{
				return;
			}

			SessionCodeParseResult parsed = SessionCode.TryParse(host.Code);

			if (!parsed.Success)
			{
				UIController.Instance.ShowMessageBox(
					"This host's code is not readable: " + parsed.Error,
					MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				return;
			}

			// The version is checked here, before any connection is attempted,
			// so a host on a different build is refused immediately rather than
			// after a timeout.
			if (!parsed.Data.IsCompatibleWithLocalBuild(out string incompatibility))
			{
				UIController.Instance.ShowMessageBox(
					incompatibility, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				return;
			}

			Disconnect();

			client = new ClientSession();
			client.Failed += OnJoinFailed;
			client.Connected += () => MarkConnected(host, true);
			client.Disconnected += reason =>
			{
				MarkConnected(host, false);

				if (!string.IsNullOrEmpty(reason))
				{
					UIController.Instance.ShowMessageBox(
						reason, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				}

				Refresh();
			};

			client.Join(host.Code);
		}

		private void OnJoinFailed(string reason)
		{
			UIController.Instance.ShowMessageBox(reason, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
		}

		/// <summary>
		/// Records whether a session with this host is live, so the list can
		/// show it. The flag lives on the entry rather than in a dictionary so
		/// it survives the list being rebuilt.
		/// </summary>
		private void MarkConnected(SavedHost host, bool connected)
		{
			if (host != null)
			{
				host.IsConnected = connected;
			}

			RefreshItemData();
		}

		private void Disconnect()
		{
			if (client == null)
			{
				return;
			}

			client.Dispose();
			client = null;
		}

		// ---- rename / delete ------------------------------------------------

		private void RenameHostButtonClick()
		{
			SavedHost host = SingleSelectedHost;

			if (host == null)
			{
				return;
			}

			UIController.Instance.ScreenNavigator.ShowRenameScreen(screen =>
			{
				screen.TitleText = "Rename host...";
				screen.MinCharacters = 1;
				screen.MaxCharacters = 30;
				screen.RegexPatternValidation = "^[a-zA-Z0-9 _-]*$";
				screen.RegexPatternValidationMessage = "Acceptable characters: a-z, A-Z, 0-9, space, '-' and '_'";
				screen.NewName = host.Name;
				screen.RenameConfirmed += (handler, confirmed, newName) =>
				{
					if (confirmed)
					{
						SavedHostStore.Rename(host, newName);

						// Rebuild so the row picks up the new name.
						ItemsList.Refresh();
						RefreshItemData();
					}
				};
			});
		}

		private void DeleteHostButtonClick()
		{
			int count = ItemsList.SelectedItemCount;

			if (count == 0)
			{
				return;
			}

			// Deleting a saved host only removes it from this player's list. It
			// does not affect the host or their game, so the wording says so.
			string message = count == 1
				? "Remove this host from your list? The host and their game are not affected."
				: $"Remove the {count} selected hosts from your list? The hosts and their games are not affected.";

			UIController.Instance.ShowMessageBox(
				message, MessageBoxButtons.OkCancel, DeleteConfirmed, MessageBoxIcon.Question);
		}

		private void DeleteConfirmed(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result != MessageBoxResult.Ok)
			{
				return;
			}

			foreach (SavedHost host in ItemsList.SelectedItems.ToList())
			{
				SavedHostStore.Remove(host);
			}

			ItemsList.Refresh();
			RefreshItemData();
		}

		// ---- details panel --------------------------------------------------

		private SavedHost SingleSelectedHost =>
			ItemsList.SelectedItemCount == 1 ? ItemsList.SingleSelectedItem : null;

		private void OnSelectedItemsChanged(ScrollListBase sender)
		{
			RefreshItemData();
		}

		/// <summary>
		/// Shows and hides the controls that only make sense for the current
		/// selection, mirroring how LoadGameScreen does it.
		/// </summary>
		private void RefreshItemData()
		{
			SavedHost single = SingleSelectedHost;

			HostInfoRoot.gameObject.SetActive(single != null);
			MultipleItemsSelectedRoot.gameObject.SetActive(ItemsList.SelectedItemCount > 1);

			JoinButton.gameObject.SetActive(single != null && !single.IsConnected);
			DeleteHostButton.gameObject.SetActive(ItemsList.SelectedItemCount > 0);
			RenameHostButton.gameObject.SetActive(single != null);

			if (single == null)
			{
				return;
			}

			HostNameLabel.text = string.IsNullOrWhiteSpace(single.Name) ? single.Code : single.Name;
			HostCodeLabel.text = single.Code;
			HostAddedLabel.text = single.AddedUtc.ToLocalTime().ToString("dd-MMM-yyyy H:mm");
			HostStatusLabel.text = single.IsConnected ? "Connected" : "Not connected";
		}

		protected override void onDestroy()
		{
			Disconnect();

			base.onDestroy();
		}
	}
}