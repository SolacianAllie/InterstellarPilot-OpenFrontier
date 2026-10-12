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

		public bool NormalCollisionDisabled;

		public float BuildBlockerRadius;

		public float SectorMapMinDrawSize = -1f;
	}
}
