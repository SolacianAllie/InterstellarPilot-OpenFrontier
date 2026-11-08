using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier.EditorTools
{
	public static class UrpBootstrap
	{
		const string SettingsDir = "Assets/InterstellarPilot2/Settings";
		const string PipelinePath = SettingsDir + "/OpenFrontier-URP.asset";
		const string RendererPath = SettingsDir + "/OpenFrontier-URP-Renderer.asset";
		

		[MenuItem("OpenFrontier/URP/1 - Create and Assign URP Pipeline")]
		public static void CreateAndAssign()
		{
			Directory.CreateDirectory(SettingsDir);

			// 1. renderer data asset (must be saved BEFORE the pipeline references it)
			var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
			if (renderer == null)
			{
				renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
				AssetDatabase.CreateAsset(renderer, RendererPath);
			}

			// 2. pipeline asset referencing the renderer
			var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
			if (pipeline == null)
			{
				pipeline = UniversalRenderPipelineAsset.Create(renderer);
				AssetDatabase.CreateAsset(pipeline, PipelinePath);
			}
			else
			{
				// repair: ensure a valid default renderer is assigned
				var so = new SerializedObject(pipeline);
				var list = so.FindProperty("m_RendererDataList");
				bool broken = list == null || list.arraySize == 0 ||
					list.GetArrayElementAtIndex(0).objectReferenceValue == null;
				if (broken && list != null)
				{
					if (list.arraySize == 0) list.arraySize = 1;
					list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
					so.ApplyModifiedPropertiesWithoutUndo();
					Debug.Log("Repaired missing default renderer on existing pipeline asset.");
				}
			}

			// the game relies on soft particles (nebulas, fog, dust) - they need scene depth
			pipeline.supportsCameraDepthTexture = true;

			// Graphics settings
			GraphicsSettings.defaultRenderPipeline = pipeline;
			// All quality levels
			int levels = QualitySettings.names.Length;
			for (int i = 0; i < levels; i++)
			{
				QualitySettings.SetQualityLevel(i, false);
				QualitySettings.renderPipeline = pipeline;
			}
			QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);

			AssetDatabase.SaveAssets();
			Debug.Log($"URP pipeline created at {PipelinePath} and assigned to Graphics + all Quality levels. Now run: Edit > Rendering > Materials > Convert All Built-in Materials to URP (or our scripted converters).");
		}

		[MenuItem("OpenFrontier/URP/Report Current Pipeline")]
		public static void Report()
		{
			Debug.Log($"GraphicsSettings.defaultRenderPipeline: {(GraphicsSettings.defaultRenderPipeline == null ? "BUILT-IN" : GraphicsSettings.defaultRenderPipeline.name)}\n" +
				$"QualitySettings.renderPipeline: {(QualitySettings.renderPipeline == null ? "BUILT-IN" : QualitySettings.renderPipeline.name)}");
		}
	}
}
