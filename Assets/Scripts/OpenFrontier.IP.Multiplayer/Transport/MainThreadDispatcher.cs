using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Moves work from background threads onto the main thread.
	///
	/// Sockets necessarily read and write off the main thread, but the
	/// simulation may only be touched from the main thread - Unity objects are
	/// not thread-safe, and engine state touched from two threads would race.
	/// So background threads do nothing but decode bytes into plain messages and
	/// post them here; the main thread drains the queue once per frame.
	///
	/// Queued items are therefore expected to be plain data (structs holding
	/// primitives, or engine objects that are only *stored* here and only
	/// dereferenced after being dequeued on the main thread).
	/// </summary>
	public static class MainThreadDispatcher
	{
		private static readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
		private static int mainThreadId;

		/// <summary>
		/// Records the main thread. Called from the multiplayer component's
		/// Awake, which Unity guarantees runs on the main thread.
		/// </summary>
		public static void RegisterMainThread()
		{
			mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
		}

		/// <summary>Whether the caller is on the main thread.</summary>
		public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;

		/// <summary>
		/// Queues work to run on the main thread. Safe from any thread.
		///
		/// Work posted while already on the main thread runs immediately rather
		/// than waiting a frame, which avoids a surprising one-frame delay for
		/// code that is already in the right place.
		/// </summary>
		public static void Post(Action action)
		{
			if (action == null)
			{
				return;
			}

			if (IsMainThread)
			{
				action();
				return;
			}

			pending.Enqueue(action);
		}

		/// <summary>
		/// Runs everything queued since the last frame. Main thread only.
		/// </summary>
		public static void Drain()
		{
			if (!IsMainThread)
			{
				// Defensive: draining from a worker would defeat the purpose.
				return;
			}

			// Bounded so a flood of inbound traffic cannot lock the frame. Work
			// left over simply runs next frame, which is the correct behaviour
			// under load - dropping it would be worse.
			const int MaxActionsPerFrame = 512;

			int processed = 0;
			while (processed < MaxActionsPerFrame && pending.TryDequeue(out Action action))
			{
				processed++;

				try
				{
					action();
				}
				catch (Exception e)
				{
					// One bad message must not stop the queue draining, and must
					// not kill the frame.
					Debug.LogException(e);
				}
			}
		}

		/// <summary>
		/// Discards queued work. Used when a session ends so late messages from
		/// a closed connection cannot run against a torn-down session.
		/// </summary>
		public static void Clear()
		{
			while (pending.TryDequeue(out _))
			{
			}
		}
	}
}