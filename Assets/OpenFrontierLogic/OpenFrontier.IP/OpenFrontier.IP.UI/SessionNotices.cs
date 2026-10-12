using UnityEngine;

namespace OpenFrontier.IP.UI
{
	/// <summary>
	/// Something the player needs to be told once there is a screen up to
	/// say it on.
	/// </summary>
	/// <remarks>
	/// Lives here, and not with the networking that produces these reasons,
	/// because the networking assembly already points this way and not the
	/// other way round. A reference back would be a circle between two
	/// assemblies, which Unity refuses outright.
	///
	/// The pattern is the same one the authority question and the
	/// shared-session flag already use: the game owns the vocabulary, and
	/// whatever has something to say says it in the game's words.
	/// </remarks>
	public static class SessionNotices
	{
		/// <summary>Whether a notice is a warning or a failure.</summary>
		public enum NoticeKind
		{
			/// <summary>Something ended that did not have to. Worth a warning.</summary>
			Warning,

			/// <summary>Something went wrong. Worth an error.</summary>
			Error
		}

		private static string pendingReason;
		private static NoticeKind pendingKind;
		private static bool hasPending;

		/// <summary>Whether something is waiting to be said.</summary>
		public static bool HasPending
		{
			get { return hasPending; }
		}

		/// <summary>
		/// Leaves a reason for the next screen that comes up.
		/// </summary>
		/// <remarks>
		/// Stashed rather than shown, because the moment a message box would
		/// be readable is usually the moment after the one worth reading it
		/// in. A session that ends in a world sends the player to the menu
		/// immediately, and the popup raised over the world is torn down with
		/// it - so the player arrives in silence, which is worse than not
		/// arriving.
		///
		/// Replaces anything already waiting. There is only ever one reason
		/// worth telling, and the latest is it.
		/// </remarks>
		public static void Publish(string reason, NoticeKind kind)
		{
			if (string.IsNullOrEmpty(reason))
			{
				return;
			}

			pendingReason = reason;
			pendingKind = kind;
			hasPending = true;
		}

		/// <summary>
		/// Takes the reason, if one is waiting, so that it is said once.
		/// </summary>
		/// <remarks>
		/// Taken rather than asked about, so returning to a screen does not
		/// repeat it. Nobody needs to be reminded of a session that ended
		/// yesterday.
		/// </remarks>
		public static bool Take(out string reason, out NoticeKind kind)
		{
			reason = pendingReason;
			kind = pendingKind;

			pendingReason = null;
			hasPending = false;

			return reason != null;
		}

		/// <summary>
		/// Says whatever is waiting, on whatever screen is up now.
		/// </summary>
		/// <returns>Whether anything was said.</returns>
		/// <remarks>
		/// Takes it whatever happens. A reason that could not be shown - the
		/// UI is mid scene change, or not built yet - is not worth keeping,
		/// because by the time the next screen is up the player has long
		/// since made up their mind about what they are doing, and an old
		/// message arriving late is noise rather than news.
		/// </remarks>
		public static bool SayPending()
		{
			string reason;
			NoticeKind kind;

			if (!Take(out reason, out kind))
			{
				return false;
			}

			try
			{
				UIController.Instance?.ShowMessageBox(
					reason,
					Screens.MessageBoxButtons.Ok,
					null,
					kind == NoticeKind.Warning
						? Screens.MessageBox.MessageBoxIcon.Warning
						: Screens.MessageBox.MessageBoxIcon.Error);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("Multiplayer: could not show the notice - " + e.Message);
			}

			return true;
		}
	}
}
