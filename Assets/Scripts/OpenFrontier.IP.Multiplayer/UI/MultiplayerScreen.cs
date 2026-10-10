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

			// Each control is wired as soon as it exists, rather than all or
			// nothing. This prefab was cloned from LoadGameScreen, so while it is
			// still being wired up one unassigned field took every control down
			// with it - Add Host looked broken rather than unwired, which is a
			// misleading thing to be looking at.
			WireButton(JoinButton, JoinSelectedHost);
			WireButton(AddHostButton, AddHostButtonClick);
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

			// Save the code exactly as typed. The parsed form is only used for
			// validation here - the player can still attempt to join a host
			// from a different version later and be told why, rather than having
			// the entry silently rejected at add time.
			return SavedHostStore.AddOrUpdate(null, enteredCode);
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

			SetActive(HostInfoRoot, single != null);
			SetActive(MultipleItemsSelectedRoot, selectedCount > 1);

			SetActive(JoinButton, single != null && !single.IsConnected);
			SetActive(DeleteHostButton, selectedCount > 0);
			SetActive(RenameHostButton, single != null);

			// Add Host never depends on a selection, so it is left alone here.

			if (single == null)
			{
				return;
			}

			if (HostNameLabel != null)
			{
				HostNameLabel.text = string.IsNullOrWhiteSpace(single.Name) ? single.Code : single.Name;
			}

			if (HostCodeLabel != null)
			{
				HostCodeLabel.text = single.Code;
			}

			if (HostAddedLabel != null)
			{
				HostAddedLabel.text = single.AddedUtc.ToLocalTime().ToString("dd-MMM-yyyy H:mm");
			}

			if (HostStatusLabel != null)
			{
				HostStatusLabel.text = single.IsConnected
					? "Connected"
					: single.ProbeResult.Describe();
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