#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Engine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// Nebula prefabs serialize all three LOD TextAssets, so any build that
	/// places one carries 1 + 8 + 64 MB of volume data even when the rung only
	/// ever uploads the 1 MB low LOD. This prices the bill before a mobile
	/// build instead of after it: the audit menu item lists it, and a mobile
	/// pre-build pass warns when enabled scenes reference medium/high LODs.
	///
	/// Editor-only by folder (Assets/Editor -> Assembly-CSharp-Editor), so
	/// none of this reaches a player build.
	/// </summary>
	static class NebulaVolumeBuildCheck
	{
		const string PrefabFolder = "Assets/OpenFrontier/Galaxy/Nebulae/Prefabs";

		struct LodBytes
		{
			public long Low;
			public long Medium;
			public long High;
			public long Total => Low + Medium + High;
		}

		[MenuItem("OpenFrontier/Audit/Nebula Volume LOD Memory")]
		static void Report()
		{
			int prefabs;
			Dictionary<string, int> lods = NebulaLods(out prefabs);
			LodBytes authored = MeasureAll(lods);
			LodBytes shipped = MeasureShipped(lods);

			Debug.Log(
				$"[NebulaVolumeAudit] {prefabs} prefabs author " +
				$"LOD64 {Mb(authored.Low)}, LOD128 {Mb(authored.Medium)}, " +
				$"LOD256 {Mb(authored.High)}.\n" +
				$"Enabled build scenes reference LOD64 {Mb(shipped.Low)}, " +
				$"LOD128 {Mb(shipped.Medium)}, LOD256 {Mb(shipped.High)} " +
				$"({Mb(shipped.Total)} total).\n" +
				"Only LOD64 is uploaded on Mobile. The rest is package size the " +
				"mobile rung never samples; LOD256 is 88% of it.");
		}

		sealed class MobileBuildWarning : IPreprocessBuildWithReport
		{
			public int callbackOrder => 0;

			public void OnPreprocessBuild(BuildReport report)
			{
				BuildTarget target = report.summary.platform;
				if (target != BuildTarget.Android && target != BuildTarget.iOS) return;

				int prefabs;
				LodBytes shipped = MeasureShipped(NebulaLods(out prefabs));
				if (shipped.Medium + shipped.High == 0) return;

				Debug.LogWarning(
					$"[NebulaVolumeAudit] This {target} build ships " +
					$"{Mb(shipped.Medium + shipped.High)} of nebula volume LODs the " +
					$"Mobile rung never samples (LOD128 {Mb(shipped.Medium)}, " +
					$"LOD256 {Mb(shipped.High)}; LOD64 {Mb(shipped.Low)}). " +
					"Placed prefabs serialize all three LODs. Ship LOD64 only, or " +
					"move the high rungs behind a desktop-only bundle.");
			}
		}

		/// <summary>LOD asset path -> rung (0/1/2), from every nebula prefab.</summary>
		static Dictionary<string, int> NebulaLods(out int prefabs)
		{
			var lods = new Dictionary<string, int>();
			prefabs = 0;

			foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null) continue;

				GalaxyNebulaVolume volume = prefab.GetComponentInChildren<GalaxyNebulaVolume>(true);
				if (volume == null) continue;

				prefabs++;
				AddLod(lods, volume.volumeLow, 0);
				AddLod(lods, volume.volumeMedium, 1);
				AddLod(lods, volume.volumeHigh, 2);
			}

			return lods;
		}

		static void AddLod(Dictionary<string, int> lods, TextAsset asset, int rung)
		{
			if (asset == null) return;
			string path = AssetDatabase.GetAssetPath(asset);
			if (!string.IsNullOrEmpty(path)) lods[path] = rung;
		}

		static LodBytes MeasureAll(Dictionary<string, int> lods)
		{
			var total = new LodBytes();
			foreach (KeyValuePair<string, int> pair in lods)
			{
				long size = SizeOf(pair.Key);
				if (pair.Value == 0) total.Low += size;
				else if (pair.Value == 1) total.Medium += size;
				else total.High += size;
			}
			return total;
		}

		static LodBytes MeasureShipped(Dictionary<string, int> lods)
		{
			var total = new LodBytes();
			var seen = new HashSet<string>();

			foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
			{
				if (!scene.enabled) continue;
				foreach (string dependency in AssetDatabase.GetDependencies(scene.path, true))
				{
					if (!seen.Add(dependency)) continue;
					int rung;
					if (!lods.TryGetValue(dependency, out rung)) continue;

					long size = SizeOf(dependency);
					if (rung == 0) total.Low += size;
					else if (rung == 1) total.Medium += size;
					else total.High += size;
				}
			}

			return total;
		}

		static long SizeOf(string path)
		{
			var info = new FileInfo(path);
			return info.Exists ? info.Length : 0;
		}

		static string Mb(long bytes) => $"{bytes / (1024f * 1024f):0.0} MB";
	}
}
#endif
