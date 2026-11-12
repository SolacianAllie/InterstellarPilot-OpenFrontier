using UnityEditor;
using UnityEngine;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// One-shot: strip the legacy Tags & Layers and land the rebuild's own
	/// hierarchy (docs/engine-plan.md, "Tags & Layers — the fresh
	/// hierarchy"). Two jobs, both idempotent and verified against
	/// RELOADED state before reporting success — the first scaffold
	/// installer reported a spine Unity had actually refused; logging
	/// records intents, disk records facts.
	///
	/// Job 1 — TagManager: the legacy game's 24 user layers retire
	/// wholesale; the fresh set lands at slots 8-12 (Celestial,
	/// PlayerShip, Ship, Effects, Volumes) and the 9 legacy tags drop.
	/// Their last live carrier (the Nebula template's SpaceScene_Nebula
	/// tag) was deleted content — the nebula strip removed it. Two legacy
	/// layers hide INSIDE the low block: this project reassigned slots 3
	/// ('SkyProbe') and 6 ('TintBackground'), pushing Water→4 and UI→5;
	/// both carried zero live objects and retire too. Slots 0-2/4/5 are
	/// the real built-ins and are verified UNCHANGED — UI at slot 5 is
	/// this project's layout (85 uGUI objects bind it); never normalize
	/// it to the standard slot 4.
	///
	/// Job 2 — the 13 Planet* shell prefabs: legacy 'Planet' roots (23)
	/// and 'BackgroundPlanet' children (26) sweep to Celestial (8), so
	/// the M3 celestial camera culls them correctly from day one. These
	/// shells carry the ONLY live layer assignments outside Default/UI
	/// in the whole project (swept and verified before this tool was
	/// written); no script, camera culling mask or physics-matrix entry
	/// references any legacy index.
	/// </summary>
	public static class LegacyStripInstaller
	{
		private const string Tag = "[LegacyStripInstaller]";
		private const string PlanetsFolder = "Assets/OpenFrontier/Resources/Prefabs/Units/Planets";

		// Legacy indices that carried live content — swept in job 2. Every
		// other legacy slot (8-31) had zero live objects when this landed.
		private const int LegacyPlanetRoot = 23; // was 'Planet'
		private const int LegacyPlanetChild = 26; // was 'BackgroundPlanet'

		// The fresh hierarchy — slot 8 up, addition order. Later milestones
		// APPEND here and to the plan doc together; never reshuffle (indices
		// become load-bearing the moment anything references them). Slots
		// 0-7 are Unity built-ins and stay untouched.
		private static readonly string[] FreshLayersBySlot =
		{
			null, null, null, null, null, null, null, null, // 0-7 built-ins
			"Celestial", // 8
			"PlayerShip", // 9
			"Ship", // 10
			"Effects", // 11
			"Volumes", // 12
		};

		// The full expected layer table — the verify loop checks ALL 32
		// slots against it. Slots 3/6 retired (this project's legacy
		// reassignments); 0-2/4/5 are the real built-ins in THIS project's
		// layout (UI at 5, not the standard 4 — never normalize).
		private static readonly string[] ExpectedLayersBySlot =
		{
			"Default", "TransparentFX", "Ignore Raycast", string.Empty, "Water", "UI", string.Empty, string.Empty,
			"Celestial", "PlayerShip", "Ship", "Effects", "Volumes",
		};

		// Slots cleared beyond 8-31: the two legacy reassignments hiding
		// inside the low block.
		private static readonly int[] ExtraClearedSlots = { 3, 6 };

		private static readonly string[] LegacyTags =
		{
			"Engine", "Hud", "SpaceScene_Planet", "SpaceScene_PlanetRings",
			"SpaceScene_LocalStar", "SpaceScene_Nebula", "SpaceScene_Camera",
			"SpaceScene_Moon", "SpaceScene_Galaxy",
		};

		[MenuItem("Open Frontier/Strip Legacy Layers and Tags (one-shot)")]
		public static void Install()
		{
			RewriteTagManager();
			SweepPlanetPrefabs();
		}

		private static void RewriteTagManager()
		{
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			if (assets == null || assets.Length == 0)
			{
				Debug.LogError($"{Tag} TagManager.asset not found - nothing written.");
				return;
			}

			SerializedObject tagManager = new SerializedObject(assets[0]);

			SerializedProperty layers = tagManager.FindProperty("layers");
			for (int i = 8; i < layers.arraySize; i++)
			{
				layers.GetArrayElementAtIndex(i).stringValue = string.Empty;
			}

			foreach (int slot in ExtraClearedSlots)
			{
				if (slot >= 0 && slot < layers.arraySize)
				{
					layers.GetArrayElementAtIndex(slot).stringValue = string.Empty;
				}
			}

			for (int i = 0; i < FreshLayersBySlot.Length && i < layers.arraySize; i++)
			{
				if (FreshLayersBySlot[i] != null)
				{
					layers.GetArrayElementAtIndex(i).stringValue = FreshLayersBySlot[i];
				}
			}

			SerializedProperty tags = tagManager.FindProperty("tags");
			int droppedTags = 0;
			for (int i = tags.arraySize - 1; i >= 0; i--)
			{
				string value = tags.GetArrayElementAtIndex(i).stringValue;
				foreach (string legacy in LegacyTags)
				{
					if (value == legacy)
					{
						tags.DeleteArrayElementAtIndex(i);
						droppedTags++;
						break;
					}
				}
			}

			tagManager.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();

			// Verify against a FRESH load, not the object just written —
			// ALL 32 slots against the expected table, which also proves
			// the real built-ins survived untouched.
			Object[] reloaded = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			SerializedObject verify = new SerializedObject(reloaded[0]);
			SerializedProperty verifyLayers = verify.FindProperty("layers");
			int wrongSlots = 0;
			for (int i = 0; i < verifyLayers.arraySize; i++)
			{
				string expected = i < ExpectedLayersBySlot.Length ? ExpectedLayersBySlot[i] : string.Empty;
				if (verifyLayers.GetArrayElementAtIndex(i).stringValue != expected)
				{
					wrongSlots++;
				}
			}

			if (wrongSlots == 0)
			{
				Debug.Log($"{Tag} TagManager rewritten and verified on reload: slots 8-12 = Celestial/PlayerShip/Ship/Effects/Volumes, legacy 'SkyProbe'/'TintBackground' cleared, {droppedTags} legacy tag(s) dropped.");
			}
			else
			{
				Debug.LogError($"{Tag} RELOADED TAGMANAGER DOES NOT MATCH ({wrongSlots} wrong slot(s)) - do not trust any success line above this one.");
			}
		}

		private static void SweepPlanetPrefabs()
		{
			int celestial = LayerMask.NameToLayer("Celestial");
			if (celestial < 0)
			{
				// NameToLayer failing here means the rewrite above did not
				// land — abort rather than sweep: re-pointing onto a dead
				// index would leave ghosts on nameless layers.
				Debug.LogError($"{Tag} 'Celestial' does not resolve after the TagManager rewrite - planet sweep ABORTED, nothing changed.");
				return;
			}

			string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PlanetsFolder });
			int swept = 0;
			int alreadyClean = 0;
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject contents = PrefabUtility.LoadPrefabContents(path);
				bool changed = false;
				try
				{
					foreach (Transform t in contents.GetComponentsInChildren<Transform>(true))
					{
						int layer = t.gameObject.layer;
						if (layer == LegacyPlanetRoot || layer == LegacyPlanetChild)
						{
							t.gameObject.layer = celestial;
							changed = true;
						}
					}

					if (changed)
					{
						PrefabUtility.SaveAsPrefabAsset(contents, path);
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(contents);
				}

				if (!changed)
				{
					alreadyClean++;
					continue;
				}

				// Verify against the RELOADED file, not the in-memory edit.
				GameObject check = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				int leftovers = 0;
				foreach (Transform t in check.GetComponentsInChildren<Transform>(true))
				{
					if (t.gameObject.layer == LegacyPlanetRoot || t.gameObject.layer == LegacyPlanetChild)
					{
						leftovers++;
					}
				}

				if (leftovers == 0)
				{
					swept++;
				}
				else
				{
					Debug.LogError($"{Tag} {path}: {leftovers} object(s) STILL on a legacy layer after save - do not trust this file's sweep.");
				}
			}

			Debug.Log($"{Tag} Planet sweep: {swept} prefab(s) re-pointed to Celestial, {alreadyClean} already clean, of {guids.Length} found under {PlanetsFolder}.");
		}
	}
}
