using System;
using System.Net;
using OpenFrontier.IP.Multiplayer;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// Hosts a session for as long as the player has multiplayer switched on.
	///
	/// The listener runs whenever the option is on, in the menu as much as in a
	/// game, so the join code is stable and can be read off a friend's screen
	/// before either of them has loaded anything. What changes with the menu is
	/// whether players are admitted: a host sitting in the main menu answers
	/// probes but refuses joins, and says so, because there is no game world for
	/// a joiner to arrive in and failing to explain that would look like the
	/// host having gone offline.
	/// </summary>
	public class MultiplayerHostService : MonoBehaviour
	{
		/// <summary>
		/// The live session, or null when not hosting.
		/// </summary>
		/// <remarks>
		/// Static because the join code is asked for from the main menu screen
		/// and from the in-game HUD, and neither of them owns this component.
		/// </remarks>
		public static HostSession Session { get; private set; }

		/// <summary>The code to share, or null when not hosting.</summary>
		public static string JoinCode => Session == null ? null : Session.JoinCode;

		/// <summary>
		/// Why hosting is not running, or null if it is.
		/// </summary>
		/// <remarks>
		/// Kept separate from "not hosting" because the two look identical from
		/// the join-code dialog otherwise: switched off, and switched on but the
		/// port could not be opened, are different things to tell a player.
		/// </remarks>
		public static string LastProblem { get; private set; }

		private float nextAttemptTime;

		/// <summary>Seconds between attempts while hosting is wanted but failing.</summary>
		private const float RetryIntervalSeconds = 10f;

		/// <summary>
		/// Puts a single instance on the persistent game object.
		/// </summary>
		/// <remarks>
		/// Created rather than authored onto a prefab. It has to live on the
		/// object that survives scene loads, and it has to exist whether or not
		/// multiplayer is switched on - a service that only appears once the
		/// option is enabled cannot turn the option on. Creating it here also
		/// means enabling the feature needs no prefab edit at all, which is what
		/// keeps this folder removable by deleting it.
		/// </remarks>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Install()
		{
			if (Instance != null)
			{
				return;
			}

			OpenFrontier.IP.Engine.GameController controller =
				OpenFrontier.IP.Engine.GameController.Instance;

			if (controller == null)
			{
				// No persistent object yet. The next scene load will try again;
				// hosting is not something that has to work before a menu exists.
				return;
			}

			MultiplayerHostService service =
				controller.gameObject.AddComponent<MultiplayerHostService>();

			Instance = service;
		}

		/// <summary>The live service, or null before it has installed.</summary>
		public static MultiplayerHostService Instance { get; private set; }

		/// <summary>
		/// Raised with the new join code each time hosting starts.
		/// </summary>
		/// <remarks>
		/// Static, because the party interested is the join code dialog, which
		/// does not own this component and should not have to find it.
		///
		/// Subscribers must unsubscribe: this outlives any single screen.
		/// </remarks>
		public static event Action<string> HostingStarted;

		private void OnEnable()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		private void Update()
		{
			ApplyPreference();
			UpdateAvailability();
		}

		private void OnDestroy()
		{
			StopHosting();

			// Only clear the shared state if this instance still owns it: a
			// replacement starting up must not have its session nulled by the
			// outgoing one shutting down.
			if (Instance == this)
			{
				Instance = null;
			}
		}

		/// <summary>
		/// Starts or stops hosting to match the option.
		/// </summary>
		private void ApplyPreference()
		{
			SyncPreferenceFromOptions();

			bool wanted = MultiplayerSettings.CanHost;

			if (!wanted)
			{
				StopHosting();
				return;
			}

			if (Session != null)
			{
				return;
			}

			// Failed attempts are retried on an interval rather than every
			// frame: a machine with no usable address would otherwise log and
			// re-probe sixty times a second.
			if (Time.unscaledTime < nextAttemptTime)
			{
				return;
			}

			nextAttemptTime = Time.unscaledTime + RetryIntervalSeconds;

			IPAddressSelection.ListenAll(out IPAddress address, out int port, out string problem);

			if (address == null)
			{
				// Nothing to listen on. Reported, not thrown: the game is fully
				// playable on its own, so this must never stop it starting.
				LastProblem = "Could not open a network connection: "
					+ (problem ?? "no usable network address was found.")
					+ " Single-player play is unaffected.";

				Debug.LogWarning("Multiplayer: " + LastProblem);
				return;
			}

			HostSession session = HostSession.Start(address, port, reason =>
			{
				LastProblem = reason;
				Debug.LogWarning("Multiplayer: " + reason);
			});

			if (session == null)
			{
				LastProblem = "Could not start hosting.";
				return;
			}

			Session = session;
			LastProblem = null;

			// Raised rather than left for listeners to poll for. Hosting starts
			// from this component's Update, and a listener waiting on its own
			// update depends on frame order and on its screen still being the
			// current one - both of which fail silently, showing a dialog that
			// never catches up with a code that is already there.
			HostingStarted?.Invoke(Session.JoinCode);

			Debug.Log("Multiplayer: hosting on " + address + ":" + port
				+ ", code " + Session.JoinCode);
		}

		/// <summary>
		/// Translates the Options toggle into the networking preference.
		/// </summary>
		/// <remarks>
		/// The two stores are deliberately kept separate, which makes this the
		/// only place they meet. Options live on PlayerOptions in the UI
		/// assembly; the networking layer reads MultiplayerSettings in this one.
		/// The UI assembly cannot reference this one - that would be circular -
		/// so the options screen cannot do the translation itself, and the
		/// multiplayer folder remains removable by deleting a single directory.
		///
		/// HostOrJoin rather than JoinOnly: this is one switch, and hosting is
		/// the point of turning it on.
		/// </remarks>
		private static void SyncPreferenceFromOptions()
		{
			OpenFrontier.IP.Engine.Core.PlayerOptions options =
				OpenFrontier.IP.Engine.GameController.Instance?.PlayerOptions;

			if (options == null)
			{
				return;
			}

			MultiplayerMode wanted = options.General_EnableMultiplayer
				? MultiplayerMode.HostOrJoin
				: MultiplayerMode.Off;

			if (MultiplayerSettings.Mode != wanted)
			{
				MultiplayerSettings.Mode = wanted;
			}
		}

		private void StopHosting()
		{
			if (Session == null)
			{
				return;
			}

			Session.Dispose();
			Session = null;
			LastProblem = null;
		}

		/// <summary>
		/// Admits players only while a game is loaded.
		/// </summary>
		/// <remarks>
		/// The listener deliberately stays up in the menu: the code has to
		/// remain readable and the host has to keep answering probes, so a
		/// friend can see "the host is in the main menu" rather than "the host is
		/// offline" - which are very different things to be told.
		/// </remarks>
		private void UpdateAvailability()
		{
			if (Session == null)
			{
				return;
			}

			bool inGame = IsInGame();

			// Recorded on the session as well as acted on, so a probe reply can
			// say *why* players are being refused rather than only that they are.
			Session.IsInMainMenu = !inGame;

			if (inGame)
			{
				Session.ResumeAcceptingPlayers();
			}
			else
			{
				Session.StopAcceptingPlayers();
			}
		}

		/// <summary>
		/// Whether a game world is loaded, as opposed to sitting in the menu.
		/// </summary>
		/// <remarks>
		/// Asked of the navigator rather than of the engine: EngineASX.Instance
		/// is assigned in Awake and never cleared, so it stays non-null after
		/// returning to the menu and cannot distinguish the two. The current
		/// screen can - every in-game screen derives from EngineScreen.
		/// </remarks>
		private static bool IsInGame()
		{
			OpenFrontier.IP.UI.Screens.ScreenBase current =
				OpenFrontier.IP.UI.UIController.Instance?.ScreenNavigator?.CurrentScreen;

			return current is OpenFrontier.IP.UI.EngineScreen;
		}
	}
}
