using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// Sends the host's world to anybody who joins.
	/// </summary>
	/// <remarks>
	/// Two things here are deliberate and both are about the frame.
	///
	/// The export happens once, not once per joiner, and is then shared. A
	/// world is a couple of megabytes and exporting it means walking the
	/// whole engine; doing that again for the second player would double a
	/// cost for no benefit, and - worse - could produce two slightly different
	/// worlds if anything moved in between, which is the exact bug this whole
	/// exercise exists to avoid.
	///
	/// The chunks go out across frames rather than in a burst. Sending all of
	/// them at once would be a few dozen blocking socket writes back to back
	/// and would be felt as a hitch at precisely the moment somebody else
	/// joins, which is a moment the host is also playing. Chunky was chosen
	/// for exactly this reason and it would be silly to undo it here by
	/// emptying the queue in one frame.
	/// </remarks>
	public sealed class WorldSnapshotSender : MonoBehaviour
	{
		private const string ObjectName = "MultiplayerWorldSender";

		/// <summary>
		/// Pieces sent per frame.
		/// </summary>
		/// <remarks>
		/// Four of 64 KB is 256 KB a frame - a few short writes, comfortably
		/// inside a frame, and about 40 frames for a 2.4 MB world.
		/// </remarks>
		private const int ChunksPerFrame = 4;

		private static WorldSnapshotSender instance;

		/// <summary>The host's world, exported once and shared between joiners.</summary>
		private byte[] cachedSnapshot;

		/// <summary>Which scenario the cached snapshot belongs to.</summary>
		private string cachedKey;
		private int nextTransferId = 1;

		/// <summary>
		/// Faction and shuttle already made for each player, by token.
		/// </summary>
		/// <remarks>
		/// Remembered so a rejoining player gets their existing faction back
		/// rather than a second one. Making a new faction on every join would
		/// leave a trail of abandoned players in the world, each with a ship
		/// nobody is flying.
		/// </remarks>
		private readonly Dictionary<string, int[]> madeFor = new Dictionary<string, int[]>();

		/// <summary>Which world those faction ids belong to.</summary>
		/// <remarks>
		/// They are ids into one world, and a new game is a different one with
		/// entirely different things at those ids. Remembering them across a
		/// world change handed a rejoining player somebody else's faction -
		/// a named one, with its own ship already in it - and told them their
		/// shuttle did not exist.
		///
		/// The sibling of the snapshot cache, which had exactly this bug and
		/// was fixed a moment earlier. Fixing one stale cache and missing the
		/// other standing right next to it is its own kind of lesson.
		/// </remarks>
		private System.Int64 madeForWorld;

		private readonly Dictionary<RemotePlayer, Queue<byte[]>> inFlight =
			new Dictionary<RemotePlayer, Queue<byte[]>>();

		public static WorldSnapshotSender Instance
		{
			get
			{
				if (instance == null)
				{
					Install();
				}

				return instance;
			}
		}

		/// <summary>How many pieces are still queued, across all joiners.</summary>
		public int PendingChunks
		{
			get
			{
				int total = 0;

				foreach (Queue<byte[]> queue in inFlight.Values)
				{
					total += queue.Count;
				}

				return total;
			}
		}

		private static void Install()
		{
			if (instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			instance = host.AddComponent<WorldSnapshotSender>();

			// The broadcaster goes on the same object. It used to be nobody's
			// job: the class existed, the applier on the other end expected
			// it, and nothing in the codebase ever added it to a GameObject.
			// So the host never sent a single unit state and every joiner
			// watched a world standing completely still - which reads as
			// "the sync does not work" rather than as "the sender was never
			// switched on".
			host.AddComponent<UnitStateBroadcaster>();

			// Same object, same reason. A joiner's credits and hold come
			// from the host too, and without this the client shows a balance
			// it has already spent for as long as the session lasts.
			host.AddComponent<FactionStatePusher>();
		}

		private void Awake()
		{
			instance = this;
		}

		private void OnDestroy()
		{
			if (instance == this)
			{
				instance = null;
			}
		}

		private void Update()
		{
			if (inFlight.Count == 0)
			{
				return;
			}

			// Copied because a player can leave between one send and the
			// next, and mutating a dictionary while walking it throws.
			var gone = new List<RemotePlayer>();

			foreach (KeyValuePair<RemotePlayer, Queue<byte[]>> entry in inFlight)
			{
				RemotePlayer player = entry.Key;
				Queue<byte[]> queue = entry.Value;

				for (int i = 0; i < ChunksPerFrame && queue.Count > 0; i++)
				{
					if (!player.Channel.IsConnected)
					{
						gone.Add(player);
						break;
					}

					player.Channel.Send(FramedMessage.Create(
						ProtocolVersion.WorldSnapshotChunkMessageId, queue.Dequeue()));
				}

				if (!player.Channel.IsConnected)
				{
					gone.Add(player);
				}
			}

			foreach (RemotePlayer player in gone)
			{
				inFlight.Remove(player);
			}

			if (inFlight.Count == 0)
			{
				// The world has moved on since this was exported. Dropping it
				// here means the next joiner gets a current one rather than
				// the world as it was when the last one joined.
				cachedSnapshot = null;
				cachedKey = null;
			}
		}

		/// <summary>
		/// Sends the current world to one freshly admitted player.
		/// </summary>
		/// <param name="reason">What happened, for the host's log to read.</param>
		public void SendTo(RemotePlayer player, Action<string> reason)
		{
			if (player == null)
			{
				reason?.Invoke("no player");
				return;
			}

			EngineASX engine = EngineASX.Instance;
			HostSession session = MultiplayerHostService.Session;

			if (engine == null || engine.World == null || session == null)
			{
				// Admitted from the menu, so there is nothing to arrive into.
				// The descriptor still goes out, with no scenario, so the
				// joiner is told rather than left waiting for bytes that will
				// never come.
				SendDescriptor(player, MultiplayerHostService.ScenarioId, -1, -1);

				reason?.Invoke("no world to send - the host is not in a game");
				return;
			}

			// Read now, not from whatever was true when hosting started: the
			// host may have gone back to the menu and begun a different game
			// since, and this value keys the cache as well as the descriptor.
			int scenarioId = MultiplayerHostService.CurrentScenarioId();

			// The faction is made BEFORE the export, not after. The save models
			// exactly one local player, so a second player cannot appear by
			// being loaded - it has to be in the snapshot. Making it here also
			// means its ids come from the host's counter, so both copies
			// number the shuttle the same way with nothing to reconcile.
			int factionId = -1;
			int shipId = -1;

			long worldId = CurrentWorldId();

			if (madeForWorld != worldId)
			{
				madeFor.Clear();
				madeForWorld = worldId;
			}

			if (!madeFor.TryGetValue(player.Identity, out int[] knownFor))
			{
				JoinerPreferencesMessage wanted = session.PreferencesFor(player.Identity);
				OpenFrontier.IP.Engine.GameController controller = OpenFrontier.IP.Engine.GameController.Instance;

				string[] resolved = wanted.Resolved(
					controller.DefaultPilotName, controller.DefaultPilotTitle,
					controller.DefaultFactionName, controller.DefaultFactionShortName);

				OpenFrontier.IP.Engine.Factions.Faction made = JoinerFactionFactory.Create(
					engine, player.Identity, resolved[0], resolved[1], resolved[2], resolved[3],
					out OpenFrontier.IP.Engine.Unit shuttle, out string makeError);

				if (made == null)
				{
					Debug.LogError("Multiplayer: no faction could be made for " + player.Identity + " - " + makeError);
				}
				else
				{
					factionId = made.UniqueId;
					shipId = shuttle == null ? -1 : shuttle.UniqueId;
					madeFor[player.Identity] = new int[] { factionId, shipId };

					// The world just changed, so what was exported last time is
					// no longer what this player should receive.
					cachedSnapshot = null;
					cachedKey = null;

					Debug.Log("Multiplayer: made faction " + factionId + " ("
						+ resolved[2] + ") with shuttle " + shipId + " for " + player.Identity);
				}
			}
			else
			{
				// Already made for this player, from an earlier join. Handing
				// back the same faction is what stops every rejoin leaving
				// another abandoned ship behind.
				factionId = knownFor[0];
				shipId = knownFor[1];
			}

			SendDescriptor(player, scenarioId, factionId, shipId);

			// Reuse the export only while it is still the world this session
			// is playing. The key changes with the scenario, and the snapshot
			// is dropped entirely when the last joiner finishes, so a host
			// who plays on for ten minutes does not keep sending the world as
			// it was when the first person joined.
			string key = Key(scenarioId);

			if (cachedSnapshot == null || cachedKey != key)
			{
				if (!WorldSnapshotBuilder.TryBuild(engine, out byte[] snapshot, out string error))
				{
					reason?.Invoke("could not export the world: " + error);
					return;
				}

				cachedSnapshot = snapshot;
				cachedKey = key;
			}

			// A fresh transfer id per joiner. Reusing one would not corrupt
			// this transfer so much as merge it with the previous one on the
			// far end, which stitches two worlds together and hands back
			// something that loads and is wrong.
			int transferId = nextTransferId++;

			var queue = new Queue<byte[]>();

			foreach (byte[] chunk in SnapshotChunker.Chunk(cachedSnapshot, transferId))
			{
				queue.Enqueue(chunk);
			}

			inFlight[player] = queue;

			reason?.Invoke("sending " + queue.Count + " pieces ("
				+ cachedSnapshot.Length + " bytes, transfer " + transferId + ")");
		}

		private void SendDescriptor(RemotePlayer player, int scenarioId, int factionId, int shipId)
		{
			var descriptor = new WorldDescriptorMessage
			{
				ScenarioInfoId = scenarioId,
				GameTypeId = (int)OpenFrontier.IP.Engine.UniverseGameType.Newcomer,
				MyFactionId = factionId,
				MyShipId = shipId,
				PilotName = string.Empty
			};

			player.Channel.Send(FramedMessage.Create(
				ProtocolVersion.WorldDescriptorMessageId, descriptor.ToBytes()));
		}

		/// <summary>
		/// A cache key that changes when the host moves to another scenario.
		/// </summary>
		/// <remarks>
		/// A different scenario means a different world, so the cached export
		/// is meaningless. The string form is because -1 and "no scenario" are
		/// different answers and an int would quietly conflate them.
		/// </remarks>
		private static long CurrentWorldId()
		{
			OpenFrontier.IP.Engine.WorldBase world = OpenFrontier.IP.Engine.EngineASX.Instance?.World;

			return world == null ? 0L : world.GetEntityId().GetHashCode();
		}

		private static string Key(int scenarioId)
		{
			// Paired with the world's own identity, so loading another save of
			// the SAME scenario - which is the ordinary case of starting a new
			// game - still invalidates the cache. The scenario id alone cannot
			// tell those apart, and sending a joiner somebody else's galaxy
			// because two worlds share a scenario is exactly the bug.
			return scenarioId + ":" + CurrentWorldId();
		}
	}
}