using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.ActiveUnitFx
{
	public class ActiveUnitFadeOutMeshController : MonoBehaviour
	{
		private struct FadeOutMesh
		{
			public MaterialPropertyBlock MaterialPropertyBlock;

			public MeshRenderer MeshRenderer;

			public Color OriginalColor;

			// Distance-animated alpha, kept here (NOT read back from the
			// block) so the cloud-hide multiplier can compose without
			// compounding.
			public float CurrentAlpha;

			public FadeOutMesh(MeshRenderer meshRenderer, MaterialPropertyBlock materialPropertyBlock, Color originalColor)
			{
				this = default;
				MeshRenderer = meshRenderer;
				MaterialPropertyBlock = materialPropertyBlock;
				OriginalColor = originalColor;
				CurrentAlpha = originalColor.a;
			}
		}

		private ActiveUnit activeUnit;

		public float UpperDistance = 300f;

		public float LowerDistance = 280f;

		public float FadeOutRate = 1f;

		public float FadeInRate = 1f;

		public bool Inverse;

		public bool RecalculateUnitDistanceInUpdate = true;

		public Color OriginalColor = Color.white;

		public bool UseRealTime = true;

		private List<FadeOutMesh> fadeOutMeshes = new List<FadeOutMesh>();

		// Open Frontier: wormhole billboards honor the same hide-in-cloud
		// flag as the gas cloud billboards (GasCloudBilboardController.
		// BillboardsHidden) - their LODs were visible through the fog.
		// Fades independently and multiplies into the distance fade.
		private float cloudFade = 1f;

		private const float CloudFadeSpeed = 1.5f;

		public float GetDeltaTime()
		{
			if (UseRealTime)
			{
				return RealTime.deltaTime;
			}
			return Time.deltaTime;
		}

		private void Awake()
		{
			activeUnit = GetComponentInParent<ActiveUnit>();
			if (activeUnit == null)
			{
				Debug.LogError("Require active unit", this);
			}
			MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer meshRenderer in componentsInChildren)
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				// URP port: URP Lit exposes _BaseColor (not _Color). Seed
				// the block with the material's own color so the fade only
				// ever animates alpha and never wipes out the albedo.
				Color originalColor = OriginalColor;
				if (meshRenderer.sharedMaterial != null)
				{
					originalColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
				}
				fadeOutMeshes.Add(new FadeOutMesh(meshRenderer, materialPropertyBlock, originalColor));
				SetColour(materialPropertyBlock, meshRenderer, originalColor);
			}
			activeUnit.DrawDistanceChangedImmediate += ActiveUnit_DrawDistanceChanged;
		}

		private void OnDestroy()
		{
			if (activeUnit != null)
			{
				activeUnit.DrawDistanceChangedImmediate -= ActiveUnit_DrawDistanceChanged;
			}
		}

		private void ActiveUnit_DrawDistanceChanged(ActiveUnit sender)
		{
			Apply(immediate: true);
		}

		private void Update()
		{
			if (RecalculateUnitDistanceInUpdate && activeUnit != null)
			{
				activeUnit.UpdateCameraDistance();
			}
			float num = (GasCloudBilboardController.BillboardsHidden ? 0f : 1f);
			if (cloudFade != num)
			{
				cloudFade = Mathf.MoveTowards(cloudFade, num, CloudFadeSpeed * GetDeltaTime());
				RefreshAllColours();
			}
			Apply(immediate: false);
		}

		private void RefreshAllColours()
		{
			for (int i = 0; i < fadeOutMeshes.Count; i++)
			{
				FadeOutMesh fadeOutMesh = fadeOutMeshes[i];
				if (fadeOutMesh.MeshRenderer != null)
				{
					Color color = fadeOutMesh.OriginalColor;
					color.a = fadeOutMesh.CurrentAlpha;
					SetColour(fadeOutMesh.MaterialPropertyBlock, fadeOutMesh.MeshRenderer, color);
				}
			}
		}

		private void Apply(bool immediate)
		{
			if (!(activeUnit != null) || !(activeUnit.LastDistanceFromCamera < float.MaxValue))
			{
				return;
			}
			for (int i = 0; i < fadeOutMeshes.Count; i++)
			{
				FadeOutMesh fadeOutMesh = fadeOutMeshes[i];
				MeshRenderer meshRenderer = fadeOutMesh.MeshRenderer;
				float originalAlpha = fadeOutMesh.OriginalColor.a;
				float num2 = fadeOutMesh.CurrentAlpha;
				if (!(meshRenderer != null))
				{
					continue;
				}
				if (!Inverse)
				{
					if (num2 <= originalAlpha && activeUnit.LastDistanceFromCamera < LowerDistance)
					{
						num2 = (immediate ? originalAlpha : Mathf.Clamp(num2 + FadeInRate * GetDeltaTime(), 0f, originalAlpha));
					}
					else if (num2 >= 0f && activeUnit.LastDistanceFromCamera > UpperDistance)
					{
						num2 = (immediate ? 0f : Mathf.Clamp(num2 - FadeOutRate * GetDeltaTime(), 0f, originalAlpha));
					}
				}
				else if (num2 >= 0f && activeUnit.LastDistanceFromCamera < LowerDistance)
				{
					num2 = (immediate ? 0f : Mathf.Clamp(num2 - FadeOutRate * GetDeltaTime(), 0f, originalAlpha));
				}
				else if (num2 < originalAlpha && activeUnit.LastDistanceFromCamera > UpperDistance)
				{
					num2 = (immediate ? originalAlpha : Mathf.Clamp(num2 + FadeInRate * GetDeltaTime(), 0f, originalAlpha));
				}
				if (num2 != fadeOutMesh.CurrentAlpha || immediate)
				{
					fadeOutMesh.CurrentAlpha = num2;
					fadeOutMeshes[i] = fadeOutMesh;
					Color color = fadeOutMesh.OriginalColor;
					color.a = num2;
					SetColour(fadeOutMesh.MaterialPropertyBlock, meshRenderer, color);
				}
			}
		}

		// Writes alpha x cloudFade: the cloud-hide fade multiplies the
		// distance fade so the two compose instead of overwriting.
		private void SetColour(MaterialPropertyBlock materialPropertyBlock, MeshRenderer meshRenderer, Color color)
		{
			color.a *= cloudFade;
			materialPropertyBlock.SetColor("_BaseColor", color); // URP port: _Color -> _BaseColor
			meshRenderer.SetPropertyBlock(materialPropertyBlock);
		}
	}
}
