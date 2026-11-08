using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnitVelocityTrail : UnitFX
	{
		private TrailRenderer activeTrailRenderer;

		public float MaxAlpha = 1f;

		public float MaxSpeedAlpha = 20f;

		public float MinAlpha;

		public GameObject TrailRendererPrefab;

		private MaterialPropertyBlock materialPropertyBlock;

		protected override float DrawDistanceMultiplier => 0.7f;

		protected override void OnFxActive()
		{
			base.OnFxActive();
			if (activeTrailRenderer == null)
			{
				materialPropertyBlock = new MaterialPropertyBlock();
				GameObject gameObject = Object.Instantiate(TrailRendererPrefab, transform);
				gameObject.transform.localPosition = Vector3.zero;
				activeTrailRenderer = gameObject.GetComponent<TrailRenderer>();
			}
			activeTrailRenderer.enabled = true;
		}

		protected override void OnFxInactive()
		{
			base.OnFxInactive();
			activeTrailRenderer.enabled = false;
		}

		protected override void update()
		{
			base.update();
			if (IsActive)
			{
				activeTrailRenderer.transform.position = transform.position;
				Rigidbody unitRigidBody = ActiveUnit.UnitRigidBody;
				Vector3 vector = Vector3.zero;
				if (unitRigidBody != null)
				{
					vector = ActiveUnit.UnitRigidBody.linearVelocity;
				}
				float a = Mathf.Lerp(GameController.Instance.GameSettings.VideoSettings.VelocityTrailMinAlpha, GameController.Instance.GameSettings.VideoSettings.VelocityTrailMaxAlpha, Mathf.Clamp01(vector.magnitude / GameController.Instance.GameSettings.VideoSettings.VelocityTrailFullAlphaSpeed));
				activeTrailRenderer.GetPropertyBlock(materialPropertyBlock);
				materialPropertyBlock.SetColor("_BaseColor", new Color(1f, 1f, 1f, a)); // URP port: _TintColor -> _BaseColor
				activeTrailRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
	}
}
