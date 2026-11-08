using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: ties the URP shadow settings to the graphics quality
	/// slider - no shadows at the lowest level, then 1K / 2K / 4K maps as
	/// quality rises. Lives on the persistent GameController and applies
	/// whenever the quality level changes. Shadows are disabled by zeroing
	/// the shadow distance (the supported-flags are not runtime-settable).
	/// </summary>
	public class ShadowQualitySync : MonoBehaviour
	{
		private int appliedLevel = -1;

		private void Awake()
		{
			// Spawn the post-processing ladder sibling here: it has no
			// prefab wiring of its own (avoids a new-script GUID dance).
			if (GetComponent<PostProcessingQualitySync>() == null)
			{
				gameObject.AddComponent<PostProcessingQualitySync>();
			}
		}

		private void Update()
		{
			int qualityLevel = QualitySettings.GetQualityLevel();
			if (qualityLevel != appliedLevel)
			{
				appliedLevel = qualityLevel;
				Apply(qualityLevel);
			}
		}

		private static void Apply(int qualityLevel)
		{
			UniversalRenderPipelineAsset universalRenderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
			if (universalRenderPipelineAsset == null)
			{
				return;
			}
			int num;
			switch (qualityLevel)
			{
			case 0:
			case 1:
				num = 1024;
				break;
			case 2:
			case 3:
				num = 2048;
				break;
			default:
				num = 4096;
				break;
			}
			if (universalRenderPipelineAsset.mainLightShadowmapResolution != num)
			{
				universalRenderPipelineAsset.mainLightShadowmapResolution = num;
			}
			// Tiers 0 and 1 are UNTOUCHED (off / stock 250u with 5/25/75
			// splits). Tiers 2, 3 AND 4 keep tier 1's exact near field
			// (5/25/75u) and stretch only cascade 3 from that cutoff
			// out to the sector's end (~3000u gate distance): the split
			// fractions below are 5/25/75 of 3000. (Highest = 4096 map
			// + full reach; Low = small map + short reach.)
			float num2;
			Vector3 vector;
			switch (qualityLevel)
			{
			case 0:
				num2 = 0f;
				vector = new Vector3(0.02f, 0.1f, 0.3f);
				break;
			case 1:
				num2 = 250f;
				vector = new Vector3(0.02f, 0.1f, 0.3f);
				break;
			default:
				num2 = 3000f;
				vector = new Vector3(5f / 3000f, 25f / 3000f, 75f / 3000f);
				break;
			}
			if (universalRenderPipelineAsset.shadowDistance != num2)
			{
				universalRenderPipelineAsset.shadowDistance = num2;
			}
			if (universalRenderPipelineAsset.cascade4Split != vector)
			{
				universalRenderPipelineAsset.cascade4Split = vector;
			}
		}
	}
}
