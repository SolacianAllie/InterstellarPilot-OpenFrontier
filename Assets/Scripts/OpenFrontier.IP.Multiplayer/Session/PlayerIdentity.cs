using System;
using System.Security.Cryptography;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// A player's persistent multiplayer identity.
	///
	/// This is the token the host uses to recognise a returning player. It is
	/// generated once on the device and stored locally, and it is what lets
	/// someone rejoin and pick up control of the same faction and the same unit
	/// they had when they left - the host maps token to faction and unit, and
	/// hands control back on the next join.
	///
	/// The token is an opaque random value, not an account: there is no server
	/// and nothing to sign in to. Anyone who copies the device's PlayerPrefs
	/// also copies the identity, which is acceptable for a game with no server
	/// and no credentials.
	/// </summary>
	public static class PlayerIdentity
	{
		private const string PrefsKey = "of_multiplayer_player_token";

		// Enough bytes that two identities colliding is not a practical concern,
		// and short enough to sit comfortably in the handshake.
		private const int TokenBytes = 16;

		private static string cachedToken;

		/// <summary>
		/// This device's token, generated on first use and then reused.
		///
		/// Generated lazily rather than in a field initializer: PlayerPrefs is a
		/// Unity native call, and this type is reached during serialization in
		/// places where such calls are not allowed.
		/// </summary>
		public static string Token
		{
			get
			{
				if (string.IsNullOrEmpty(cachedToken))
				{
					cachedToken = LoadOrCreateToken();
				}

				return cachedToken;
			}
		}

		/// <summary>
		/// Discards the cached token so the next read comes from storage again.
		/// Used after a settings reset; not part of normal play.
		/// </summary>
		public static void InvalidateCache()
		{
			cachedToken = null;
		}

		private static string LoadOrCreateToken()
		{
			string existing = PlayerPrefs.GetString(PrefsKey, string.Empty);
			if (!string.IsNullOrEmpty(existing))
			{
				return existing;
			}

			string created = GenerateToken();
			PlayerPrefs.SetString(PrefsKey, created);

			// Persist immediately: losing this token means losing the ability to
			// reclaim the faction, so it should survive a crash right after
			// first use.
			PlayerPrefs.Save();
			return created;
		}

		private static string GenerateToken()
		{
			byte[] bytes = new byte[TokenBytes];

			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(bytes);
			}

			return Convert.ToBase64String(bytes)
				.Replace("+", "-")
				.Replace("/", "_")
				.TrimEnd('=');
		}

		/// <summary>
		/// A fresh session secret for a newly hosted game. Not persisted: the
		/// host's own code is regenerated each time it starts hosting, and an
		/// old session's secret should not keep working.
		/// </summary>
		public static byte[] CreateSessionSecret()
		{
			byte[] bytes = new byte[SessionCode.PayloadSize];

			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(bytes);
			}

			return bytes;
		}
	}

	/// <summary>What this installation is willing to do with multiplayer.</summary>
	public enum MultiplayerMode
	{
		/// <summary>Never open a listener, never offer to join. The default for anyone who has not opted in.</summary>
		Off = 0,

		/// <summary>Can join someone else's session, but will not host one.</summary>
		JoinOnly = 1,

		/// <summary>Can host and can join.</summary>
		HostOrJoin = 2
	}

	/// <summary>
	/// The player's multiplayer preference.
	///
	/// This is the enable/disable switch the player controls. It is checked
	/// before any socket is opened, so turning multiplayer off genuinely means
	/// nothing is listening and no connection is attempted - not merely that
	/// the UI is hidden.
	/// </summary>
	public static class MultiplayerSettings
	{
		private const string PrefsKey = "of_multiplayer_mode";

		private static MultiplayerMode? cachedMode;

		/// <summary>Current preference, defaulting to Off.</summary>
		public static MultiplayerMode Mode
		{
			get
			{
				if (!cachedMode.HasValue)
				{
					cachedMode = Load();
				}

				return cachedMode.Value;
			}

			set
			{
				cachedMode = value;
				PlayerPrefs.SetInt(PrefsKey, (int)value);
				PlayerPrefs.Save();
			}
		}

		/// <summary>Whether this installation may take part in multiplayer at all.</summary>
		public static bool Enabled => Mode != MultiplayerMode.Off;

		/// <summary>Whether this installation may open a listener for others.</summary>
		public static bool CanHost => Mode == MultiplayerMode.HostOrJoin;

		/// <summary>Whether this installation may connect to someone else's session.</summary>
		public static bool CanJoin => Mode == MultiplayerMode.JoinOnly || Mode == MultiplayerMode.HostOrJoin;

		private static MultiplayerMode Load()
		{
			// Absent key means the player has never chosen, which is Off: joining
			// a session should be a deliberate act, not a default.
			if (!PlayerPrefs.HasKey(PrefsKey))
			{
				return MultiplayerMode.Off;
			}

			int raw = PlayerPrefs.GetInt(PrefsKey, (int)MultiplayerMode.Off);

			// Guard against a corrupted or stale value written by an older
			// build rather than casting an arbitrary int to the enum.
			if (raw == (int)MultiplayerMode.Off
				|| raw == (int)MultiplayerMode.JoinOnly
				|| raw == (int)MultiplayerMode.HostOrJoin)
			{
				return (MultiplayerMode)raw;
			}

			return MultiplayerMode.Off;
		}
	}
}