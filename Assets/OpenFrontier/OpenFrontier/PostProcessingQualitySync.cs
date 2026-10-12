using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: post-processing quality ladder. The global
	/// DefaultVolumeProfile's bloom is authored for Ultra (intensity 5,
	/// high quality filtering, 8 iterations). The lowest tier kills camera
	/// post-processing outright; the tiers between get a cheaper bloom
	/// (no HQ filtering, fewer pyramid iterations) with intensity scaled
	/// down. Uses a runtime-created global Volume with a runtime-created
	/// profile, so no assets are edited (no play-mode drift). Lives on the
	/// persistent GameController and applies whenever the quality level
	/// changes.
	/// </summary>
	public class PostProcessingQualitySync : MonoBehaviour
	{
		private int appliedLevel = -1;

		private Volume volume;

		private Bloom bloom;

		private void Update()
		{
			int qualityLevel = QualitySettings.GetQualityLevel();
			if (qualityLevel != appliedLevel)
			{
				appliedLevel = qualityLevel;
				Apply(qualityLevel);
			}
		}

		private void Apply(int qualityLevel)
		{
			// Post stays ON at every tier: the ACES tonemap + exposure
			// grade is integral to the look, and in URP the grade is
			// baked into the single final color-grading pass - nearly
			// free. What low tiers shed is BLOOM (the expensive
			// multi-pass pyramid), not the grade.
			if (Camera.main != null)
			{
				Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
			}
			EnsureVolume();
			if (qualityLevel <= 2)
			{
				// Lowest/Low/Normal: bloom off entirely; the authored
				// tonemap + color adjustments carry the look alone.
				bloom.active = false;
				return;
			}
			bloom.active = true;
			float value;
			bool value2;
			int value3;
			switch (qualityLevel)
			{
			case 3:
				value = 5f;
				value2 = true;
				value3 = 7;
				break;
			default:
				value = 5f;
				value2 = true;
				value3 = 8;
				break;
			}
			// Only these three are overridden - threshold/scatter/tint
			// flow through from the authored global profile.
			bloom.intensity.overrideState = true;
			bloom.intensity.value = value;
			bloom.highQualityFiltering.overrideState = true;
			bloom.highQualityFiltering.value = value2;
			bloom.maxIterations.overrideState = true;
			bloom.maxIterations.value = value3;
		}

		private void EnsureVolume()
		{
			if (!(volume != null))
			{
				VolumeProfile volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
				bloom = volumeProfile.Add<Bloom>(overrides: true);
				volume = gameObject.AddComponent<Volume>();
				volume.isGlobal = true;
				volume.priority = 10f;
				volume.profile = volumeProfile;
			}
		}
	}
}
