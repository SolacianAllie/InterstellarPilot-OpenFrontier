using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// Converts the 10 legacy "Particles/VertexLit Blended" materials to
	/// URP Particles/Unlit using the Material API (YAML hand-edits of these
	/// files get stripped by URP's material postprocessor, so we create the
	/// materials natively instead).
	/// </summary>
	public static class LegacyParticleConverter
	{
		static readonly string[] Paths =
		{
			"Assets/InterstellarPilot2/Extracted/Material/MaterialAsteroidClusterTypeHDistant.mat",
			"Assets/InterstellarPilot2/Extracted/Material/MaterialAsteroidClusterTypeADistant.mat",
			"Assets/InterstellarPilot2/Extracted/Material/MaterialWormholeBilboard.mat",
			"Assets/InterstellarPilot2/Extracted/Material/MaterialGasCloudExtremeDarkBilboard.mat",
			"Assets/InterstellarPilot2/Extracted/Material/ShieldHit32MaterialNew.mat",
			"Assets/InterstellarPilot2/Extracted/Material/MaterialWormholeParticles.mat",
			"Assets/InterstellarPilot2/Extracted/Material/MaterialWormholeUnstableBilboard.mat",
			"Assets/InterstellarPilot2/Extracted/Material/MaterialGasCloudBilboard.mat",
			"Assets/InterstellarPilot2/Extracted/Material/MaterialStarParticle.mat",
			"Assets/InterstellarPilot2/Extracted/Material/wormhole_capMaterial.mat",
		};

		[MenuItem("OpenFrontier/Repair Star Particle Material Ref")]
		public static void RepairStarParticleMaterial()
		{
			const string matPath = "Assets/InterstellarPilot2/Extracted/Material/MaterialStarParticle.mat";
			var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
			var sb = new StringBuilder();
			sb.AppendLine($"load {matPath}: {(mat == null ? "NULL (import broken!)" : "OK - shader " + mat.shader.name)}");
			if (mat == null)
			{
				Debug.LogError(sb.ToString());
				return;
			}
			string[] prefabs =
			{
				"Assets/InterstellarPilot2/Resources/prefabs/worlddata/engine/StarParticles.prefab",
				"Assets/InterstellarPilot2/Resources/prefabs/worlddata/engine/cameras/GameCamera.prefab",
			};
			foreach (var p in prefabs)
			{
				var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
				if (go == null) { sb.AppendLine($"prefab missing: {p}"); continue; }
				int fixedCount = 0;
				foreach (var rend in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
				{
					if (rend.sharedMaterial == null || rend.sharedMaterial != mat)
					{
						rend.sharedMaterial = mat;
						fixedCount++;
						sb.AppendLine($"  rewired renderer on '{rend.gameObject.name}' in {p}");
					}
				}
				if (fixedCount > 0) EditorUtility.SetDirty(go);
				sb.AppendLine($"{p}: {fixedCount} renderers rewired");
			}
			AssetDatabase.SaveAssets();
			Debug.Log(sb.ToString());
		}

		[MenuItem("OpenFrontier/URP/2 - Convert Remaining Legacy Particle Materials")]
		public static void Convert()
		{
			var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
			if (shader == null)
			{
				Debug.LogError("URP Particles/Unlit shader not found - is URP active?");
				return;
			}
			var sb = new StringBuilder();
			foreach (var path in Paths)
			{
				var legacy = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (legacy == null) { sb.AppendLine($"MISSING: {path}"); continue; }

				var tex = legacy.HasProperty("_MainTex") ? legacy.GetTexture("_MainTex") : null;
				var scale = legacy.HasProperty("_MainTex") ? legacy.GetTextureScale("_MainTex") : Vector2.one;
				var offset = legacy.HasProperty("_MainTex") ? legacy.GetTextureOffset("_MainTex") : Vector2.zero;
				var tint = legacy.HasProperty("_TintColor") ? legacy.GetColor("_TintColor") : Color.white;
				var invFade = legacy.HasProperty("_InvFade") ? legacy.GetFloat("_InvFade") : 1f;

				var mat = new Material(shader)
				{
					name = Path.GetFileNameWithoutExtension(path)
				};
				// transparent, alpha blend, no cull (faithful to legacy particle shaders)
				mat.SetFloat("_Surface", 1f);
				mat.SetFloat("_Blend", 0f);
				mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
				mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				mat.SetFloat("_SrcBlendAlpha", 1f);
				mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				mat.SetFloat("_BlendOp", 0f);
				mat.SetFloat("_ZWrite", 0f);
				mat.SetFloat("_Cull", 0f);
				mat.SetColor("_BaseColor", tint);
				if (tex != null)
				{
					mat.SetTexture("_BaseMap", tex);
					mat.SetTextureScale("_BaseMap", scale);
					mat.SetTextureOffset("_BaseMap", offset);
				}
				if (invFade > 0f)
				{
					mat.SetFloat("_SoftParticlesEnabled", 1f);
					// Empirical: on this OpenGL stack the depth sample behind empty space
					// reads 0, so Near=1/Far=0 is the configuration that keeps
					// soft particles visible (fade = saturate(1 - negativeGap) = 1).
					mat.SetFloat("_SoftParticlesNearFadeDistance", 1f);
					mat.SetFloat("_SoftParticlesFarFadeDistance", 0f);
				}
				mat.SetOverrideTag("RenderType", "Transparent");
				mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

				AssetDatabase.DeleteAsset(path);
				AssetDatabase.CreateAsset(mat, path);
				sb.AppendLine($"converted: {path} (tex={(tex ? tex.name : "none")}, tint={tint})");
			}
			AssetDatabase.SaveAssets();
			Debug.Log(sb.ToString());
		}
	}
}
