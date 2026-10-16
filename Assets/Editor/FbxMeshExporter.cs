using System.IO;
using UnityEditor;
using UnityEngine;

namespace OpenFrontier.Editor
{
	/// <summary>
	/// Exports a Mesh to FBX without going through Unity's FBX exporter.
	/// </summary>
	/// <remarks>
	/// WHY NOT com.unity.formats.fbx
	///
	/// That exporter only walks GameObjects and says nothing when it cannot:
	///
	///   * Its menu item is registered as "GameObject/Export To FBX...", so it
	///     only appears with a GameObject selected in the Hierarchy. Selecting
	///     a Mesh .asset in the Project window offers no such menu.
	///   * The API is typed UnityEngine.Object but documented "The Unity
	///     GameObject to export". Handing it a Mesh returns null with NO
	///     message, because ExportObjects only reports success when
	///     ExportAll() &gt; 0 and otherwise falls through to `return null`.
	///
	/// Wrapping the asset in a host GameObject does get it through, but its
	/// settings window and ExportSettings are both internal, so the options
	/// have to be guessed anyway. Writing the file directly is shorter and has
	/// no internal API in it at all. See FbxMeshWriter for the format.
	///
	/// WHAT IS AND IS NOT VERIFIED
	///
	/// The writer is covered by tools/fbx: it is compiled outside Unity against
	/// synthetic meshes and the output is parsed by an independent validator
	/// that checks array counts, the complement markers, index range, layer
	/// wiring and whether the winding agrees with the shading normals.
	///
	/// What that CANNOT check is how Blender interprets the result, because
	/// nothing here can run Blender. Handedness is the one real unknown: Unity
	/// is left-handed, Blender's FBX importer is right-handed, and a file can
	/// import inside out. <see cref="FlipWinding"/> exists for exactly that.
	/// </remarks>
	public static class FbxMeshExporter
	{
		private const string PlanetMeshPath =
			"Assets/InterstellarPilot2/Extracted/Mesh/SphereMediumPoly.asset";

		private const string BatchOutputPath = "MeshExports/SphereMediumPoly.fbx";

		/// <summary>
		/// Reverses triangle winding on export. Leave off unless the imported
		/// mesh comes in inside out.
		/// </summary>
		public static bool FlipWinding = false;

		/// <summary>
		/// Project window: select one or more Mesh assets, right-click, export.
		/// </summary>
		[MenuItem("Assets/Export Mesh To FBX...", false, 200)]
		private static void ExportSelected()
		{
			Object[] selection = Selection.objects;
			if (selection == null || selection.Length == 0)
			{
				return;
			}

			Mesh[] selected = new Mesh[selection.Length];
			int found = 0;
			for (int i = 0; i < selection.Length; i++)
			{
				Mesh mesh = selection[i] as Mesh;
				if (mesh != null)
				{
					selected[found++] = mesh;
				}
			}

			if (found == 0)
			{
				EditorUtility.DisplayDialog("Export Mesh To FBX",
					"Select one or more Mesh assets.\n\n"
					+ "Only Mesh assets are supported. Prefabs and models already "
					+ "go through Unity's own exporter.", "OK");
				return;
			}

			if (found == 1)
			{
				string target = EditorUtility.SaveFilePanelInProject(
					"Export Mesh To FBX", selected[0].name, "fbx",
					"Choose where to write the FBX.");
				if (string.IsNullOrEmpty(target))
				{
					return;
				}

				Export(selected[0], target);
				return;
			}

			// Several meshes: one file each, beside each other, because an
			// FBX holds a single model and merging them would mean inventing
			// transforms the user did not ask for.
			string folder = EditorUtility.OpenFolderPanel(
				"Choose a folder for " + found + " FBX files", "Assets", "");
			if (string.IsNullOrEmpty(folder))
			{
				return;
			}

			foreach (Mesh mesh in selected)
			{
				if (mesh == null)
				{
					continue;
				}

				Export(mesh, Path.Combine(folder, mesh.name + ".fbx"));
			}

			AssetDatabase.Refresh();
		}

