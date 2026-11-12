using OpenFrontier.Engine;
using OpenFrontier.Gamemodes;
using UnityEditor;
using UnityEngine;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// Ensures the dev gamemode's launch target exists: the core.devtest
	/// definition asset points at the M1 stub scene. The scaffold installer
	/// created the definition's presentation fields; the engine's world
	/// boot added TargetSceneName to GamemodeDefinition, and this sets it.
	/// Idempotent, and verified against the RELOADED file before reporting
	/// success — the first scaffold reported a spine Unity had actually
	/// refused, and logging records intents while disk records facts.
	/// </summary>
	public static class CoreDevtestInstaller
	{
		private const string DefinitionPath = "Assets/OpenFrontier/Resources/Gamemodes/Development.asset";
		private const string Tag = "[CoreDevtestInstaller]";

		[MenuItem("Open Frontier/Set Core Devtest Stub Scene")]
		public static void Install()
		{
			GamemodeDefinition definition = AssetDatabase.LoadAssetAtPath<GamemodeDefinition>(DefinitionPath);
			if (definition == null)
			{
				Debug.LogError($"{Tag} No definition at {DefinitionPath} - run 'Open Frontier/Scaffold Gamemode Screen (one-shot)' first.");
				return;
			}

			if (definition.TargetSceneName == SceneNames.EngineStub)
			{
				Debug.Log($"{Tag} {DefinitionPath} already targets '{SceneNames.EngineStub}' - left untouched.");
				return;
			}

			definition.TargetSceneName = SceneNames.EngineStub;
			EditorUtility.SetDirty(definition);
			AssetDatabase.SaveAssets();

			// Verify against the reloaded file, not the in-memory object.
			GamemodeDefinition reloaded = AssetDatabase.LoadAssetAtPath<GamemodeDefinition>(DefinitionPath);
			if (reloaded != null && reloaded.TargetSceneName == SceneNames.EngineStub)
			{
				Debug.Log($"{Tag} {DefinitionPath} now targets '{SceneNames.EngineStub}' - the dev gamemode boots the M1 stub world.");
			}
			else
			{
				Debug.LogError($"{Tag} RELOADED FILE DOES NOT CARRY THE CHANGE - the save was refused. Do not trust any success line above this one.");
			}
		}
	}
}
