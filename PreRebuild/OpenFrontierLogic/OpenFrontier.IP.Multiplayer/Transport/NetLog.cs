using System;
using System.Collections.Generic;
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

		private static Dictionary<string, StreamWriter> writers;

		/// <summary>Whether the file is being written at all.</summary>
		public static bool Working => writers != null && writers.Count > 0;

		/// <summary>Appends a line to the host's file.</summary>
		public static void Host(string message) => Write(HostPath, "host", message);

		/// <summary>Appends a line to the client's file.</summary>
		public static void Client(string message) => Write(ClientPath, "client", message);

		private static void Write(string path, string role, string message)
		{
			if (writers == null)
			{
				writers = new Dictionary<string, StreamWriter>();
			}

			if (!writers.TryGetValue(path, out StreamWriter writer) || writer == null)
			{
				try
				{
					writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write));
					writer.WriteLine("--- " + role + " session started at " + DateTime.Now + " ---");
					writer.Flush();
					writers[path] = writer;
				}
				catch (Exception e)
				{
					Debug.LogWarning("Multiplayer: could not open " + path + " - " + e.Message);
					return;
				}
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
	}
}