		[MenuItem("Assets/Export Mesh To FBX...", true, 200)]
		private static bool ValidateExportSelected()
		{
			return Selection.objects != null && Selection.objects.Length > 0;
		}

		/// <summary>
		/// Headless, for the planet mesh:
		/// <c>Unity -batchmode -quit -projectPath . \
		///   -executeMethod OpenFrontier.Editor.FbxMeshExporter.ExportPlanetMeshBatch</c>
		/// </summary>
		public static void ExportPlanetMeshBatch()
		{
			Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(PlanetMeshPath);
			if (mesh == null)
			{
				Debug.LogError("[FbxMeshExporter] no Mesh at " + PlanetMeshPath +
					" -- has it been moved or reimported?");
				return;
			}

			Export(mesh, BatchOutputPath);
		}

		private static void Export(Mesh mesh, string target)
		{
			try
			{
				string text = FbxMeshWriter.Build(mesh.name,
					Flatten(mesh.vertices, 3),
					mesh.normals != null && mesh.normals.Length == mesh.vertexCount
						? Flatten(mesh.normals, 3) : null,
					mesh.tangents != null && mesh.tangents.Length == mesh.vertexCount
						? FlattenTangents(mesh.tangents) : null,
					mesh.uv != null && mesh.uv.Length == mesh.vertexCount
						? Flatten(mesh.uv, 2) : null,
					mesh.GetTriangles(0),
					FlipWinding);

				string full = ToAbsolutePath(target);
				string directory = Path.GetDirectoryName(full);
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				File.WriteAllText(full, text);
				Debug.Log("[FbxMeshExporter] wrote " + full + " -- " +
					mesh.vertexCount + " verts, " + (mesh.GetTriangles(0).Length / 3) +
					" tris, " + new FileInfo(full).Length + " bytes" +
					(FlipWinding ? " (winding flipped)" : ""));
			}
			catch (System.Exception e)
			{
				// Never swallow this. A writer that fails quietly is how you
				// end up with an empty file and no idea why.
				Debug.LogError("[FbxMeshExporter] failed to export " +
					mesh.name + ": " + e.Message);
				throw;
			}
		}

		private static float[] Flatten(Vector3[] source, int components)
		{
			float[] outArray = new float[source.Length * components];
			for (int i = 0; i < source.Length; i++)
			{
				outArray[(i * components) + 0] = source[i].x;
				if (components > 1)
				{
					outArray[(i * components) + 1] = source[i].y;
				}

				if (components > 2)
				{
					outArray[(i * components) + 2] = source[i].z;
				}
			}

			return outArray;
		}

		private static float[] Flatten(Vector2[] source, int components)
		{
			float[] outArray = new float[source.Length * components];
			for (int i = 0; i < source.Length; i++)
			{
				outArray[i * components] = source[i].x;
				outArray[(i * components) + 1] = source[i].y;
			}

			return outArray;
		}

		/// <summary>Vector4 tangents to a flat xyzw array.</summary>
		private static float[] FlattenTangents(Vector4[] source)
		{
			float[] outArray = new float[source.Length * 4];
			for (int i = 0; i < source.Length; i++)
			{
				outArray[(i * 4) + 0] = source[i].x;
				outArray[(i * 4) + 1] = source[i].y;
				outArray[(i * 4) + 2] = source[i].z;
				outArray[(i * 4) + 3] = source[i].w;
			}

			return outArray;
		}

		private static string ToAbsolutePath(string path)
		{
			if (Path.IsPathRooted(path))
			{
				return path;
			}

			// Project-relative. Resolved against the project root rather than
			// the working directory, which is not the project in batch mode.
			string projectRoot = Path.GetDirectoryName(Application.dataPath);
			return Path.GetFullPath(Path.Combine(projectRoot, path));
		}
	}
}