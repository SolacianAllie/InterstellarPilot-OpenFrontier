using System;
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

			// The prefab this screen was adapted from is LoadGameScreen, which
			// still has its own field set. Swapping the script without remapping
			// the fields leaves every one of them null, and each is dereferenced
			// the moment the screen is opened - so the failure would look like a
			// crash on opening rather than an unwired prefab. Naming it here
			// means the console says what is actually wrong.
			if (!AreReferencesAssigned())
			{
				return;
			}

			JoinButton.onClick.AddListener(JoinSelectedHost);
			AddHostButton.onClick.AddListener(AddHostButtonClick);
			DeleteHostButton.onClick.AddListener(DeleteHostButtonClick);
			RenameHostButton.onClick.AddListener(RenameHostButtonClick);

			ItemsList.SelectedItemsChanged += OnSelectedItemsChanged;
		}

		protected override void refresh()
		{
			base.refresh();

			// An unwired prefab would otherwise touch a null list here and throw
			// on every refresh, burying the one message that explains why.
			if (ItemsList == null)
			{
				return;
			}

			SavedHostStore.InvalidateCache();
			ItemsList.Refresh();
			RefreshItemData();

			// Probe as soon as the screen opens rather than waiting out the
			// first interval, so the list is useful immediately.
			ProbeAllHosts();
			nextProbeTime = Time.unscaledTime + ProbeIntervalSeconds;
		}

		/// <summary>
		/// Re-probes on an interval while the screen is open.
		/// </summary>
		protected override void update()
		{
			base.update();

			if (ItemsList == null || Time.unscaledTime < nextProbeTime)
			{
				return;
			}

			// Unscaled time, so a paused game still refreshes - a player
			// checking whether a friend is up should not have to unpause.
			nextProbeTime = Time.unscaledTime + ProbeIntervalSeconds;
			ProbeAllHosts();
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
		/// Checks every reference this screen needs, and names the ones that are
		/// missing.
		/// </summary>
		private bool AreReferencesAssigned()
		{
			List<string> missing = new List<string>();

			if (ItemsList == null) missing.Add(nameof(ItemsList));
			if (JoinButton == null) missing.Add(nameof(JoinButton));
			if (AddHostButton == null) missing.Add(nameof(AddHostButton));
			if (DeleteHostButton == null) missing.Add(nameof(DeleteHostButton));
			if (RenameHostButton == null) missing.Add(nameof(RenameHostButton));
			if (HostInfoRoot == null) missing.Add(nameof(HostInfoRoot));
			if (MultipleItemsSelectedRoot == null) missing.Add(nameof(MultipleItemsSelectedRoot));
			if (HostNameLabel == null) missing.Add(nameof(HostNameLabel));
			if (HostCodeLabel == null) missing.Add(nameof(HostCodeLabel));
			if (HostAddedLabel == null) missing.Add(nameof(HostAddedLabel));
			if (HostStatusLabel == null) missing.Add(nameof(HostStatusLabel));

			if (missing.Count == 0)
			{
				return true;
			}

			Debug.LogError(
				"MultiplayerScreen is not fully wired. Unassigned: "
				+ string.Join(", ", missing)
				+ ". This prefab was cloned from LoadGameScreen, so its fields still have that "
				+ "screen's names; see Assets/GameObject/MultiplayerScreen.README.md for the mapping.");

			return false;
		}

		// ---- probing for liveness -------------------------------------------

		/// <summary>
		/// How often the saved hosts are probed while this screen is open.
		/// </summary>
		/// <remarks>
		/// Each probe is one short connection to the host, so this is a
		/// deliberate cost: often enough that a host which has just come up is
		/// noticed quickly, rare enough that a long session is not quietly
		/// hammering someone else's machine.
		/// </remarks>
		private const float ProbeIntervalSeconds = 15f;

		private float nextProbeTime;

		/// <summary>
		/// Asks every saved host whether it is up, concurrently.
		/// </summary>
		/// <remarks>
		/// Sequential probing would cost the timeout per dead host, so a list of
		/// ten offline hosts would take half a minute to settle. Probing in
		/// parallel bounds the whole refresh to one timeout.
		/// </remarks>
		private void ProbeAllHosts()
		{
			IReadOnlyList<SavedHost> hosts = SavedHostStore.Hosts;

			if (hosts.Count == 0)
			{
				return;
			}

			List<SessionCodeData> targets = new List<SessionCodeData>(hosts.Count);
			Dictionary<string, SavedHost> byCode = new Dictionary<string, SavedHost>(hosts.Count);

			foreach (SavedHost host in hosts)
			{
				SessionCodeParseResult parsed = SessionCode.TryParse(host.Code);

				// A host whose code no longer parses cannot be probed; it keeps
				// whatever it had, and joining it will explain the problem.
				if (!parsed.Success)
				{
					continue;
				}

				targets.Add(parsed.Data);
				byCode[host.Code] = host;
			}

			if (targets.Count == 0)
			{
				return;
			}

			HostProbe.ProbeAll(targets, (probed, result) =>
			{
				// A probe can land after the player has backed out. Touching the
				// list then re-initialises it - ScrollList.Refresh() calls Awake
				// if it thinks it is not awake - which fights the screen's own
				// teardown and leaves the navigation state inconsistent, so the
				// back button stops responding on the first press.
				if (isTornDown || ItemsList == null)
				{
					return;
				}

				if (byCode.TryGetValue(ProbeKey(probed), out SavedHost host))
				{
					host.ProbeResult = result;

					// Repaint the affected rows. Refresh() on the whole list is
					// cheap at this size and avoids reaching into the list's
					// internals to update one row.
					ItemsList.Refresh();
					RefreshItemData();
				}
			});
		}

		/// <summary>
		/// Set once the screen is going away, so late probe results are ignored.
		/// </summary>
		private bool isTornDown;

		/// <summary>
		/// A stable key for a probed code, so results can be matched back to the
		/// entry they belong to.
		/// </summary>
		private static string ProbeKey(SessionCodeData data)
		{
			return data.Address + ":" + data.Port + ":"
				+ Convert.ToBase64String(data.Secret ?? Array.Empty<byte>());
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

			if (single.IsConnected)
			{
				HostStatusLabel.text = "Connected";
			}
			else
			{
				HostStatusLabel.text = single.ProbeResult.Describe();
			}
		}

		protected override void onDestroy()
		{
			Disconnect();

			base.onDestroy();
		}
	}
}