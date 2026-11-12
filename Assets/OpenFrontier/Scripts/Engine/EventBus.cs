using System;
using System.Collections.Generic;

namespace OpenFrontier.Engine
{
	/// <summary>Marker for engine events. Structs only: hot paths carry no
	/// managed payloads, and journal/notification text is formatted OUTSIDE
	/// the hot path by whoever renders it (standing rule) — the bus moves
	/// facts, never strings.</summary>
	public interface IEngineEvent { }

	/// <summary>
	/// The engine event bus: typed publish/subscribe over struct payloads,
	/// synchronous, in dispatch order, main thread. Notification text
	/// (toasts, journal lines) is a LISTENER's job — the legacy engine
	/// pushed strings through UIController from deep inside sim code, and
	/// that coupling is exactly what this replaces. Handlers subscribing
	/// during a dispatch take effect on the next publish; unsubscribing
	/// during one is deferred to the end of the outermost dispatch, so
	/// index-based iteration never walks an unstable list.
	/// </summary>
	public sealed class EventBus
	{
		private readonly Dictionary<Type, List<Delegate>> handlersByType =
			new Dictionary<Type, List<Delegate>>();

		private List<Delegate> pendingRemovals;
		private int dispatchDepth;

		public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEngineEvent
		{
			if (handler == null)
			{
				return;
			}

			if (!handlersByType.TryGetValue(typeof(TEvent), out List<Delegate> list))
			{
				list = new List<Delegate>();
				handlersByType[typeof(TEvent)] = list;
			}

			// Double-subscribe guard: the double-tick lesson, event edition.
			if (list.Contains(handler))
			{
				return;
			}

			list.Add(handler);
		}

		public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEngineEvent
		{
			if (handler == null)
			{
				return;
			}

			if (!handlersByType.TryGetValue(typeof(TEvent), out List<Delegate> list)
				|| !list.Contains(handler))
			{
				return;
			}

			if (dispatchDepth > 0)
			{
				// Deferred: the dispatch loop above us is iterating this list.
				if (pendingRemovals == null)
				{
					pendingRemovals = new List<Delegate>();
				}

				pendingRemovals.Add(handler);
				return;
			}

			list.Remove(handler);
		}

		/// <summary>Publish to every subscriber of this event type, in
		/// subscription order. No allocation in steady state; the struct
		/// travels by value, nothing boxes.</summary>
		public void Publish<TEvent>(in TEvent evt) where TEvent : struct, IEngineEvent
		{
			if (!handlersByType.TryGetValue(typeof(TEvent), out List<Delegate> list)
				|| list.Count == 0)
			{
				return;
			}

			dispatchDepth++;
			try
			{
				// Capture the count: handlers added DURING this dispatch
				// join on the next publish, not this one.
				int count = list.Count;
				for (int i = 0; i < count; i++)
				{
					((Action<TEvent>)list[i])(evt);
				}
			}
			finally
			{
				dispatchDepth--;
				if (dispatchDepth == 0 && pendingRemovals != null && pendingRemovals.Count > 0)
				{
					for (int i = 0; i < pendingRemovals.Count; i++)
					{
						RemoveEverywhere(pendingRemovals[i]);
					}

					pendingRemovals.Clear();
				}
			}
		}

		private void RemoveEverywhere(Delegate handler)
		{
			foreach (KeyValuePair<Type, List<Delegate>> pair in handlersByType)
			{
				pair.Value.Remove(handler);
			}
		}
	}

	/// <summary>Raised once per world when its boot chain completes — the
	/// moment the load/pause protocol lifts to 1 (single authority).</summary>
	public readonly struct WorldLoadedEvent : IEngineEvent { }
}
