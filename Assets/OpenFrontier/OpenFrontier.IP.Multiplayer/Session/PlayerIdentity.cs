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
		// Scoped by PlayerProfile, so two copies of the game on one machine can
		// be two players. See PlayerProfile.
		private const string PrefsKey = "of_multiplayer_player_token";

		private const string SessionSecretPrefsKey = "of_multiplayer_session_secret";

		// Enough bytes that two identities colliding is not a practical concern,
		// and short enough to sit comfortably in the handshake.
		private const int TokenBytes = 16;

		private static string cachedToken;

		private static byte[] cachedSecret;

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
		/// Discards everything cached so the next read comes from storage again.
		/// Used after a settings reset; not part of normal play.
		/// </summary>
		public static void InvalidateCache()
		{
			cachedToken = null;
			cachedSecret = null;
			PlayerProfile.InvalidateCache();
		}

		private static string LoadOrCreateToken()
		{
			string existing = PlayerPrefs.GetString(PlayerProfile.Key(PrefsKey), string.Empty);
			if (!string.IsNullOrEmpty(existing))
			{
				return existing;
			}

			string created = GenerateToken();
			PlayerPrefs.SetString(PlayerProfile.Key(PrefsKey), created);

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

			return ToBase64Url(bytes);
		}

		/// <summary>
		/// This installation's session secret, generated on first use and then
		/// reused for every session it hosts.
		/// </summary>
		/// <remarks>
		/// Persisted, which reverses an earlier decision to draw a new secret
		/// every time hosting started. That decision was defensible on its own
		/// terms - an old session's secret should not keep working - but it is
		/// incompatible with a saved host list, because a join code carries
		/// part of this secret and every code a player had already saved would
		/// be dead the moment the host restarted.
		///
		/// Making the port fixed was only half of that fix and this is the
		/// other half. Together they are what a saved code needs to survive a
		/// restart; with only the port fixed it still died here, and the host
		/// refused a code it had issued minutes earlier with "that join code
		/// isn't valid for this session".
		///
		/// Reusing it costs very little safety. The secret travels in the
		/// clear inside a join code that is already copied around by hand, and
		/// what it protects is the right to open a session the host could have
		/// closed at any moment anyway. The self-join check is unaffected -
		/// it compares the secret AND the endpoint, and both still identify
		/// exactly this machine's current session.
		/// </remarks>
		public static byte[] SessionSecret
		{
			get
			{
				if (cachedSecret == null)
				{
					cachedSecret = LoadOrCreateSessionSecret();
				}

				return cachedSecret;
			}
		}

		private static byte[] LoadOrCreateSessionSecret()
		{
			// Base64url, as for the token: PlayerPrefs is all strings, and
			// "+" and "/" would have to be escaped.
			string existing = PlayerPrefs.GetString(PlayerProfile.Key(SessionSecretPrefsKey), string.Empty);

			if (!string.IsNullOrEmpty(existing))
			{
				try
				{
					byte[] stored = FromBase64Url(existing);

					if (stored.Length == SessionCode.PayloadSize)
					{
						return stored;
					}
				}
				catch (FormatException)
				{
					// Corrupted, or written by a build that meant something
					// else. Fall through and make a new one.
				}
			}

			byte[] bytes = new byte[SessionCode.PayloadSize];

			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(bytes);
			}

			PlayerPrefs.SetString(PlayerProfile.Key(SessionSecretPrefsKey), ToBase64Url(bytes));

			// Saved immediately for the same reason the token is: a code that
			// has already been shared out should not stop working because the
			// machine lost power a second later.
			PlayerPrefs.Save();

			return bytes;
		}

		private static string ToBase64Url(byte[] bytes)
		{
			return Convert.ToBase64String(bytes)
				.Replace("+", "-")
				.Replace("/", "_")
				.TrimEnd('=');
		}

		private static byte[] FromBase64Url(string value)
		{
			string padded = value.Replace('-', '+').Replace('_', '/');

			switch (padded.Length % 4)
			{
				case 2:
					padded += "==";
					break;

				case 3:
					padded += "=";
					break;
			}

			return Convert.FromBase64String(padded);
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
				PlayerPrefs.SetInt(PlayerProfile.Key(PrefsKey), (int)value);
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
			if (!PlayerPrefs.HasKey(PlayerProfile.Key(PrefsKey)))
			{
				return MultiplayerMode.Off;
			}

			int raw = PlayerPrefs.GetInt(PlayerProfile.Key(PrefsKey), (int)MultiplayerMode.Off);

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