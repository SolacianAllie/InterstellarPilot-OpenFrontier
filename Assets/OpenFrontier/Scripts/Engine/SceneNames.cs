namespace OpenFrontier.Engine
{
	/// <summary>
	/// The single source of scene-name spellings (pinned decision: scene
	/// names are serialized strings — no enums, no GUID lookups). Every
	/// scene load, every build-settings list, every TargetSceneName field
	/// reads these constants, so a rename is one line and a typo is
	/// impossible at call sites. Names here must match the .unity file
	/// names EXACTLY — a mismatch fails at load time, not compile time,
	/// which is why the constants exist.
	/// </summary>
	public static class SceneNames
	{
		/// <summary>Build index 0 — the boot conductor scene.</summary>
		public const string Landing = "Landing Scene";

		/// <summary>The menu. Stays standalone-playable in the editor.</summary>
		public const string MainMenu = "Main Menu";

		/// <summary>The M1 bare stub world: empty skybox, engine boots,
		/// timeScale 0→1 — the core.devtest destination (zero content
		/// authoring).</summary>
		public const string EngineStub = "OpenFrontier Stub";
	}
}
