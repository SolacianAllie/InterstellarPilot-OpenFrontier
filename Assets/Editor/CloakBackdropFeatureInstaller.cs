using OpenFrontier;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Registers CloakBackdropFeature onto every UniversalRendererData in the
// project, idempotently. A renderer feature cannot be added from runtime
// code, so this lives in Assets/Editor (loose scripts, Assembly-CSharp-
// Editor). It mirrors the behaviour of URP's own "Add Renderer Feature"
// editor button (internal, not callable from here): the feature object is
// created fresh per renderer asset and EMBEDDED as a sub-asset of it,
// with both m_RendererFeatures and m_RendererFeatureMap grown together --
// a shared instance cannot be embedded in two containers, and a list
// entry without a map entry relies on URP's self-heal after a reload.
static class CloakBackdropFeatureInstaller
{
	[MenuItem("Tools/OpenFrontier/Add Cloak Backdrop Feature To Renderers")]
	static void Install()
	{
		int installed = 0;
		foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
			if (rendererData == null)
			{
				continue;
			}

			bool present = rendererData.rendererFeatures != null && rendererData.rendererFeatures.Exists(f => f is CloakBackdropFeature);
			if (present)
			{
				Debug.Log($"[CloakBackdrop] already registered: {path}");
				continue;
			}

			EmbedFeature(rendererData);
			EditorUtility.SetDirty(rendererData);
			installed++;
			Debug.Log($"[CloakBackdrop] installed onto {path}");
		}

		AssetDatabase.SaveAssets();
		if (installed == 0)
		{
			Debug.Log("[CloakBackdrop] nothing to install (renderer assets are already covered).");
		}
	}

	static void EmbedFeature(UniversalRendererData rendererData)
	{
		var feature = ScriptableObject.CreateInstance<CloakBackdropFeature>();
		feature.name = "CloakBackdropFeature";
		feature.hideFlags |= HideFlags.HideInHierarchy;
		// Same convention as URP's own editor: the feature lives inside the
		// renderer asset so the reference survives the project anywhere.
		AssetDatabase.AddObjectToAsset(feature, rendererData);
		AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out var _, out long localId);

		var serializedObject = new SerializedObject(rendererData);
		var features = serializedObject.FindProperty("m_RendererFeatures");
		var map = serializedObject.FindProperty("m_RendererFeatureMap");
		features.arraySize++;
		features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
		map.arraySize++;
		map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
		serializedObject.ApplyModifiedProperties();
	}
}
