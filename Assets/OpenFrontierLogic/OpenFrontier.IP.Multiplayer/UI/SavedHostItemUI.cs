using System.Collections.Generic;
using OpenFrontier.IP.UI;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// One row in the host list.
	///
	/// Shows the host's name, what the last liveness probe found, its game
	/// version when that version is the reason it cannot be joined, and how
	/// many players are already in. Rows that are confirmed unreachable are
	/// dimmed; "Checking..." while a probe is in flight - claiming a host is
	/// offline before asking it would be a lie the player acts on.
	/// </summary>
	///
	/// Kept in a file of its own rather than alongside <see cref="SavedHostItemsUI"/>
	/// because Unity registers a MonoScript per file, named after the file. A
	/// second MonoBehaviour sharing someone else's file has no MonoScript, so
	/// AddComponent and the inspector both refuse it and it serialises as a
	/// missing script - which compiles perfectly well and only fails once it is
	/// actually put on a prefab.
	public class SavedHostItemUI : ScrollListItem<SavedHost>
	{
		/// <summary>The host's name, falling back to its code when unnamed.</summary>
		public TextMeshProUGUI NameLabel;

		/// <summary>What the last probe found: online, offline, or a mismatch.</summary>
		public TextMeshProUGUI StatusLabel;

		/// <summary>
		/// The host's game version, shown only when it differs from this build's
		/// - which is the one case where the player has to be told something
		/// rather than simply being unable to join.
		/// </summary>
		public TextMeshProUGUI VersionLabel;

		/// <summary>How many players are already connected to the host.</summary>
		public TextMeshProUGUI PlayerCountLabel;

		/// <summary>
		/// How much a row that cannot be joined is dimmed by.
		/// </summary>
		/// <remarks>
		/// Applied to the labels rather than to a badge: the row already says
		/// "Offline" in words, and the only spare graphic in the prefab is the
		/// leftover permadeath chip, which is a plain white box and means
		/// nothing to a host. Dimming puts the eye on the rows that can
		/// actually be joined, which is the thing worth showing at a glance.
		/// </remarks>
		public float OfflineAlpha = 0.45f;

		private Dictionary<TextMeshProUGUI, Color> authoredColours;

		protected override void awake()
		{
			base.awake();

			// Captured once, before anything dims them. Refresh() runs on every
			// list rebuild and a row is reused for whichever host lands in its
			// slot, so the dim has to be lifted from what was dimmed before -
			// not from whatever the prefab happened to ship with.
			authoredColours = new Dictionary<TextMeshProUGUI, Color>();

			Remember(NameLabel);
			Remember(StatusLabel);
			Remember(VersionLabel);
			Remember(PlayerCountLabel);
		}

		private void Remember(TextMeshProUGUI label)
		{
			if (label != null && !authoredColours.ContainsKey(label))
			{
				authoredColours[label] = label.color;
			}
		}

		public override void Refresh()
		{
			base.Refresh();

			if (Item == null)
			{
				return;
			}

			NameLabel.text = string.IsNullOrWhiteSpace(Item.Name) ? Item.Code : Item.Name;

			HostProbeResult probe = Item.ProbeResult;

			// Being in the session is the most useful thing to say, so it takes
			// precedence over the probe result.
			StatusLabel.text = Item.IsConnected ? "Connected" : probe.Describe();

			// Version: only worth showing when it explains a refusal. Showing
			// "0.1.1" against every row that matches would be noise, so it
			// stays hidden rather than blank.
			if (VersionLabel != null)
			{
				bool mismatch = probe.Reachability == HostReachability.VersionMismatch;

				VersionLabel.enabled = mismatch;
				VersionLabel.text = mismatch && probe.HostVersion != null
					? "v" + probe.HostVersion
					: string.Empty;
			}

			// The count has its own label, so DescribeState() deliberately
			// leaves it out. A host that is up with nobody in it shows nothing.
			if (PlayerCountLabel != null)
			{
				bool show = probe.Reachability == HostReachability.Online
					&& probe.PlayerCount > 0;

				PlayerCountLabel.enabled = show;
				PlayerCountLabel.text = show ? probe.PlayerCount.ToString() : string.Empty;
			}

			// Only a confirmed negative dims anything. While a probe is still
			// in flight the honest answer is "we don't know", and dimming on
			// that would flash a lie every time the list opened.
			bool unreachable = !Item.IsConnected
				&& probe.IsKnown
				&& probe.Reachability == HostReachability.Unreachable;

			SetDimmed(unreachable);
		}

		private void SetDimmed(bool dimmed)
		{
			if (authoredColours == null)
			{
				return;
			}

			float alpha = dimmed ? Mathf.Clamp01(OfflineAlpha) : 1f;

			foreach (KeyValuePair<TextMeshProUGUI, Color> entry in authoredColours)
			{
				Color colour = entry.Value;
				colour.a = colour.a * alpha;

				entry.Key.color = colour;
			}
		}
	}
}
