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
using UnityEngine.Events;
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

		/// <summary>Shows this player's own join code, to share or copy.</summary>
		public Button JoinCodeButton;

		/// <summary>
		/// The dialog showing that code.
		/// </summary>
		/// <remarks>
		/// Typed as ScreenBase rather than JoinCodeScreen for the same reason
		/// the main menu's reference to this screen is: the navigator looks
		/// screens up by type, and naming the multiplayer type here would make
		/// the two assemblies reference each other.
		/// </remarks>
		public ScreenBase JoinCodeScreenPrefab;

		public Button DeleteHostButton;

		public Button RenameHostButton;

		public GameObject SessionPanelRoot;

		public Transform MultipleItemsSelectedRoot;

		/// <summary>The panel's heading.</summary>
		public Text SessionTitleLabel;

		/// <summary>This player's own join code.</summary>
		public Text SessionCodeLabel;

		/// <summary>Whether this player is hosting.</summary>
		public Text SessionHostingLabel;

		/// <summary>The address and port written into every join code.</summary>
		public Text SessionAddressLabel;

		/// <summary>The host or join setting, in words.</summary>
		public Text SessionModeLabel;

		/// <summary>The game build this player is running.</summary>
		public Text SessionVersionLabel;

		/// <summary>The wire protocol, which is what a join actually checks.</summary>
		public Text SessionProtocolLabel;

		/// <summary>How many players are connected.</summary>
		public Text SessionPlayersLabel;

		/// <summary>This player's identity, short enough to compare out loud.</summary>
		public Text SessionIdentityLabel;

		/// <summary>Anything that went wrong, or empty.</summary>
		/// <remarks>
		/// Left empty rather than filled with a dash, so the line does not sit
		/// there saying nothing for as long as nothing is wrong.
		/// </remarks>
		public Text SessionProblemLabel;

		/// <summary>Whether the router is forwarding this session's port.</summary>
		public Text SessionRouterLabel;

		public Text TitleLabel;

		private ClientSession client;

		protected override void awake()
		{
			base.awake();

			// Each control is wired as soon as it exists, rather than all or
			// nothing. This prefab was cloned from LoadGameScreen, so while it is
			// still being wired up one unassigned field took every control down
			// with it - Add Host looked broken rather than unwired, which is a
			// misleading thing to be looking at.
			WireButton(JoinButton, JoinSelectedHost);
			WireButton(AddHostButton, AddHostButtonClick);
			WireButton(JoinCodeButton, JoinCodeButtonClick);
			WireButton(DeleteHostButton, DeleteHostButtonClick);
			WireButton(RenameHostButton, RenameHostButtonClick);

			if (ItemsList != null)
			{
				ItemsList.SelectedItemsChanged += OnSelectedItemsChanged;
			}

			// Still reported, because something genuinely is unwired - but only
			// after the controls that did get wired are live, so the screen is
			// usable while the prefab is being finished.
			AreReferencesAssigned();
		}

		private static void WireButton(Button button, UnityAction action)		{
			if (button != null)
			{
				button.onClick.AddListener(action);
			}
		}

		protected override void refresh()
		{
			base.refresh();

			// Reported here rather than from the callback that stashed it,
			// because refresh() is what runs once this screen is current again -
			// the point at which a message box can be shown without stealing the
			// navigation away from whatever is trying to close.
			ReportPendingAddError();

			// An unwired prefab would otherwise touch a null list here and throw
			// on every refresh, burying the one message that explains why.
			if (ItemsList == null)
			{
				return;
			}

			RefreshHostList();
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

			// Refreshed every frame rather than on the probe interval: the
			// host service retries on a timer of its own and may start or
			// fail long after this screen was drawn, and this panel exists
			// to report exactly that. Waiting for the probes would leave it
			// lying about the state of the thing it is describing.
			RefreshSessionPanel();

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

				// 44 covers the 32-character code with room for the separators
				// the player might paste along with it - a code is normally
				// shown as four blocks of eight, so a copy out of the host's
				// screen arrives with dashes already in it.
				screen.MaxCharacters = 44;

				// Base32 is A-Z and 2-7, plus the separators a code is displayed
				// with. This has to match what SessionCode.TryParse strips, or
				// validation would reject a code the parser would have accepted
				// - which is what a paste straight out of the host's screen
				// looks like. Being permissive here costs nothing: TryParse
				// checks the length and the checksum, and a wrong code is
				// refused there rather than silently saved.
				screen.RegexPatternValidation = "^[A-Za-z2-7\\-_ ]*$";
				screen.RegexPatternValidationMessage =
					"That is not a join code. A code uses the letters A-Z, the digits 2-7, and dashes.";

				screen.NewName = string.Empty;
				screen.RenameConfirmed += (handler, confirmed, enteredCode) =>
				{
					if (!confirmed)
					{
						return;
					}

					SavedHost added = AddHostFromCode(enteredCode);

					if (added != null)
					{
						ShowHost(added);
					}
				};
			});
		}

		/// <summary>
		/// Validates a typed code and saves it, stashing anything wrong with it
		/// to be reported once this screen is current again.
		/// </summary>
		/// <returns>The saved entry, or null if nothing was saved.</returns>
		/// <remarks>
		/// Deliberately does not show a message box. This runs inside the rename
		/// screen's RenameConfirmed callback, which fires immediately before that
		/// screen calls NavigateBack - and a message box becomes the current
		/// screen, at which point NavigateBack refuses ("Only current panel ...
		/// can navigate back"). The player is then left sitting on the entry
		/// screen with an error over it, pressing OK to get nowhere.
		///
		/// Letting the rename screen close first and reporting here means the
		/// navigation always completes and the message lands somewhere the
		/// player can act on it.
		/// </remarks>
		private SavedHost AddHostFromCode(string enteredCode)
		{
			if (string.IsNullOrWhiteSpace(enteredCode))
			{
				return null;
			}

			// Confirm the code is at least well-formed before saving it, so the
			// list cannot fill up with rubbish typed by mistake.
			SessionCodeParseResult parsed = SessionCode.TryParse(enteredCode);

			if (!parsed.Success)
			{
				pendingAddError = parsed.Error;
				return null;
			}

			// Caught here rather than at join time, so the mistake never reaches
			// the host list at all. Refused locally rather than by connecting and
			// failing: that would surface as a network error and say nothing
			// about what actually went wrong.
			if (IsOwnCode(parsed.Data))
			{
				pendingAddError = SelfJoinReplies.Pick();
				return null;
			}

			// Save the code exactly as typed. The parsed form is only used for
			// validation here - the player can still attempt to join a host
			// from a different version later and be told why, rather than having
			// the entry silently rejected at add time.
			return SavedHostStore.AddOrUpdate(null, enteredCode);
		}

		/// <summary>
		/// Whether a parsed code is the player's own.
		/// </summary>
		/// <remarks>
		/// False when not hosting, because then there is no session for the code
		/// to belong to - and a player with multiplayer off who pastes a code
		/// they were given should be told about the version instead.
		/// </remarks>
		private static bool IsOwnCode(SessionCodeData data)
		{
			return MultiplayerHostService.Session != null
				&& MultiplayerHostService.Session.IsOwnCode(data);
		}

		/// <summary>An add-host failure waiting for this screen to be current again.</summary>
		private string pendingAddError;

		/// <summary>Reports a stashed add-host failure, once it is safe to.</summary>
		private void ReportPendingAddError()
		{
			if (string.IsNullOrEmpty(pendingAddError))
			{
				return;
			}

			string message = pendingAddError;
			pendingAddError = null;

			UIController.Instance.ShowMessageBox(
				message, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
		}

		/// <summary>
		/// Puts a newly added host on screen: selected, scrolled to, and probed
		/// straight away.
		/// </summary>
		/// <remarks>
		/// Probed here rather than left to the interval because otherwise the
		/// row the player just created sits on "Checking..." for up to fifteen
		/// seconds - which reads as the add having not worked. A mistyped code
		/// that still passes the checksum is exactly the case worth catching
		/// while the player is still looking at the screen.
		/// </remarks>
		private void ShowHost(SavedHost added)
		{
			RefreshHostList();

			if (ItemsList == null || added == null)
			{
				return;
			}

			// Re-resolved: refreshing the list rebuilds the entries, so the
			// object AddOrUpdate returned may already be a discarded one.
			SavedHost live = FindByCode(added.Code);

			if (live == null)
			{
				return;
			}

			ItemsList.SetSelectedItems(new[] { live });
			ItemsList.ScrollToSelected();
			RefreshItemData();

			ProbeHost(live);
		}

		/// <summary>Probes one host and repaints the list when the answer lands.</summary>
		private void ProbeHost(SavedHost host)
		{
			if (host == null || ItemsList == null)
			{
				return;
			}

			SessionCodeParseResult parsed = SessionCode.TryParse(host.Code);

			if (!parsed.Success)
			{
				return;
			}

			HostProbe.Probe(parsed.Data, result =>
			{
				// A probe can land after the player has backed out; touching the
				// list then re-initialises it and leaves the navigation state
				// inconsistent.
				if (isTornDown || ItemsList == null)
				{
					return;
				}

				ApplyProbeResult(host.Code, result);
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

			// Checked again here, not only when the host was added: a code saved
			// hours ago can be this player's own by now, since the secret is
			// regenerated each time hosting starts. Without this the join would
			// be attempted and reported as a failure to connect.
			if (IsOwnCode(parsed.Data))
			{
				UIController.Instance.ShowMessageBox(
					SelfJoinReplies.Pick(), MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
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
			// Resolved by code rather than set on the object handed in:
			// SavedHostStore drops its cache and rebuilds the list on every
			// screen refresh, which creates new SavedHost objects. The one a
			// join callback captured is often already a discarded one, and its
			// flag would then be set on an object no row is showing.
			SavedHost live = FindByCode(host == null ? null : host.Code);

			if (live != null)
			{
				live.IsConnected = connected;
			}

			RefreshItemData();
		}

		/// <summary>The current entry for a code, or null if it is no longer saved.</summary>
		private static SavedHost FindByCode(string code)
		{
			if (string.IsNullOrEmpty(code))
			{
				return null;
			}

			foreach (SavedHost candidate in SavedHostStore.Hosts)
			{
				if (string.Equals(candidate.Code, code, StringComparison.OrdinalIgnoreCase))
				{
					return candidate;
				}
			}

			return null;
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
					if (!confirmed)
					{
						return;
					}

					SavedHost live = FindByCode(host.Code) ?? host;

					SavedHostStore.Rename(live, newName);

					// Rebuild so the row picks up the new name.
					RefreshHostList();
					RefreshItemData();
				};
			});
		}

		private void DeleteHostButtonClick()
		{
			int count = ItemsList == null ? 0 : ItemsList.SelectedItemCount;

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
			if (result != MessageBoxResult.Ok || ItemsList == null)
			{
				return;
			}

			foreach (SavedHost host in ItemsList.SelectedItems.ToList())
			{
				SavedHostStore.Remove(host);
			}

			RefreshHostList();
			RefreshItemData();
		}

		// ---- showing your own join code ---------------------------------------

		/// <summary>
		/// Opens the dialog showing this player's own join code.
		/// </summary>
		private void JoinCodeButtonClick()
		{
			if (JoinCodeScreenPrefab == null)
			{
				UIController.Instance.ShowMessageBox(
					"The join code dialog is not set up yet. Run Tools > Multiplayer > "
						+ "Transfer Screen References, then assign JoinCodeScreenPrefab.",
					MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				return;
			}

			JoinCodeScreen screen = Instantiate(JoinCodeScreenPrefab) as JoinCodeScreen;

			if (screen == null)
			{
				UIController.Instance.ShowMessageBox(
					"The join code dialog could not be created.",
					MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				return;
			}

			// The title is not set here: it depends on whether a code exists,
			// which only the dialog knows once refresh() has run.
			screen.CodeText = OwnJoinCode;
			screen.Explanation = MultiplayerHostService.LastProblem;

			// A directly instantiated screen needs Awake called explicitly; the
			// navigator only does that for screens it loads itself.
			screen.Awake();

			screen.Copied += () => UIController.Instance.ShowMessageBox(
				"Join code copied to the clipboard.", MessageBoxButtons.Ok, null, MessageBoxIcon.Info);

			UIController.Instance.ScreenNavigator.NavigateToScreen(screen);
		}

		/// <summary>
		/// This player's join code, or null when they are not hosting.
		/// </summary>
		/// <remarks>
		/// Hosting runs whenever the option is on, so this is null only when the
		/// player has switched multiplayer off or the listener could not start -
		/// and in the second case <see cref="MultiplayerHostService.LastProblem"/>
		/// says why, which is worth showing instead of the generic "not hosting".
		/// </remarks>
		private static string OwnJoinCode => MultiplayerHostService.JoinCode;

		// ---- details panel --------------------------------------------------

		private SavedHost SingleSelectedHost =>
			ItemsList != null && ItemsList.SelectedItemCount == 1 ? ItemsList.SingleSelectedItem : null;

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
			if (JoinCodeButton == null) missing.Add(nameof(JoinCodeButton));
			if (DeleteHostButton == null) missing.Add(nameof(DeleteHostButton));
			if (RenameHostButton == null) missing.Add(nameof(RenameHostButton));
			if (SessionPanelRoot == null) missing.Add(nameof(SessionPanelRoot));
			if (MultipleItemsSelectedRoot == null) missing.Add(nameof(MultipleItemsSelectedRoot));
			if (SessionTitleLabel == null) missing.Add(nameof(SessionTitleLabel));
			if (SessionCodeLabel == null) missing.Add(nameof(SessionCodeLabel));
			if (SessionHostingLabel == null) missing.Add(nameof(SessionHostingLabel));
			if (SessionAddressLabel == null) missing.Add(nameof(SessionAddressLabel));
			if (SessionModeLabel == null) missing.Add(nameof(SessionModeLabel));
			if (SessionVersionLabel == null) missing.Add(nameof(SessionVersionLabel));
			if (SessionProtocolLabel == null) missing.Add(nameof(SessionProtocolLabel));
			if (SessionPlayersLabel == null) missing.Add(nameof(SessionPlayersLabel));
			if (SessionRouterLabel == null) missing.Add(nameof(SessionRouterLabel));
			if (SessionIdentityLabel == null) missing.Add(nameof(SessionIdentityLabel));
			if (SessionProblemLabel == null) missing.Add(nameof(SessionProblemLabel));

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

			// Keyed by probe identity, valued by code. The entry is looked up
			// again when the result lands rather than held onto, because a
			// refresh in between rebuilds the list and orphans the object.
			Dictionary<string, string> codeByKey = new Dictionary<string, string>(hosts.Count);

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
				codeByKey[ProbeKey(parsed.Data)] = host.Code;
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

				if (codeByKey.TryGetValue(ProbeKey(probed), out string code))
				{
					ApplyProbeResult(code, result);
				}
			});
		}

		/// <summary>
		/// Records a probe result against the live entry and repaints.
		/// </summary>
		private void ApplyProbeResult(string code, HostProbeResult result)
		{
			SavedHost live = FindByCode(code);

			if (live == null)
			{
				return;
			}

			live.ProbeResult = result;

			// Repaint the affected rows. Refresh() on the whole list is cheap at
			// this size and avoids reaching into the list's internals to update
			// one row.
			RefreshHostList();
			RefreshItemData();
		}

		/// <summary>
		/// Re-reads the store and rebuilds the list.
		/// </summary>
		/// <remarks>
		/// Guarded against a row prefab that cannot be instantiated. ScrollList
		/// logs the mismatch and pools a null, then dereferences it during
		/// Awake - which runs inside this call, and which for this screen runs
		/// inside OnMadeCurrentScreen. The exception then unwinds out of the
		/// navigator half way through making the screen current, leaving it
		/// activated but not really shown, and the player cannot get back out
		/// of it. Refusing to refresh leaves an empty list the player can leave,
		/// with one message saying why.
		/// </remarks>
		private void RefreshHostList()
		{
			if (ItemsList == null)
			{
				return;
			}

			if (!RowPrefabIsUsable())
			{
				return;
			}

			SavedHostStore.InvalidateCache();
			ItemsList.Refresh();
		}

		/// <summary>
		/// Whether the list's row prefab can actually produce a row.
		/// </summary>
		private bool RowPrefabIsUsable()
		{
			GameObject rowPrefab = ItemsList.ItemPrefab;

			if (rowPrefab == null)
			{
				return false;
			}

			if (rowPrefab.GetComponent<ScrollListItem<SavedHost>>() != null)
			{
				return true;
			}

			Debug.LogError(
				"The host list's row prefab '" + rowPrefab.name + "' has no SavedHostItemUI, so no "
					+ "rows can be shown. Run Tools > Multiplayer > Transfer Screen References to put "
					+ "it there - see Assets/GameObject/MultiplayerScreen.README.md.", this);

			return false;
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
			int selectedCount = ItemsList == null ? 0 : ItemsList.SelectedItemCount;
			SavedHost single = SingleSelectedHost;

			// The panel reports this player's own session rather than the
			// selected host, so it has nothing to do with the selection.
			// The one exception is the multi-select prompt, which shares
			// the same space and would otherwise be drawn on top of it.
			bool severalSelected = selectedCount > 1;

			SetActive(SessionPanelRoot, !severalSelected);
			SetActive(MultipleItemsSelectedRoot, severalSelected);

			SetActive(JoinButton, single != null && !single.IsConnected);
			SetActive(DeleteHostButton, selectedCount > 0);
			SetActive(RenameHostButton, single != null);

			// Add Host never depends on a selection, so it is left alone here.

			PositionHostRowButtons();

			RefreshSessionPanel();
		}

		/// <summary>
		/// Reports this player's own multiplayer session: whether hosting is
		/// working, on what address and port, and what to do about it if not.
		/// </summary>
		/// <remarks>
		/// This panel is the answer to "is multiplayer even working?", which
		/// until now could only be had from a log file on a developer's
		/// machine. Every line is something a failed join traces back to: a
		/// code that has to be copied exactly, an address that has to be
		/// reachable, a port that is different on every restart, and two
		/// builds that have to match exactly.
		/// 
		/// Stuck to plain facts on purpose. Nothing here guesses at why a join
		/// failed, because from inside the game a stale code and a firewall
		/// look identical, and a confident wrong answer costs a player more
		/// than an honest one.
		/// </remarks>
		private void RefreshSessionPanel()
		{
			HostSession session = MultiplayerHostService.Session;

			Set(SessionTitleLabel, "Your multiplayer session");
			Set(SessionModeLabel, DescribeMode(MultiplayerSettings.Mode));
			Set(SessionVersionLabel, Versioning.Version == null
				? "unknown"
				: Versioning.Version.ToString());
			Set(SessionProtocolLabel, ProtocolVersion.Current.ToString());
			Set(SessionPlayersLabel, session == null ? "0" : session.Players.Count.ToString());
			// "All interfaces", because that is the bind: the session accepts on
			// WiFi, on Ethernet and on any public address alike, and the row
			// below names the one address that goes into the code.
			Set(SessionHostingLabel, session != null ? "Yes - all interfaces" : "No");
			Set(SessionAddressLabel, session == null ? "Not listening" : session.Endpoint);
			Set(SessionCodeLabel, string.IsNullOrEmpty(OwnJoinCode) ? "None" : OwnJoinCode);

			// Short enough to read out and compare. The whole token is there
			// for a developer; a player checking two devices want to know
			// they are two different people, not to read sixty characters
			// aloud.
			string token = PlayerIdentity.Token ?? string.Empty;
			Set(SessionIdentityLabel, token.Length <= 8 ? token : token.Substring(0, 8));

			Set(SessionRouterLabel, DescribePortMapping(MultiplayerHostService.PortMapping));

			// The router's own explanation is the most useful sentence on this
			// screen when a join fails, and there is nowhere else to put it. It
			// only takes the line when there is nothing more important to say,
			// so a real networking problem is never buried under it.
			UpnpResult mapping = MultiplayerHostService.PortMapping;
			Set(SessionProblemLabel, string.IsNullOrEmpty(MultiplayerHostService.LastProblem)
				? mapping.Detail
				: MultiplayerHostService.LastProblem);
		}

		/// <summary>Sets a label, tolerating it not being there.</summary>
		/// <remarks>
		/// Every line on this panel is optional. A prefab cloned before the
		/// panel existed would otherwise throw on the first missing reference
		/// and take the whole screen down with it, which is a worse outcome
		/// than a panel missing a row.
		/// </remarks>
		private static void Set(Text label, string value)
		{
			if (label != null)
			{
				label.text = value ?? string.Empty;
			}
		}

		/// <summary>
		/// Says whether players outside this network can reach the host.
		/// </summary>
		/// <remarks>
		/// Reported rather than assumed, and never phrased as a guarantee: a
		/// router can drop the forward at any time, and a player told they are
		/// reachable who is not has been told something worse than nothing.
		/// </remarks>
		private static string DescribePortMapping(UpnpResult mapping)
		{
			switch (mapping.Status)
			{
				case UpnpStatus.Asking:
					return "Asking...";

				case UpnpStatus.Mapped:
					return "Forwarded: " + mapping.Port;

				case UpnpStatus.NotAttempted:
					return "Not hosting";

				case UpnpStatus.NotNeeded:
					return "No way out";

				case UpnpStatus.Removed:
					return "Closed";

				// The failures are named rather than collapsed into "No". A
				// router that is simply not offering UPnP is a different
				// situation from one that offered and then refused, and only
				// one of them is worth a player changing something.
				case UpnpStatus.NoGateway:
					return "None found";

				case UpnpStatus.Refused:
					return "Router said no";

				case UpnpStatus.Failed:
					return "Ask failed";

				default:
					return "No";
			}
		}

		/// <summary>Turns the setting into something a player can act on.</summary>
		private static string DescribeMode(MultiplayerMode mode)
		{
			switch (mode)
			{
				case MultiplayerMode.HostOrJoin:
					return "Host or join";

				case MultiplayerMode.JoinOnly:
					return "Join only";

				default:
					return "Off";
			}
		}

		/// <summary>
		/// Right-anchored x position of the rightmost button in the row.
		/// </summary>
		private const float RowRightmostX = -52f;

		/// <summary>
		/// How far left each further button sits.
		/// </summary>
		/// <remarks>
		/// The row is positioned by hand rather than by a layout group:
		/// ImageForePanelDetails - the parent of all three - carries only a
		/// LayoutElement, and the layout group is further up on LayoutHorizontal.
		/// So hiding one button does not close the gap, it just leaves a hole in
		/// the row, and the survivors have to be moved to keep the row looking
		/// deliberate.
		///
		/// Spacing rather than three fixed positions per state, so the row stays
		/// right-aligned and a fourth button would slot in without new numbers.
		/// </remarks>
		private const float RowSpacingX = -108f;

		/// <summary>
		/// Re-positions the host row's buttons for whichever of them are showing.
		/// </summary>
		private void PositionHostRowButtons()
		{
			bool joinVisible = JoinButton != null && JoinButton.gameObject.activeSelf;

			// Join, when it is showing, takes the rightmost slot. Otherwise Add
			// Host takes it and everything else shifts in by one.
			SetAnchoredX(joinVisible ? JoinButton : AddHostButton, RowRightmostX);

			if (joinVisible)
			{
				SetAnchoredX(AddHostButton, RowRightmostX + RowSpacingX);
				SetAnchoredX(JoinCodeButton, RowRightmostX + (2 * RowSpacingX));
			}
			else
			{
				SetAnchoredX(JoinCodeButton, RowRightmostX + RowSpacingX);
			}
		}

		/// <summary>
		/// Moves a control along the row, keeping its y where it was.
		/// </summary>
		private static void SetAnchoredX(Component component, float x)
		{
			RectTransform rect = component == null ? null : component.transform as RectTransform;

			if (rect != null)
			{
				rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
			}
		}

		/// <summary>
		/// Shows or hides an optional panel or control.
		/// </summary>
		/// <remarks>
		/// Checked rather than assumed because this runs on every selection
		/// change and every probe result: one unassigned field would otherwise
		/// throw repeatedly and take the screen down with it.
		/// </remarks>
		private static void SetActive(Component component, bool active)
		{
			if (component != null)
			{
				component.gameObject.SetActive(active);
			}
		}

		/// <summary>
		/// Shows or hides an optional panel held as a GameObject.
		/// </summary>
		/// <remarks>
		/// An overload rather than a change of field type: the prefab stores a
		/// GameObject reference under a different fileID from a Transform one,
		/// so retyping the field would silently drop the assignment.
		/// </remarks>
		private static void SetActive(GameObject gameObject, bool active)
		{
			if (gameObject != null)
			{
				gameObject.SetActive(active);
			}
		}

		protected override void onDestroy()
		{
			// Set before anything else so a probe that lands while the screen is
			// being torn down sees the flag rather than reaching for a list that
			// is going away underneath it.
			isTornDown = true;

			Disconnect();

			base.onDestroy();
		}
	}
}