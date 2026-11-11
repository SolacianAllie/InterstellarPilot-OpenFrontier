using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes crease-averaged "outline normals" into the vertex COLOR channel
/// of every ship mesh at import time.
///
/// Why: the hologram's outline ring (OpenFrontier/Hologram Ship, PASS 3)
/// inflates along vertex normals. Ships' hard edges (90-degree hull
/// plates) are SPLIT vertices - two copies, two face normals - so the
/// two extruded shells slide apart and the ring opens gaps exactly where
/// faces meet. The fix is the classic one: weld vertices by position,
/// average the normals every position saw, and store that average per
/// vertex - every copy of a crease vertex then carries the SAME
/// direction and the shells stay glued, while the extrusion stays the
/// old even-width per-vertex-normal method.
///
/// Carrier: the COLOR channel (rgb = n*0.5+0.5, alpha = 128 marks "baked
/// data present"). Verified free across the ship family: Ship Surface
/// and the hologram shader read no vertex colour, and no game script
/// writes one. The hologram's PASS 3 ALWAYS extrudes along this bake -
/// it accepts the data ONLY when alpha sits at the 0.5 marker; a mesh
/// without the stream reads white (a=1) or zero (a=0), both fail the
/// marker and fall back to the plain vertex normal, so an unbaked mesh
/// can never garbage-extrude.
///
/// Scope: Assets/OpenFrontier/Ships/ only. Runs on every re-import of a
/// model there (idempotent - each pass recomputes from the mesh's own
/// normals) and once on demand via "Open Frontier / Bake Holo Outline
/// Normals (Re-import Ships)". Degenerate welds (opposite normals
/// cancelling, two-sided fins) keep the vertex's own normal.
/// </summary>
internal class HoloOutlineNormalsBaker : AssetPostprocessor
{
	const string ShipsRoot = "Assets/OpenFrontier/Ships";

	// Alpha byte written with every baked colour. 128/255 = 0.502; the
	// shader accepts |a - 0.5| < 0.02 as "baked data present", which
	// distinguishes the bake from a mesh with no colour stream at all
	// (that reads white, a=1, or zero, a=0 - both fail the marker).
	const byte MarkerAlpha = 128;

	void OnPostprocessModel(GameObject root)
	{
		// Ships only - meshes elsewhere stay untouched.
		if (!assetPath.StartsWith(ShipsRoot + "/"))
			return;

		int baked = 0;
		foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
		{
			if (filter.sharedMesh != null && Bake(filter.sharedMesh)) baked++;
		}
		foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
		{
			if (skinned.sharedMesh != null && Bake(skinned.sharedMesh)) baked++;
		}
		if (baked > 0)
			Debug.Log($"[HoloOutlineNormals] {assetPath}: baked smoothed outline normals into {baked} mesh(es) (vertex COLOR, alpha marker 0.5).");
	}

	/// <summary>
	/// Welds by quantized position (1e-5 object units - exporter's float
	/// copies weld even when their bits differ slightly), sums the
	/// normals each position saw, and writes the normalized average.
	/// Returns false when the mesh was not touched.
	/// </summary>
	static bool Bake(Mesh mesh)
	{
		Vector3[] verts = mesh.vertices;
		Vector3[] norms = mesh.normals;
		int count = verts.Length;
		if (count == 0 || norms.Length != count)
			return false;

		const float kQuant = 100000f; // 1e-5 object units
		var weld = new Dictionary<Vector3Int, int>(count);
		var sum = new List<Vector3>(count);
		var slot = new int[count];

		for (int i = 0; i < count; i++)
		{
			var key = new Vector3Int(
				Mathf.RoundToInt(verts[i].x * kQuant),
				Mathf.RoundToInt(verts[i].y * kQuant),
				Mathf.RoundToInt(verts[i].z * kQuant));
			if (!weld.TryGetValue(key, out int index))
			{
				index = sum.Count;
				weld.Add(key, index);
				sum.Add(Vector3.zero);
			}
			sum[index] += norms[i];
			slot[i] = index;
		}

		var colors = new Color32[count];
		for (int i = 0; i < count; i++)
		{
			Vector3 total = sum[slot[i]];
			float len = total.magnitude;
			Vector3 dir = len > 1e-6f ? total / len : norms[i];
			colors[i] = new Color32(
				(byte)Mathf.Clamp(Mathf.RoundToInt((dir.x * 0.5f + 0.5f) * 255f), 0, 255),
				(byte)Mathf.Clamp(Mathf.RoundToInt((dir.y * 0.5f + 0.5f) * 255f), 0, 255),
				(byte)Mathf.Clamp(Mathf.RoundToInt((dir.z * 0.5f + 0.5f) * 255f), 0, 255),
				MarkerAlpha);
		}

		mesh.SetColors(colors);
		return true;
	}

	/// <summary>
	/// One click to (re)bake everything under Ships/ after the tool's
	/// behaviour changes: re-imports every model there, which reruns the
	/// OnPostprocessModel bake above.
	/// </summary>
	[MenuItem("Open Frontier/Bake Holo Outline Normals (Re-import Ships)")]
	public static void ReimportShips()
	{
		int models = 0;
		foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ShipsRoot }))
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			models++;
		}
		Debug.Log($"[HoloOutlineNormals] re-imported {models} model(s) under {ShipsRoot} - outlines may use the baked normals now.");
	}
}
