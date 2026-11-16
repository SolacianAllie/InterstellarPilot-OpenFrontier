#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Debounced editor preview scheduling shared by the galaxy renderers.
	/// A component registers its apply/rebuild callback after an inspector or
	/// asset change; later changes within the delay postpone it. The tick runs
	/// on EditorApplication.delayCall, so it fires when the drag settles even
	/// if the Scene view never repaints (Update-based debounces can stall
	/// waiting for a repaint that does not come).
	///
	/// Editor-only by construction: the whole file is stripped from players,
	/// and the only callers sit inside #if UNITY_EDITOR blocks.
	/// </summary>
	internal static class GalaxyEditPreview
	{
		struct Entry
		{
			public Action Action;
			public double Due;
		}

		static readonly List<Entry> Entries = new List<Entry>();
		static bool _ticking;

		/// <summary>Schedule (or postpone) the action by delaySeconds.</summary>
		public static void Schedule(Action action, float delaySeconds)
		{
			if (action == null) return;
			double due = EditorApplication.timeSinceStartup + Math.Max(0.05f, delaySeconds);
			for (int i = 0; i < Entries.Count; i++)
			{
				if (!Entries[i].Action.Equals(action)) continue;
				Entry entry = Entries[i];
				entry.Due = due;
				Entries[i] = entry;
				EnsureTick();
				return;
			}
			Entries.Add(new Entry { Action = action, Due = due });
			EnsureTick();
		}

		/// <summary>Drop a pending action (a component disabled before it fired).</summary>
		public static void Cancel(Action action)
		{
			if (action == null) return;
			for (int i = Entries.Count - 1; i >= 0; i--)
			{
				if (Entries[i].Action.Equals(action)) Entries.RemoveAt(i);
			}
		}

		static void EnsureTick()
		{
			if (_ticking) return;
			_ticking = true;
			EditorApplication.delayCall += Tick;
		}

		static void Tick()
		{
			double now = EditorApplication.timeSinceStartup;
			bool waiting = false;
			for (int i = Entries.Count - 1; i >= 0; i--)
			{
				// An executed action may Cancel() a lower entry, shortening
				// the list under this loop; the index is no longer valid then.
				if (i >= Entries.Count) continue;
				Entry entry = Entries[i];
				// a destroyed component leaves a dead delegate behind
				if (entry.Action.Target is UnityEngine.Object target && target == null)
				{
					Entries.RemoveAt(i);
					continue;
				}
				if (now >= entry.Due)
				{
					Entries.RemoveAt(i);
					entry.Action();
				}
				else
				{
					waiting = true;
				}
			}
			if (waiting || Entries.Count > 0) EditorApplication.delayCall += Tick;
			else _ticking = false;
		}
	}
}
#endif
