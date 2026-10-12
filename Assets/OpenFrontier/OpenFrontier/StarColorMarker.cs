using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: a scene-placed data marker that overrides the sector's
	/// star color for hand-crafted universes (e.g. Uncharted Space). It is
	/// NOT a star itself - the normal dynamic sun still renders; it just
	/// takes this marker's color instead of the generated one (see
	/// StarColorGenerator.ResolveCurrent).
	///
	/// Usage: drop the prefab under a scenario's Sector<name> object to
	/// hand-craft THAT sector's star, or anywhere else in the scene for a
	/// scene-wide override. Sector markers win over scene markers.
	///
	/// In the editor (edit mode only) an EditorOnly child preview shows the
	/// chosen color and reports it in its name; the preview is stripped
	/// from play mode and builds.
	/// </summary>
	[ExecuteAlways]
	public class StarColorMarker : MonoBehaviour
	{
		[ColorUsage(false, true)]
		public Color StarColor = Color.white;

		public Material PreviewMaterial;

		public float PreviewSize = 50f;

		// The currently active marker (one per scene), registered on
		// enable/disable so StarColorGenerator.ResolveCurrent stays O(1).
		public static StarColorMarker ActiveMarker { get; private set; }

		private const string PreviewName = "StarPreview";

		private void OnEnable()
		{
			ActiveMarker = this;
			RebuildPreview();
		}

		private void OnDisable()
		{
			if (ActiveMarker == this)
			{
				ActiveMarker = null;
			}
		}

		private void OnValidate()
		{
			RebuildPreview();
		}

		private void RebuildPreview()
		{
#if UNITY_EDITOR
			Transform transform2 = null;
			foreach (Transform item in transform)
			{
				if (item.name.StartsWith(PreviewName))
				{
					transform2 = item;
					break;
				}
			}
			if (Application.isPlaying)
			{
				if (transform2 != null)
				{
					Object.DestroyImmediate(transform2.gameObject);
				}
				return;
			}
			if (transform2 == null)
			{
				GameObject gameObject = new GameObject();
				gameObject.tag = "EditorOnly";
				gameObject.transform.SetParent(transform, worldPositionStays: false);
				MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = SharedMeshes.Quad;
				MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = PreviewMaterial;
				meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				transform2 = gameObject.transform;
			}
			transform2.name = PreviewName + " #" + ColorUtility.ToHtmlStringRGB(StarColor);
			transform2.localScale = new Vector3(PreviewSize, PreviewSize, 1f);
			MeshRenderer component = transform2.GetComponent<MeshRenderer>();
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			materialPropertyBlock.SetColor("_Color", StarColor);
			component.SetPropertyBlock(materialPropertyBlock);
#endif
		}
	}
}
