using System;
using System.IO;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Turns bytes received from a host into a playable world.
	/// </summary>
	/// <remarks>
	/// The last step of joining, and the one with the most ways to go quietly
	/// wrong. Everything it does is written down here rather than left to the
	/// caller's discretion, because each of these has a specific failure that
	/// looks like nothing at all from the outside:
	///
	/// The snapshot is written to a file rather than parsed from memory,
	/// because EngineIO.Load only accepts a path. The file therefore has to
	/// exist, and the header has to be readable out of it, before anything
	/// can be attempted.
	///
	/// The scenario is taken from the snapshot's own header rather than from
	/// the descriptor that arrived earlier. That is not a preference: the
	/// header is the host's own record of what it exported, so it cannot
	/// disagree with the world that follows it. The descriptor is still worth
	/// having, because it arrives early enough to check for a missing
	/// scenario before a couple of megabytes has been paid for.
	///
	/// Nothing here is deleted afterwards. The temporary file is left in
	/// place because a load can fail deep inside the importer, and a failed
	/// attempt with the world still on disk is something a bug report can be
	/// built from - which is how the logs have earned their keep so far.
	/// </remarks>
	public static class ReceivedWorldLoader
	{
		/// <summary>
		/// Writes the received world out and loads it.
		/// </summary>
		/// <param name="finished">Called with what happened, for the player to read.</param>
		/// <param name="descriptor">
		/// What the host said about itself, including which faction is ours.
		/// </param>
		public static void Load(byte[] snapshot, WorldDescriptorMessage descriptor, Action<string> finished)
		{
			if (snapshot == null)
			{
				finished?.Invoke("The host sent an empty world.");
				return;
			}

			string path = null;

			try
			{
				Debug.Log("Multiplayer: writing " + snapshot.Length
					+ " received bytes to disk");

				path = WriteTemporary(snapshot);

				// Read back through the game's own header reader rather than
				// trusting the bytes just written. It checks the version
				// against what this build understands, and it is the same
				// check every save in the load list has already passed.
				Debug.Log("Multiplayer: reading the header back out of " + path);

				EngineSaveGameHeader header = SaveGameUtilities.LoadHeader(path);

				if (header == null)
				{
					finished?.Invoke("The world the host sent could not be read.");
					return;
				}

				if (!header.CanBeLoaded)
				{
					finished?.Invoke("The world the host sent is from a save version this game cannot load.");
					return;
				}

				ScenarioInfo scenario = GameController.Instance.GetScenarioInfoById(header.ScenarioInfoId);

				if (scenario == null)
				{
					// The one failure worth naming plainly, because nothing
					// else in the game can explain it to the player: they
					// simply do not have this scenario installed.
					finished?.Invoke("You do not have the scenario the host is playing (id "
						+ header.ScenarioInfoId + "). It may be from a different version of the game.");
					return;
				}

				Debug.Log("Multiplayer: loading the host's world from " + path
					+ " (scenario " + header.ScenarioInfoId + ", " + header.SaveVersion + ")");

				bool loaded = GameController.Instance.ScenarioLoader.TryLoadScenario(header);

				if (!loaded)
				{
					finished?.Invoke("The host's world could not be loaded.");
					return;
				}

				// Only now, once the world is in, can this copy be about the
				// joiner rather than the host. Doing it earlier would be
				// pointing at factions that are not loaded yet.
				if (JoinerIdentityAdopter.Adopt(descriptor.MyFactionId, descriptor.MyShipId, out string adopted))
				{
					Debug.Log("Multiplayer: joined as " + adopted);
					finished?.Invoke("Joined as " + adopted + ".");
				}
				else
				{
					// Said plainly rather than left to be discovered: the
					// player is otherwise the host's pilot, with no sign that
					// anything went wrong.
					Debug.LogWarning("Multiplayer: joined, but no faction of ours - " + adopted);

					finished?.Invoke("Joined, but could not become your own "
						+ "pilot: " + adopted + ".");
				}
			}
			catch (Exception e)
			{
				// Reaching here means the world arrived intact and still
				// would not load, which is a bug worth a full stack trace
				// rather than a message to the player.
				Debug.LogError("Multiplayer: loading the host's world failed: " + e);
				finished?.Invoke("The host's world could not be loaded: " + e.Message);
			}
		}

		private static string WriteTemporary(byte[] snapshot)
		{
			string directory = Application.temporaryCachePath;

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// Named for the moment it arrived rather than a fixed name, so
			// two joins in one session cannot overwrite each other - and so
			// the file left behind after a failure says which attempt it was.
			string path = Path.Combine(directory,
				"hostworld-" + DateTime.UtcNow.Ticks + ".sav");

			File.WriteAllBytes(path, snapshot);

			return path;
		}
	}
}