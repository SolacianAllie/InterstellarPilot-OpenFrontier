using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.Gamemodes
{
	/// <summary>
	/// The single source of gamemode definitions. The NewGame screen asks
	/// here; it never enumerates modes itself (docs/gamemode-modding-design.md,
	/// Decision 1) - "we add a gamemode" and "a mod adds a gamemode" are the
	/// same act: something puts a definition in front of this registry.
	///
	/// Built-ins load once from Resources ("Gamemodes" - author definitions as
	/// assets under Assets/OpenFrontier/Resources/Gamemodes/). The public
	/// Register hook is reserved for the future ModLoader (Decision 6): it
	/// will load a mod's bundle and hand each GamemodeDefinition here before
	/// the menu opens. The UI never changes for any of it.
	///
	/// Conflict rule: a duplicate Id keeps whichever definition is already
	/// present and logs a warning. Built-ins always load FIRST (Register
	/// forces EnsureLoaded itself), so "built-in wins" is deterministic, not
	/// luck-of-order - a mod can never register ahead of the core set and
	/// claim an Id by arriving early. Override semantics ("a mod rebalances a
	/// core mode") are deliberately reserved, not designed.
	///
	/// DeveloperOnly definitions are filtered at load: visible in the editor
	/// and development builds, absent from release - so a dev gamemode can
	/// never leak into a shipped build's UI (the "diagnostics do not ship"
	/// rule, menu edition).
	///
	/// Loading is lazy on first access: Resources.LoadAll is a native call,
	/// and native calls in static initializers poison the whole type (the
	/// TypeInitializationException trap in AGENTS.md).
	/// </summary>
	public static class GamemodeRegistry
	{
		private static readonly List<GamemodeDefinition> modes = new List<GamemodeDefinition>();

		private static bool loaded;

		/// <summary>
		/// Every visible definition - built-ins plus anything mods registered,
		/// sorted for display, developer-only entries filtered outside the
		/// editor and development builds. A live view: re-read it where needed
		/// rather than holding it, because realtime mod toggles change it.
		/// </summary>
		public static IReadOnlyList<GamemodeDefinition> Modes
		{
			get
			{
				EnsureLoaded();
				return modes;
			}
		}

		/// <summary>
		/// Adds a definition to the visible set. Reserved for the future
		/// ModLoader - built-ins never arrive through here. Forces the
		/// built-in load first so the conflict rule stays deterministic.
		/// </summary>
		public static void Register(GamemodeDefinition definition)
		{
			EnsureLoaded();

			if (definition == null)
			{
				Debug.LogWarning("[GamemodeRegistry] Register called with a null definition.");
				return;
			}

			if (definition.DeveloperOnly && !DeveloperBuild)
			{
				return;
			}

			if (IsDuplicate(definition.Id))
			{
				Debug.LogWarning($"[GamemodeRegistry] Id '{definition.Id}' is already registered - keeping the existing definition, ignoring '{definition.name}'.");
				return;
			}

			modes.Add(definition);
			SortForDisplay();
		}

		private static void EnsureLoaded()
		{
			if (loaded)
			{
				return;
			}

			loaded = true;

			GamemodeDefinition[] builtIns = Resources.LoadAll<GamemodeDefinition>("Gamemodes");

			for (int i = 0; i < builtIns.Length; i++)
			{
				GamemodeDefinition mode = builtIns[i];

				if (mode.DeveloperOnly && !DeveloperBuild)
				{
					continue;
				}

				if (IsDuplicate(mode.Id))
				{
					// Two authored assets sharing an Id is an authoring
					// mistake; the first wins and the second is skipped loudly.
					Debug.LogWarning($"[GamemodeRegistry] Built-in Id '{mode.Id}' is duplicated in Resources - skipping '{mode.name}'.");
					continue;
				}

				modes.Add(mode);
			}

			SortForDisplay();
		}

		private static bool IsDuplicate(string id)
		{
			for (int i = 0; i < modes.Count; i++)
			{
				if (modes[i].Id == id)
				{
					return true;
				}
			}

			return false;
		}

		private static void SortForDisplay()
		{
			modes.Sort(CompareForDisplay);
		}

		private static int CompareForDisplay(GamemodeDefinition a, GamemodeDefinition b)
		{
			if (a.SortOrder != b.SortOrder)
			{
				return a.SortOrder.CompareTo(b.SortOrder);
			}

			return string.CompareOrdinal(a.Title, b.Title);
		}

		private static bool DeveloperBuild
		{
			get { return Application.isEditor || Debug.isDebugBuild; }
		}
	}
}
