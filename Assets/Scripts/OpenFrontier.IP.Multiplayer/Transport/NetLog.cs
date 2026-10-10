using System;
using System.IO;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Writes the networking diagnostics to a file of its own, unbuffered.
	/// </summary>
	/// <remarks>
	/// Unity buffers both Player.log and Editor.log and flushes them
	/// periodically and on exit. That is fine for debugging a crash and
	/// useless for watching a live session: for most of a run the file on
	/// disk says nothing about what the last thirty seconds did, and the
	/// only way to read it is to stop the game - which is exactly when you
	/// least want to.
	///
	/// It cost us an afternoon. Two of us read a log, concluded a broadcaster
	/// had stopped after one message, and started fixing something that was
	/// simply not visible yet.
	///
	/// Appended with an explicit flush per line. Opening and closing the file
	/// per line would be slow; holding it open and flushing is not, and this
	/// writes a few lines a second at most.
	/// </remarks>
	public static class NetLog
	{
		private const string HostPath = "/tmp/openfrontier-net-host.log";
		private const string ClientPath = "/tmp/openfrontier-net-client.log";

		private static StreamWriter writer;

		/// <summary>Whether the file is being written at all.</summary>
		public static bool Working => writer != null;

		/// <summary>Appends a line to whichever file this side uses.</summary>
		public static void Write(string message)
		{
			if (writer == null && !Open())
			{
				return;
			}

			try
			{
				writer.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
				writer.Flush();
			}
			catch (Exception)
			{
				// Diagnostics must never be the thing that breaks a game.
				writer = null;
			}
		}

		private static bool Open()
		{
			try
			{
				string path = IsClientSide ? ClientPath : HostPath;

				writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write))
				{
					AutoFlush = false
				};

				writer.WriteLine("--- session started at " + DateTime.Now + " ---");
				writer.Flush();

				return true;
			}
			catch (Exception e)
			{
				Debug.LogWarning("Multiplayer: could not open the network log - " + e.Message);
				return false;
			}
		}

		/// <summary>
		/// Whether this process is the joining side.
		/// </summary>
		/// <remarks>
		/// Decided by whether the multiplayer menu has been opened in this
		/// process at all, which is the only signal available before a join
		/// has been attempted. A host that also joins would be filed as a
		/// host, which is the safer of the two to get wrong: the host's file
		/// is the one that is always worth having.
		/// </remarks>
		private static bool IsClientSide => UI.MultiplayerScreen.MultiplayerScreenWasOpened;
	}
}