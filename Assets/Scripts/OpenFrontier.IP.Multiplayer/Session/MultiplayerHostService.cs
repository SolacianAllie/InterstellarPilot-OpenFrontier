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

		/// <summary>
		/// What the network's gateway said when asked to forward this session.
		/// </summary>
		/// <remarks>
		/// Reported rather than assumed. Whether a host is reachable from
		/// outside their own network is the single most useful thing to know
		/// when a join fails and cannot be worked out from inside the game,
		/// so it is surfaced on the session panel rather than left in a log.
		/// </remarks>
		public static UpnpResult PortMapping { get; private set; }

		/// <summary>Whether the current session's port is forwarded.</summary>
		public static bool PortIsForwarded => PortMapping.Status == UpnpStatus.Mapped;

		/// <summary>
		/// The id of the scenario this session is playing, or -1.
		/// </summary>
		/// <remarks>
		/// An id rather than the scenario itself, because the joining copy has
		/// the same scenarios installed and can look it up itself - the same
		/// thing LoadGameScreen already does when it opens a save. Sending the
		/// whole object would mean serialising a class that is meant to come
		/// from the game's own content, and would let a host describe a
		/// scenario its peer does not have.
		///
		/// Captured when the session starts rather than read from a save,
		/// because most of the ways to be in a world - a sandbox, a skirmish,
		/// anything started and not yet saved - have no save to read it from
		/// yet, and that is exactly when somebody might want to join them.
		/// </remarks>
		public static int ScenarioId { get; private set; } = -1;

		/// <summary>
		/// The scenario being played RIGHT NOW, not the one at startup.
		/// </summary>
		/// <remarks>
		/// Hosting outlives games. A host can start hosting in one game, go
		/// back to the menu and start another, all without the listener ever
		/// stopping - so the value captured when hosting began describes a
		/// world that may not exist any more. It is used to key the snapshot
		/// cache, and a stale key means the next joiner is handed the previous
		/// player's galaxy while being told the new one's name.
		/// </remarks>
		public static int CurrentScenarioId()
		{
			return CaptureScenarioId();
		}

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

		private bool hadAWorld;

		/// <summary>
		/// Tells everybody to go home, once, when this host stops playing.
		/// </summary>
		/// <remarks>
		/// Once, because the main menu is also where a host waits between
		/// games and this runs every frame - without the flag they would be
		/// told to leave a session they had already left, over and over.
		///
		/// Sent before the socket closes, which is the only moment it can
		/// be. A host whose machine dies says nothing, and that is exactly
		/// the case a client reads as a lost connection instead.
		/// </remarks>
		private void SayGoodbyeIfLeaving()
		{
			OpenFrontier.IP.Engine.EngineASX engine = OpenFrontier.IP.Engine.EngineASX.Instance;
			bool inAWorld = engine != null && engine.World != null;

			if (inAWorld)
			{
				hadAWorld = true;
				return;
			}

			if (!hadAWorld || Session == null || Session.Players.Count == 0)
			{
				return;
			}

			hadAWorld = false;

			Session.Broadcast(ProtocolVersion.HostFarewellMessageId,
				new HostFarewellMessage { Reason = "The host has left the game." }.ToBytes());

			Debug.Log("Multiplayer: told " + Session.Players.Count
				+ " player(s) this host is leaving");
		}

		private void Update()
		{
			SayGoodbyeIfLeaving();
			ApplyPreference();
			UpdateAvailability();
			ApplyWorldRestrictions();
		}

		/// <summary>
		/// Tells the world which conveniences it has to do without.
		/// </summary>
		/// <remarks>
		/// Follows the option rather than whether a listener happens to be up, so
		/// switching multiplayer off mid-session gives the conveniences back
		/// immediately rather than at the next scene load.
		///
		/// God mode and hypersleep are single-player conveniences: one player
		/// using either in a world others are in would change the game for them
		/// without them having done anything.
		/// </remarks>
		private static void ApplyWorldRestrictions()
		{
			OpenFrontier.IP.Engine.UniverseWorld.WorldPlayerPermissions.SharedSession =
				MultiplayerSettings.Enabled;
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

			IPAddressSelection.ChooseEndpoints(
				out IPAddress bindAddress,
				out IPAddress advertisedAddress,
				out string problem);

			if (advertisedAddress == null)
			{
				// Nothing to listen on. Reported, not thrown: the game is fully
				// playable on its own, so this must never stop it starting.
				LastProblem = "Could not open a network connection: "
					+ (problem ?? "no usable network address was found.")
					+ " Single-player play is unaffected.";

				Debug.LogWarning("Multiplayer: " + LastProblem);
				return;
			}

			// The preferred ports in order, so the join code is the same one
			// session after session and a saved host entry keeps working. A
			// range rather than a single port: anything else on the machine
			// holding one number should cost a number, not the session.
			//
			// Port 0 is the last resort - any free port - because hosting on a
			// random one beats not hosting, even though it cannot be forwarded
			// and its code goes stale on the next restart.
			int[] preferredPorts = IPAddressSelection.PreferredPorts();

			HostSession session = null;

			// The reason each attempt failed is captured rather than logged:
			// eight failures in a row would bury the one that matters, and the
			// only interesting outcome is whether the whole range was refused.
			string lastPortFailure = null;

			foreach (int candidate in preferredPorts)
			{
				session = HostSession.Start(bindAddress, advertisedAddress, candidate, reason =>
				{
					lastPortFailure = reason;
				}, line => Debug.Log(line));

				if (session != null)
				{
					break;
				}
			}

			if (session == null)
			{
				if (lastPortFailure != null)
				{
					Debug.LogWarning("Multiplayer: every preferred port was taken (" + lastPortFailure + ")");
				}

				session = HostSession.Start(bindAddress, advertisedAddress, 0, reason =>
				{
					LastProblem = reason;
					Debug.LogWarning("Multiplayer: " + reason);
				}, line => Debug.Log(line));

				if (session != null)
				{
					Debug.LogWarning(
						"Multiplayer: hosting on " + session.Port + " instead. It will not be "
						+ "reachable from outside this network, and its code changes whenever "
						+ "the game restarts.");
				}
			}

			if (session == null)
			{
				LastProblem = "Could not start hosting.";
				return;
			}

			Session = session;
			LastProblem = null;

			// The handshake cannot move ships itself, so it is told who can.
			// The faction adopter is installed at startup rather than here,
			// because a player alone is still a player and their faction number
			// must not depend on whether anyone has joined.

			HostSession.ShipUploadHandler = HostShipUpload.Apply;
			HostSession.ShipMayBeClaimed = HostShipUpload.MayBeClaimed;


			// Re-read every time hosting starts, not cached once: a host can
			// stop, load a different scenario and start again, and a stale id
			// would send a joiner to a scenario nobody is playing.
			ScenarioId = CaptureScenarioId();

			// The join is only half finished until the joiner has been given
			// a world to arrive in. Without this the handshake succeeds, the
			// joiner sits connected, and nothing further ever arrives - which
			// looks from the joiner's side exactly like a host that has gone
			// quiet.
			session.PlayerJoined += player =>
			{
				WorldSnapshotSender.Instance.SendTo(player, what =>
					Debug.Log("Multiplayer: " + (player.DisplayName ?? player.Identity) + " - " + what));
			};

			// Raised rather than left for listeners to poll for. Hosting starts
			// from this component's Update, and a listener waiting on its own
			// update depends on frame order and on its screen still being the
			// current one - both of which fail silently, showing a dialog that
			// never catches up with a code that is already there.
			HostingStarted?.Invoke(Session.JoinCode);

			AskTheRouterToForward();

			// The bound port, not the requested one: the session asks the OS
			// for any free port, so logging the request prints a port of zero -
			// true, and useless when a join will not open.
			//
			// The advertised address and the endpoint it listens on are now two
			// different things, so both are named: the address goes into the code,
			// and 0.0.0.0 is what every interface is bound to.
			Debug.Log("Multiplayer: hosting as " + PlayerProfile.Describe()
				+ " (pid " + System.Diagnostics.Process.GetCurrentProcess().Id + ")"
				+ ", code advertises " + advertisedAddress
				+ ", listening on every interface at " + Session.Endpoint
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

			// Before anything else: a forward left behind is a hole in the
			// player's firewall that outlives the game, and most routers keep
			// one until something removes it - which, for plenty of them, is
			// nothing at all until the router is rebooted.
			PortMapping = new UpnpResult { Status = UpnpStatus.NotAttempted };
			UpnpPortMapper.Remove();
		}

		/// <summary>
		/// Asks the gateway to forward this session's port, if it can.
		/// </summary>
		/// <remarks>
		/// Entirely on its own thread and entirely optional. A router that is
		/// slow, absent, or refuses has to cost the player nothing at all, so
		/// nothing here can fail the session or hold up the code it is about
		/// to be handed out on. The result is reported rather than assumed:
		/// a player told they are reachable who is not has been told a lie.
		/// </remarks>
		/// <summary>
		/// The scenario currently being played, if there is one.
		/// </summary>
		/// <remarks>
		/// Null-safe throughout: this runs when hosting starts, which can be
		/// while still in the main menu, before any world exists at all.
		/// </remarks>
		private static int CaptureScenarioId()
		{
		OpenFrontier.IP.Engine.WorldBase world = OpenFrontier.IP.Engine.EngineASX.Instance?.World;
		OpenFrontier.IP.Engine.ScenarioInfo scenario = world?.ScenarioInfo;

		return scenario == null ? -1 : scenario.UniqueId;
		}

		private static void AskTheRouterToForward()
		{
			HostSession session = Session;

			if (session == null)
			{
				return;
			}

			PortMapping = new UpnpResult { Status = UpnpStatus.Asking, Port = session.Port };

			// Only the private address is useful to a router. On a machine holding
			// a public address the advertised one is not something a gateway can
			// forward to, and asking it to would just be refused.
			IPAddressSelection.TryGetPrivateAddress(out IPAddress internalAddress);

			UpnpPortMapper.Map(session.Port, internalAddress, result =>
			{
				// A late reply from a session that has already gone must not be
				// allowed to resurrect the panel, or leave a forward behind that
				// nothing will take down again.
				if (Session == null)
				{
					UpnpPortMapper.Remove();
					return;
				}

				PortMapping = result;

				if (!string.IsNullOrEmpty(result.Detail))
				{
					Debug.Log("Multiplayer: " + result.Detail);
				}
			});
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
