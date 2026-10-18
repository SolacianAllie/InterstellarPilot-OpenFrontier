using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class UnitClassDisplayData : MonoBehaviour
	{
		public float HUDTargetBracketsRadiusMultiplier = 1f;

		public float CameraOrbitDistanceMultipler = 1f;

		public Vector3 HudCompassManualFudgeOffset = Vector3.zero;

		public float HudCompassRadiusFudgeMultiplier = 1f;

		public float Width = 1f;

		public float Radius = 10f;

		public string ActiveUnitClassName;

		public string ActiveUnitClassNameDistant;

		[FormerlySerializedAs("HudHullTextureName")]
		public string ThumbnailSpriteName;

		public string RenderIconSpriteName;

		[FormerlySerializedAs("IconSpriteColor")]
		public Color ThumbnailIconSpriteColor = Color.white;

		public bool ForceThumbnailIconSpriteColor;

		public bool ApplyThumbnailHullColor = true;

		// ---- planet scale roll ----
		//
		// Per-class size variety, rolled per unit by CreatePlanetsSeeder and
		// re-derived on every activation by Unit.ApplyPlanetVisualScale from
		// the unit's own Seed. It lives here rather than on the seeder
		// settings because the value has to be readable at BOTH times: a
		// freshly generated world (placement math needs it before a
		// position can be picked) and a save being reloaded (the roll must
		// reproduce exactly, and Unit.Seed is the piece of the unit the
		// save always carried).
		//
		// 0, 0 - the default on every asset that predates this - reads as
		// stock size 1: no roll happens, nothing is rescaled, and every
		// existing prefab keeps its behaviour. Author a range on a class
		// and its bodies start varying inside it.
		//
		// The same pair is used for planets and for moons; a moon class and
		// a planet class simply author their own ranges.
		//
		// VisualScaleSteps quantizes the roll instead of smoothing it: 0
		// (the default on every asset predating it) keeps the range
		// continuous, and N >= 2 snaps to N evenly spaced sizes INCLUDING
		// both endpoints. Min 0.5, Max 1, Steps 2 reads as "each moon is
		// exactly 0.5x or exactly 1x, never between, never above".
		public float VisualScaleMin;

		public float VisualScaleMax;

		public int VisualScaleSteps;

		public bool NormalCollisionDisabled;

		public float BuildBlockerRadius;

		public float SectorMapMinDrawSize = -1f;
	}
}
