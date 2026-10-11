using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Owns this copy's place in someone else's session, for as long as it lasts.
	///
	/// It exists because the join used to be owned by the multiplayer menu
	/// screen. That works right up until the player enters a game: the screen
	/// is destroyed, its client goes with it, and the copy stops hearing
	/// anything - which is precisely when there is a world to hear about. The
	/// socket would still be open on the host's side while this player sat
	/// silently in a game, looking for all the world like a network fault.
	///
	/// Installed at startup alongside the host service, and for the same
	/// reason: it has to exist whether or not multiplayer is currently on, and
	/// asking every caller to remember to create it is how it would go missing.
	/// </summary>
	public sealed class MultiplayerClientService : MonoBehaviour
	{
		private const string ObjectName = "MultiplayerClient";

		public static MultiplayerClientService Instance { get; private set; }

		/// <summary>The session this copy is in, or null.</summary>
		public ClientSession Session { get; private set; }

		/// <summary>Raised once the host has admitted us.</summary>
		public event Action Connected;

		/// <summary>Raised with the reason when a join fails or a session ends.</summary>
		public event Action<string> Failed;

		/// <summary>Where incoming unit states go.</summary>
		public event Action<UnitStateMessage> UnitStateReceived;

		/// <summary>The host saying how much money a faction has.</summary>
		/// <remarks>
		/// Raised here rather than off the session directly, and that is the
		/// whole point. An applier is added to the same GameObject as this
		/// service at BeforeSceneLoad, so this service already exists when it
		/// subscribes - but the session does not, and will not for some time
		/// after. An applier that waits for the session therefore depends on
		/// being woken later, and two of them were: FactionStateApplier was
		/// written against the session in the first place and has never run,
		/// and DockedStateApplier copied it.
		///
		/// UnitStateApplier has always subscribed here and has always worked,
		/// which is the difference in one line.
		/// </remarks>
		public event Action<FactionStateMessage> FactionStateReceived;

		/// <summary>The host saying which hangar a ship is in.</summary>
		public event Action<DockedStateMessage> DockedStateReceived;

		/// <summary>The host saying a ship now exists.</summary>
		public event Action<NewUnitMessage> NewUnitReceived;

		/// <summary>The host saying a ship no longer exists.</summary>
		public event Action<UnitGoneMessage> UnitGoneReceived;

		/// <summary>How many unit states have arrived since play began.</summary>
		public int StatesReceived { get; private set; }

		/// <summary>Docked-state answers received from the host.</summary>
		public int DockedStatesReceived { get; private set; }

		/// <summary>New-ship announcements received from the host.</summary>
		public int NewUnitsReceived { get; private set; }

		/// <summary>Departures received from the host.</summary>
		public int UnitsGoneReceived { get; private set; }

		/// <summary>
		/// What the host said it was playing, once that has arrived.
		/// </summary>
		public WorldDescriptorMessage? HostWorld { get; private set; }

		/// <summary>Raised with a sentence for the player when joining resolves.</summary>
		public event Action<string> WorldReady;

		/// <summary>
		/// Raised when the session ends, whether or not anyone asked for it.
		/// </summary>
		/// <remarks>
		/// The same event as Failed, on purpose. A drop is not a different
		/// thing from a refusal as far as the screen is concerned: both mean
		/// the join is over and the row should stop saying connected. Two
		/// events for one outcome is how a screen ends up subscribing to
		/// only one of them and quietly going stale.
		/// </remarks>
		public event Action<string> Disconnected;

		/// <summary>Why the session ended, in words meant for the player.</summary>
		public enum Ending
		{
			/// <summary>Nothing has gone wrong.</summary>
			None,

			/// <summary>The host said goodbye and left on purpose.</summary>
			HostLeft,

			/// <summary>The host's machine stopped answering.</summary>
			Lost,

			/// <summary>This copy left the game.</summary>
			LeftGame,

			/// <summary>The join was refused or could not be made.</summary>
			Refused
		}

		/// <summary>How the last session ended.</summary>
		public Ending LastEnding { get; private set; } = Ending.None;

		/// <summary>Whether the host's world has finished arriving.</summary>
		public bool HasWorld { get; private set; }

		private SnapshotReassembler reassembler;

		/// <summary>Pieces seen for the transfer in progress, for the log.</summary>
		private int chunkCount;

		/// <summary>What this client believes about who owns what.</summary>
		public ClientOwnershipView OwnershipView { get; } = new ClientOwnershipView();

		public bool IsConnected => Session != null && Session.State == JoinState.Connected;

		/// <summary>Installs the service before any scene loads.</summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Install()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			DontDestroyOnLoad(host);
			host.AddComponent<MultiplayerClientService>();

			// The appliers go with it rather than into a scene, for the
			// reason this whole service exists: anything parented to a scene
			// stops existing the moment a player leaves the menu, and what it
			// exists to do only happens in a game.
			host.AddComponent<UnitStateApplier>();

			// And the faction one, which was written and then never fitted to
			// anything.
			//
			// The host has been pushing credits and cargo holds to every
			// client since faction state was built - FactionStatePusher sends
			// per player, change-detected - and ClientSession decoded it and
			// raised FactionStateReceived into a room with nobody in it. The
			// README said DONE. The diagram in the same README said NOT
			// BUILT, and the diagram was right.
			//
			// So a client's credits were whatever they were at join, and no
			// host-side spend ever reached it. Nothing looked broken: the
			// number on the screen was simply always the number from the
			// snapshot.
			//
			// Safe to install on a host too, because Attach() subscribes only
			// to a live ClientSession and a host has none.
			host.AddComponent<FactionStateApplier>();

			// And which hangars ships are in, which the host decides and every
			// copy applies. Before this, docked-ness was on nothing: a client
			// that docked put its ship in a hangar the host had never heard
			// of, and the two copies' UnitHangar.Items stayed different for
			// the rest of the session with nothing to reconcile them.
			host.AddComponent<DockedStateApplier>();

			// And the one thing on a client that creates something rather
			// than writing onto something: a ship the host says exists, built
			// from the same prefab the host built it from.
			host.AddComponent<NewUnitApplier>();
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				// Domain reload left the previous instance alive. The outgoing
				// one has to go, or two copies of this would both believe they
				// own the session.
				Destroy(gameObject);
				return;
			}

			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance != this)
			{
				return;
			}

			// Disconnected before the reference goes, so a listener elsewhere
			// sees the end of the session rather than a service that merely
			// stopped existing.
			Disconnect();
			Instance = null;
		}

		/// <summary>Joins a session by its code, replacing any current one.</summary>
		public void Join(string code)
		{
			Disconnect();

			ClientSession client = new ClientSession();

			client.Connected += () =>
			{
				Connected?.Invoke();
			};

			client.Failed += reason =>
			{
				Debug.LogWarning("Multiplayer: session ended - " + reason);
				EndSession(Ending.Refused, reason);
			};

			client.Disconnected += reason =>
			{
				Failed?.Invoke(reason);
				Disconnected?.Invoke(reason);
			};

			client.UnitStateReceived += states =>
			{
				StatesReceived++;
				UnitStateReceived?.Invoke(states);
			};

			client.FactionStateReceived += state => FactionStateReceived?.Invoke(state);

			client.DockedStateReceived += docked =>
			{
				DockedStatesReceived++;
				DockedStateReceived?.Invoke(docked);
			};

			client.NewUnitReceived += created =>
			{
				NewUnitsReceived++;
				NewUnitReceived?.Invoke(created);
			};

			client.UnitGoneReceived += gone =>
			{
				UnitsGoneReceived++;
				UnitGoneReceived?.Invoke(gone);
			};

			client.WorldDescriptorReceived += descriptor =>
			{
				HostWorld = descriptor;

				Debug.Log("Multiplayer: host describes its world - scenario "
					+ descriptor.ScenarioInfoId + (descriptor.HasJoiner
						? ", our faction is " + descriptor.MyFactionId
							+ " and our shuttle is " + descriptor.MyShipId
						: ", no faction made for us"));

				if (!descriptor.HasScenario)
				{
					Debug.Log("Multiplayer: the host did not name a scenario - "
						+ "probably hosting from the main menu, so there is nothing to join yet.");
					return;
				}

				// Checked here, while refusing costs nothing, rather than after
				// a couple of megabytes has crossed the network to fail the
				// same way.
				if (OpenFrontier.IP.Engine.GameController.Instance.GetScenarioInfoById(descriptor.ScenarioInfoId) == null)
				{
					Failed?.Invoke("You do not have the scenario the host is playing. "
						+ "It may be from a different version of the game.");
					Disconnect();
					return;
				}
			};

			client.WorldSnapshotChunkReceived += chunk => OnWorldChunk(chunk);

			client.PreferencesWanted += () => SendPreferences(client);

			client.HostLeft += reason =>
			{
				// Said before the socket closes, so this is the one ending
				// that can be told apart from a dead connection. "Go and
				// find them" and "try again" are different advice and a
				// client cannot tell them apart from a closed socket alone.
				EndSession(Ending.HostLeft,
					string.IsNullOrEmpty(reason) ? "The host has left the game." : reason);
			};

			client.OwnershipAssigned += assignment =>
			{
				// The whole handover, in both directions, through one channel
				// that already existed.
				//
				// The host names itself as owner when it takes a ship to fly
				// an order, and names the player when it hands it back. The
				// client reads both out of ApplyAssignment and changes who
				// flies the ship accordingly - no separate message, and
				// nothing for the client to have to guess at.
				OwnershipView.ApplyAssignment(assignment.UnitId, assignment.OwnerToken, client.Token);
			};

			Session = client;
			client.Join(code);
		}

		/// <summary>
		/// Keeps the host told about which ship this player is flying.
		/// </summary>
		/// <remarks>
		/// Driven by what the player is doing rather than by hooking the
		/// autopilot button, because the observable facts are unambiguous:
		/// the unit in the engine is the ship being flown, and
		/// PlayerUnitAutoPilotEnabled says whether the stick has been given
		/// back. A hook would have to know about every way control can
		/// change hands - docking, being boarded, a fleet taking over its
		/// ships - and would still be wrong about at least one of them.
		///
		/// Claims are sent once, when the ship first becomes this client's to
		/// fly. Resending every frame would be a message per frame per ship
		/// for no change, and the host would answer each one.
		/// </remarks>
		/// <summary>
		/// Tells the host what this player wants to be called.
		/// </summary>
		/// <remarks>
		/// Asked for rather than assumed, because those defaults are per
		/// profile: two copies of the game on one machine have different
		/// ones, and every player in a shared session wants their own. A host
		/// that named joiners after itself would be technically correct and
		/// obviously wrong.
		/// </remarks>
		private void SendPreferences(ClientSession client)
		{
			OpenFrontier.IP.Engine.GameController controller = OpenFrontier.IP.Engine.GameController.Instance;

			var preferences = new JoinerPreferencesMessage
			{
				PilotName = controller.DefaultPilotName,
				PilotTitle = controller.DefaultPilotTitle,
				FactionName = controller.DefaultFactionName,
				FactionShortName = controller.DefaultFactionShortName
			};

			client.Send(ProtocolVersion.JoinerPreferencesMessageId, preferences.ToBytes());
		}

		/// <summary>
		/// Sends ship state to the host, and installs the uploader.
		/// </summary>
		/// <remarks>
		/// The uploader lives here rather than being installed on its own so
		/// that "connected" means the same thing to every part of the client,
		/// which is the distinction that froze a host's own ships earlier.
		/// </remarks>
		/// <summary>
		/// Ends the session with a reason, and says so where a player sees it.
		/// </summary>
		/// <remarks>
		/// The popup is the point. A session that ends while the player is
		/// out in a world - which is where it ends, since that is what they
		/// were doing - otherwise produces no visible event at all: the ships
		/// simply stop moving, and the UI has nothing to say about why.
		/// </remarks>
		private void EndSession(Ending ending, string reason)
		{
			LastEnding = ending;

			Debug.Log("Multiplayer: session ended (" + ending + ") - " + reason);

			// Close the socket here, while the reason is still known.
			//
			// It used not to, and that left a client that had been told the
			// host left still holding a live session - sitting in the main
			// menu, reported as connected, with a socket nothing would ever
			// answer on. The session ended on paper and nowhere else.
			//
			// Safe from inside a message handler: the session checks for its
			// own disposal before it looks at another message.
			Disconnect();

			Failed?.Invoke(reason);
			Disconnected?.Invoke(reason);

			if (ending != Ending.LeftGame)
			{
				// Only for the other endings. When this copy left on purpose
				// it has somewhere to go already and is on its way.
				PublishNotice(reason, ending);

				GoBackToMenu();
			}
			else
			{
				// No menu transition to lose the popup in, so it can be said
				// now, where the player is still looking at the world they
				// just left.
				TellThePlayer(ending, reason);
			}
		}

		/// <summary>
		/// Returns to the main menu after a session that ended in a game.
		/// </summary>
		/// <remarks>
		/// Saying the session ended and then leaving the player standing in
		/// the host's world is worse than saying nothing: the world is frozen,
		/// nothing explains it, and the way out is a reload. The menu screen
		/// that used to handle this only exists in the menu, so nothing was
		/// listening at exactly the moment it mattered.
		/// </remarks>
		private static void GoBackToMenu()
		{
			try
			{
				OpenFrontier.IP.Engine.GameController.Instance.QuitToMainMenu();
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("Multiplayer: could not return to the main menu - " + e.Message);
			}
		}

		/// <summary>
		/// Leaves a reason for the next screen up, in the game's own words.
		/// </summary>
		/// <remarks>
		/// The game assembly owns the popup and the menu screen that would
		/// read it, and the networking assembly points at it rather than the
		/// other way round - a reference back would be a circle between two
		/// assemblies. Same shape as the authority question and the
		/// shared-session flag, for the same reason.
		/// </remarks>
		private static void PublishNotice(string reason, Ending ending)
		{
			OpenFrontier.IP.UI.SessionNotices.Publish(reason,
				ending == Ending.HostLeft
					? OpenFrontier.IP.UI.SessionNotices.NoticeKind.Warning
					: OpenFrontier.IP.UI.SessionNotices.NoticeKind.Error);
		}

		private static void TellThePlayer(Ending ending, string reason)
		{
			if (string.IsNullOrEmpty(reason))
			{
				return;
			}

			try
			{
				OpenFrontier.IP.UI.UIController.Instance?.ShowMessageBox(
					reason, OpenFrontier.IP.UI.Screens.MessageBoxButtons.Ok, null,
					ending == Ending.HostLeft
						? OpenFrontier.IP.UI.Screens.MessageBox.MessageBoxIcon.Warning
						: OpenFrontier.IP.UI.Screens.MessageBox.MessageBoxIcon.Error);
			}
			catch (System.Exception e)
			{
				// The UI is not always up - it may be a scene change, or a
				// headless probe. Losing the popup is not worth a crash.
				Debug.LogWarning("Multiplayer: could not show the session-ended message - " + e.Message);
			}
		}

		private void UpdateOwnership()
		{
			EnsureUploader();

			if (Session == null || Session.State != JoinState.Connected)
			{
				return;
			}

			OpenFrontier.IP.Engine.EngineASX engine = OpenFrontier.IP.Engine.EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			OpenFrontier.IP.Engine.Unit local = engine.LocalUnit;

			if (local == null || local.IsDestroyed)
			{
				return;
			}

			int unitId = local.UniqueId;

			// "The host is flying this" is the question, and it has two answers.
			//
			// The obvious one is whether this copy thinks the ship is on
			// autopilot. It is the right question when the client built the
			// order itself, and it is always NO here, because an order is sent
			// rather than built: TrySend returns before any DockOrder exists,
			// so IsPilottedByNpc has nothing to see and the client keeps
			// flying a ship the host is already flying.
			//
			// Three writers on one transform. The host's fleet flies it, this
			// copy simulates it, and this copy uploads what it simulated over
			// the host's position - which reads as the ship being thrown about
			// violently, on both machines, and neither of them able to say why.
			//
			// So the other answer is: did I just hand this ship to the host?
			// Which is what a client knows, and cannot learn from the autopilot
			// flag, because the flag is a thing about the local copy and the
			// order was never local.
			// Twice, for two different reasons. Once because this copy built the
			// order itself and can see the ship flown by an NPC. Once because the
			// host has said it is flying it - IsHeldByHost, which is the host
			// saying so in as many words. The second is what happens when the
			// order came FROM this copy and was never built here, so there was
			// never a local autopilot flag to notice.
			if (engine.PlayerUnitAutoPilotEnabled
				|| OwnershipView.IsHeldByHost(unitId))
			{
				// Onto autopilot: let it go, so the host flies it and starts
				// broadcasting it back. Until the host says so this client
				// keeps simulating, which looks exactly like the AI flying
				// it - the same trick the other direction relies on.
				if (OwnershipView.BelongsToMe(unitId))
				{
					OwnershipView.Release(unitId);
					Session.Send(ProtocolVersion.UnitOwnershipMessageId,
						UnitOwnershipMessage.Assign(unitId, null).ToBytes());
				}

				return;
			}

			if (OwnershipView.BelongsToMe(unitId))
			{
				return;
			}

			if (OwnershipView.Request(unitId))
			{
				// Asked, and the ship is already ours to fly: BelongsToMe is
				// true from here on, so the player is never waiting on this.
				Session.Send(ProtocolVersion.UnitOwnershipMessageId,
					UnitOwnershipMessage.Request(unitId).ToBytes());
			}
		}

		private ClientShipUploader uploader;

		private void EnsureUploader()
		{
			if (uploader != null)
			{
				return;
			}

			uploader = gameObject.AddComponent<ClientShipUploader>();
		}

		private bool hasHadAWorld;

		/// <summary>How long the world must be gone before we believe it.</summary>
		/// <remarks>
		/// A single frame of nothing is not leaving. Swapping ships, docking,
		/// jumping and coming back all pass through moments where the world
		/// is briefly not there, and a check that fired on one of them told
		/// the player they had left the game while they were still playing
		/// it. Quitting takes seconds and does not come back; that difference
		/// is the whole test.
		/// </remarks>
		private const float GoneGraceSeconds = 2f;

		private float worldGoneSince = -1f;

		/// <summary>
		/// Ends the session when this copy leaves its world.
		/// </summary>
		/// <remarks>
		/// Quit from the pause menu does not quit anything - it returns to the
		/// main menu - and nothing on that path touched the session. So a
		/// player who quit stayed connected and silent, and a host who quit
		/// left everybody they had admitted attached to a main menu they had
		/// just been ejected from.
		///
		/// Watched rather than hooked, because the engine assembly cannot
		/// reference this one, and every route back to the menu - quitting,
		/// loading another save, coming back from the title - goes through a
		/// different screen.
		///
		/// Only once a world has been seen, or the gap between joining and
		/// the world arriving would look like a departure and would tear the
		/// session down mid-transfer.
		/// </remarks>
		private void EndSessionIfWorldIsGone()
		{
			OpenFrontier.IP.Engine.EngineASX engine = OpenFrontier.IP.Engine.EngineASX.Instance;

			if (engine != null && engine.World != null)
			{
				hasHadAWorld = true;
				worldGoneSince = -1f;
				return;
			}

			if (!hasHadAWorld || Session == null || Session.State != JoinState.Connected)
			{
				return;
			}

			if (worldGoneSince < 0f)
			{
				worldGoneSince = Time.unscaledTime;
				return;
			}

			if (Time.unscaledTime - worldGoneSince < GoneGraceSeconds)
			{
				return;
			}

			worldGoneSince = -1f;

			EndSession(Ending.LeftGame, "You left the game, so the session ended.");

			Disconnect();
			hasHadAWorld = false;
		}

		private void Update()
		{
			EndSessionIfWorldIsGone();
			UpdateOwnership();
		}

		private void OnWorldChunk(byte[] chunk)
		{
			// One reassembler for the life of the session. A joiner that
			// abandoned a half-finished world and joined again gets a new
			// one from here, because Disconnect clears it.
			if (reassembler == null)
			{
				reassembler = new SnapshotReassembler();
			}

			if (!reassembler.Accept(chunk, out byte[] completed, out string error))
			{
				Debug.LogWarning("Multiplayer: discarding a world transfer - " + error);
				reassembler.Clear();
				return;
			}

			if (completed == null)
			{
				// A piece, not the world. Nothing to do, which is the normal
				// case for most of the arrival - but silence here is exactly
				// what a stalled transfer looks like, so the count is kept.
				chunkCount++;
				return;
			}

			Debug.Log("Multiplayer: the host's world arrived after " + chunkCount
				+ " pieces, " + completed.Length + " bytes");

			chunkCount = 0;
			HasWorld = true;

			ReceivedWorldLoader.Load(completed,
				HostWorld ?? default(WorldDescriptorMessage), reason =>
			{
				Debug.Log("Multiplayer: join finished - " + reason);
				WorldReady?.Invoke(reason);
			});
		}

		/// <summary>Sends this copy's own ship state to the host.</summary>
		public void SendUnitStates(List<UnitStateMessage> messages)
		{
			if (Session == null || Session.State != JoinState.Connected || messages == null)
			{
				return;
			}

			for (int i = 0; i < messages.Count; i++)
			{
				Session.Send(ProtocolVersion.UnitStateMessageId, messages[i].ToBytes());
			}
		}

		/// <summary>Leaves the session, if there is one.</summary>
		public void Disconnect()
		{
			if (Session == null)
			{
				return;
			}

			ClientSession leaving = Session;
			Session = null;

			// A half-received world is worse than none: it holds a megabyte
			// or two of buffer for a transfer nobody is waiting on any more.
			reassembler?.Clear();
			reassembler = null;
			HasWorld = false;
			OwnershipView.Clear();

			leaving.Dispose();
		}
	}
}
