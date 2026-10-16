using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.Editor
{
	/// <summary>
	/// Makes Unity's own FBX exporter work on the ripped <c>.asset</c> meshes,
	/// by adding <b>Assets / Export To FBX...</b>.
	/// </summary>
	/// <remarks>
	/// THE BUG BEING FIXED
	///
	/// The exporter can only export a GameObject, but it never says so:
	///
	///   * Its menu item is registered as "GameObject/Export To FBX...", so it
	///     only appears with a GameObject selected in the Hierarchy.
	///     Selecting a Mesh .asset in the Project window offers no such menu.
	///   * The API is typed UnityEngine.Object but documented "The Unity
	///     GameObject to export". Handing it a Mesh returns null with NO
	///     message, because ExportObjects only reports success when
	///     ExportAll() &gt; 0 and otherwise falls through to `return null`.
	///
	/// So a ripped Mesh asset is unreachable through the exporter's own entry
	/// points, silently. This wraps one in a host GameObject so it is not.
	///
	/// WHY IT EXPORTS DIRECTLY RATHER THAN OPENING UNITY'S OWN WINDOW
	///
	/// ExportModelEditorWindow and ExportSettings are both internal, so the
	/// exporter's settings window cannot be opened or read from outside the
	/// package. Delegating to it is not possible; the options below are
	/// therefore explicit rather than inherited from whatever the user last
	/// set in that window.
	///
	/// The host lives in a preview scene and is flagged HideAndDontSave, so
	/// it cannot be saved into the user's scene, nor into the prefab they
	/// happen to have open. It is destroyed immediately after the export --
	/// there is no window to outlive, because the export is synchronous.
	/// </remarks>
	public static class FbxAssetExporter
	{
		private const string PlanetMeshPath =
			"Assets/InterstellarPilot2/Extracted/Mesh/SphereMediumPoly.asset";

		private const string BatchOutputPath = "MeshExports/SphereMediumPoly.fbx";

		private static Scene s_previewScene;
		private static readonly List<GameObject> s_hosts = new List<GameObject>();

		[MenuItem("Assets/Export To FBX...", false, 200)]
		private static void ExportSelection()
		{
			UnityEngine.Object[] selection = Selection.objects;
			if (selection == null || selection.Length == 0)
			{
				return;
			}

			string target = EditorUtility.SaveFilePanelInProject(
				"Export To FBX",
				DefaultFileName(selection),
				"fbx",
				"Choose where to write the FBX.");
			if (string.IsNullOrEmpty(target))
			{
				// Cancelled. Not an error.
				return;
			}

			Report(ExportSelectionTo(selection, target));
		}

		[MenuItem("Assets/Export To FBX...", true, 200)]
		private static bool ValidateExportSelection()
		{
			return Selection.objects != null && Selection.objects.Length > 0;
		}

		/// <summary>
		/// Headless, for the planet mesh:
		/// <c>Unity -batchmode -quit -projectPath . \
		///   -executeMethod OpenFrontier.Editor.FbxAssetExporter.ExportPlanetMeshBatch</c>
		/// </summary>
		public static void ExportPlanetMeshBatch()
		{
			Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(PlanetMeshPath);
			if (mesh == null)
			{
				Debug.LogError("[FbxAssetExporter] no Mesh at " + PlanetMeshPath +
					" -- has it been moved or reimported?");
				return;
			}

			Report(ExportSelectionTo(new UnityEngine.Object[] { mesh },
				BatchOutputPath));
		}

		private static string ExportSelectionTo(UnityEngine.Object[] selection,
			string target)
		{
			List<UnityEngine.Object> toExport = new List<UnityEngine.Object>();

			// The try wraps host creation too, not just the export. Creating a
			// host can throw (a preview scene that will not open), and a throw
			// mid-loop would otherwise leave the hosts already made behind.
			try
			{
				int wrapped = 0;
				foreach (UnityEngine.Object selected in selection)
				{
					Mesh mesh = selected as Mesh;
					if (mesh != null)
					{
						toExport.Add(CreateHost(mesh));
						wrapped++;
					}
					else
					{
						// Prefabs, models and GameObjects already work; pass
						// them through untouched.
						toExport.Add(selected);
					}
				}

				if (wrapped > 0)
				{
					Debug.Log("[FbxAssetExporter] wrapped " + wrapped +
						" Mesh asset(s) in a hidden GameObject, because the "
						+ "exporter only walks GameObjects.");
				}

				return ModelExporter.ExportObjects(ToAbsolutePath(target),
					toExport.ToArray(), DefaultOptions());
			}
			finally
			{
				// Always, including when the export returns null.
				CleanupHosts();
			}
		}

		private static ExportModelOptions DefaultOptions()
		{
			ExportModelOptions options = new ExportModelOptions();

			// These are the package defaults, spelled out because the saved
			// settings are internal and unreachable. They are also what suits a
			// DCC: ASCII is readable and diffable, and LocalCentered leaves the
			// mesh on the origin.
			options.ExportFormat = ExportFormat.ASCII;
			options.ObjectPosition = ObjectPosition.LocalCentered;

			// Model only. A ripped mesh has no animation, and asking for it
			// adds nothing to the file.
			options.ModelAnimIncludeOption = Include.Model;

			// Defaults to true, and it mangles names for anything that is not
			// Maya.
			options.UseMayaCompatibleNames = false;

			return options;
		}

		private static string DefaultFileName(UnityEngine.Object[] selection)
		{
			if (selection.Length == 1)
			{
				return selection[0].name;
			}

			return "Untitled";
		}

		private static void Report(string written)
		{
			// ExportObjects returns null on failure without a word, so this is
			// the only place that can report it.
			if (string.IsNullOrEmpty(written))
			{
				Debug.LogError("[FbxAssetExporter] the FBX export produced no "
					+ "file. The exporter returns null when ExportAll() is 0, "
					+ "which is what a raw asset with no host GameObject gives.");
				return;
			}

			Debug.Log("[FbxAssetExporter] wrote " + written + " (" +
				new FileInfo(written).Length + " bytes)");
		}

		private static GameObject CreateHost(Mesh mesh)
		{
			GameObject host = new GameObject(mesh.name);
			host.hideFlags = HideFlags.HideAndDontSave;
			host.AddComponent<MeshFilter>().sharedMesh = mesh;

			// A preview scene, not the active one. Without this, exporting
			// while a prefab is open puts the host into the prefab stage.
			Scene preview = EnsurePreviewScene();
			if (preview.IsValid() && preview.isLoaded)
			{
				SceneManager.MoveGameObjectToScene(host, preview);
			}

			s_hosts.Add(host);
			return host;
		}

		private static Scene EnsurePreviewScene()
		{
			if (!s_previewScene.IsValid() || !s_previewScene.isLoaded)
			{
				s_previewScene = EditorSceneManager.NewPreviewScene();
			}

			return s_previewScene;
		}

		private static void CleanupHosts()
		{
			foreach (GameObject host in s_hosts)
			{
				if (host != null)
				{
					Object.DestroyImmediate(host);
				}
			}

			s_hosts.Clear();
		}

		private static string ToAbsolutePath(string path)
		{
			if (Path.IsPathRooted(path))
			{
				return path;
			}

			// Project-relative ("Assets/...", "MeshExports/..."). Resolved
			// against the project root, not the working directory, which is
			// not the project in batch mode.
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