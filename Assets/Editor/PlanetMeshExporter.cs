using System.IO;
using UnityEditor;
using UnityEngine;

// NOT "Unity.Formats.Fbx". The assembly is named Unity.Formats.Fbx.Editor,
// but its namespace is UnityEditor.Formats.Fbx.Exporter, and the two do not
// match -- guessing the namespace from the assembly name gives CS0234 on a
// reference that is present and correct.
using UnityEditor.Formats.Fbx.Exporter;

namespace OpenFrontier.Editor
{
	/// <summary>
	/// Exports a mesh asset to FBX, so it can be opened in a DCC such as
	/// Blender.
	/// </summary>
	/// <remarks>
	/// WHY THIS EXISTS AT ALL -- Unity's FBX exporter cannot export a Mesh
	/// asset, and it does not say so.
	///
	/// Its menu item is registered as "GameObject/Export To FBX...", so it
	/// only appears when a GameObject is selected in the Hierarchy; selecting
	/// the .asset in the Project window gives you no such menu. The API is
	/// typed UnityEngine.Object but documented "The Unity GameObject to
	/// export", so handing it the asset returns null with NO message: the
	/// public ExportObjects only reports success when ExportAll() &gt; 0, and
	/// otherwise falls straight through to `return null`.
	///
	/// The mesh therefore has to sit on a GameObject first. That is the whole
	/// of this class.
	///
	/// MENU: <b>Tools / Open Frontier / Export Planet Mesh to FBX</b>
	///
	/// All twelve planets share ONE mesh (SphereMediumPoly) and differ only
	/// by material, so the geometry is exported once rather than per planet.
	/// </remarks>
	public static class PlanetMeshExporter
	{
		private const string PlanetMeshPath =
			"Assets/InterstellarPilot2/Extracted/Mesh/SphereMediumPoly.asset";

		private const string BatchOutputPath =
			"MeshExports/SphereMediumPoly.fbx";

		[MenuItem("Tools/Open Frontier/Export Planet Mesh to FBX")]
		public static void ExportPlanetMesh()
		{
			string target = EditorUtility.SaveFilePanelInProject(
				"Export Planet Mesh",
				"SphereMediumPoly",
				"fbx",
				"Choose where to write the FBX.");

			if (string.IsNullOrEmpty(target))
			{
				// Cancelled. Not an error.
				return;
			}

			Report(PlanetMeshPath, target);
		}

		/// <summary>
		/// Headless entry point:
		/// <c>Unity -batchmode -quit -projectPath . \
		///   -executeMethod OpenFrontier.Editor.PlanetMeshExporter.ExportPlanetMeshBatch</c>
		/// </summary>
		public static void ExportPlanetMeshBatch()
		{
			Report(PlanetMeshPath, BatchOutputPath);
		}

		private static void Report(string meshAssetPath, string fbxPath)
		{
			string written = ExportMeshToFbx(meshAssetPath, fbxPath);
			if (string.IsNullOrEmpty(written))
			{
				// ExportObjects returns null on failure without a word, so
				// this is the only place the failure can be reported.
				Debug.LogError(
					"[PlanetMeshExporter] FBX export produced no file: " + fbxPath);
				return;
			}

			Debug.Log("[PlanetMeshExporter] wrote " + written +
				" (" + new FileInfo(written).Length + " bytes)");
		}

		/// <summary>
		/// Wraps a mesh asset in a throwaway GameObject, exports it, and
		/// returns the written path -- or null if the export produced nothing.
		/// </summary>
		public static string ExportMeshToFbx(string meshAssetPath, string fbxPath)
		{
			Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
			if (mesh == null)
			{
				Debug.LogError("[PlanetMeshExporter] no Mesh at " + meshAssetPath +
					" -- has it been moved or reimported?");
				return null;
			}

			// The exporter walks GameObjects, so the mesh needs a host. Both
			// components: the MeshFilter carries the geometry, and the
			// MeshRenderer is what the exporter's per-renderer callbacks look
			// for, so a filter alone can come back empty.
			GameObject host = new GameObject(mesh.name);
			try
			{
				host.AddComponent<MeshFilter>().sharedMesh = mesh;
				host.AddComponent<MeshRenderer>();

				ExportModelOptions options = new ExportModelOptions();
				// The defaults are already right for a DCC: ASCII (readable,
				// diffable) and LocalCentered (mesh sits on the origin).
				options.ExportFormat = ExportFormat.ASCII;
				options.ModelAnimIncludeOption = Include.Model;
				// Defaults to true, and it mangles names for anything that is
				// not Maya.
				options.UseMayaCompatibleNames = false;

				return ModelExporter.ExportObject(ToAbsolutePath(fbxPath), host, options);
			}
			finally
			{
				// Always, including on the null-return path: a leftover host in
				// the open scene is worse than a failed export.
				Object.DestroyImmediate(host);
			}
		}

		private static string ToAbsolutePath(string path)
		{
			if (Path.IsPathRooted(path))
			{
				return path;
			}

			// Project-relative ("Assets/...", "MeshExports/..."). Resolve
			// against the project root, not the working directory, which is
			// not guaranteed to be the project in batch mode.
			string projectRoot = Path.GetDirectoryName(Application.dataPath);
			string full = Path.GetFullPath(Path.Combine(projectRoot, path));

			string directory = Path.GetDirectoryName(full);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			return full;
		}
	}
}