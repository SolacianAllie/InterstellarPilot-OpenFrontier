using OpenFrontier.Gamemodes;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// What a launch carries across the scene boundary — the fresh shape of
	/// the legacy ScenarioLoadData/UniverseScenarioData pair, composed at
	/// launch confirm time from the authored gamemode definition. Grows as
	/// later milestones need it (seed settings, game type, pilot name) —
	/// additive fields, no migration.
	/// </summary>
	public sealed class LaunchPayload
	{
		/// <summary>The authored gamemode this launch serves. Null only for
		/// a bare boot (the stub scene opened directly in the editor).</summary>
		public GamemodeDefinition Gamemode;

		/// <summary>Scene the boot lands in. Serialized-string contract —
		/// SceneNames is the single source of spellings.</summary>
		public string TargetSceneName;

		/// <summary>Null = new game. Saves are a later milestone; the field
		/// exists so the boot branch already has its seam (the legacy
		/// save-vs-new branch keyed on exactly this).</summary>
		public string FullSaveGamePath;

		/// <summary>An explicit seed from UI — none exists in M1, so
		/// compose leaves it -1 = random-assigned at compose time.</summary>
		public int CustomSeed = -1;

		/// <summary>The integer world seed everything deterministic derives
		/// from — integer-only math (threading rule 2).</summary>
		public int ResolvedSeed;
	}
}
