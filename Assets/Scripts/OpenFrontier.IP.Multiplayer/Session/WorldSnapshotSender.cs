using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine;
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

			if (engine == null || engine.World == null)
			{
				// Admitted from the menu, so there is nothing to arrive into.
				// The descriptor still goes out, with no scenario, so the
				// joiner is told rather than left waiting for bytes that will
				// never come.
				SendDescriptor(player, MultiplayerHostService.ScenarioId, string.Empty);

				reason?.Invoke("no world to send - the host is not in a game");
				return;
			}

			int scenarioId = MultiplayerHostService.ScenarioId;

			SendDescriptor(player, scenarioId, string.Empty);

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

		private void SendDescriptor(RemotePlayer player, int scenarioId, string pilotName)
		{
			var descriptor = new WorldDescriptorMessage
			{
				ScenarioInfoId = scenarioId,
				GameTypeId = (int)OpenFrontier.IP.Engine.UniverseGameType.Newcomer,
				PilotName = pilotName ?? string.Empty
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
		private static string Key(int scenarioId) => scenarioId.ToString();
	}
}