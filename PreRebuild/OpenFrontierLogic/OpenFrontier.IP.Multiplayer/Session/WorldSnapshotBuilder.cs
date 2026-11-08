using System;
using System.IO;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers;
using OpenFrontier.IP.SavedGames.V2.Model;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Turns the host's live world into something a joiner can load.
	/// </summary>
	/// <remarks>
	/// This is the same export the save button performs, pointed at memory
	/// instead of a file, and deliberately nothing else. Two things the file
	/// version does are skipped on purpose, because they are side effects on
	/// the HOST rather than part of the world:
	///
	/// It does not bump the save count, and it does not set
	/// LastLoadedData.FullSaveGamePath. Both belong to EngineIO.Save, which
	/// is about recording this game to this machine's disk; doing either here
	/// would have the host pointing its own autosave at a path that does not
	/// exist, or renumber its saves every time somebody joined.
	///
	/// The snapshot is taken once per joining player and then held, rather
	/// than re-exported as it is sent. Re-exporting would mean the bytes
	/// could differ between two chunks of one transfer, and would cost a full
	/// world walk per chunk instead of once.
	/// </remarks>
	public static class WorldSnapshotBuilder
	{
		/// <summary>
		/// Exports the given engine's world.
		/// </summary>
		/// <returns>False with a reason when there is no world to export.</returns>
		public static bool TryBuild(EngineASX engine, out byte[] snapshot, out string error)
		{
			snapshot = null;
			error = null;

			if (engine == null || engine.World == null)
			{
				// Hosting can be started from the main menu, before any world
				// exists. Saying so plainly beats exporting an empty one and
				// sending it.
				error = "There is no world to send yet.";
				return false;
			}

			try
			{
				var exporter = new SaveGameModelExporter();

				SavedGame savedGame = (SavedGame)exporter.Export(engine);
				savedGame.Header = exporter.ExportHeader(engine, EngineIO.SaveVersion, isAutosave: false);

				var writer = new SaveGameWriter(new HeaderWriter());

				using (var stream = new MemoryStream())
				{
					using (var binaryWriter = new BinaryWriter(stream))
					{
						writer.Write(binaryWriter, savedGame);
					}

					// Written inside the using, read after the stream closes,
					// so the buffer is complete rather than mid-flush.
					snapshot = stream.ToArray();
				}

				return true;
			}
			catch (Exception e)
			{
				// A failure here must not take the host's game down with it.
				// The host is playing normally and simply cannot admit this
				// one player, which is a far better outcome than an unhandled
				// export exception ending the session.
				snapshot = null;
				error = "The world could not be exported: " + e.Message;
				return false;
			}
		}
	}
}
