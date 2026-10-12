using UnityEngine;

namespace OpenFrontier.IP.Engine.ActiveUnitFx
{
	public class GasCloudBilboardController : MonoBehaviour
	{
		public float Distance = 100f;

		public float MinDistanceFromCamera = 50f;

		// Open Frontier: set by GasCloudController - when the camera is
		// inside ANY gas cloud, every cloud billboard fades out (distant
		// billboards read as crisp shapes floating inside the fog you are
		// flying through). Each controller fades its own renderers via a
		// MaterialPropertyBlock - the GameObject stays active so the fade
		// reverses on exit (toggling SetActive would kill Update and the
		// billboards would never come back).
		public static bool BillboardsHidden;

		private const float FadeSpeed = 1.5f;

		private float fade = 1f;

		private MeshRenderer[] childRenderers;

		private MaterialPropertyBlock fadeBlock;

		private void Awake()
		{
			childRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
			fadeBlock = new MaterialPropertyBlock();
			EngineASX.Instance.CameraMoved += Instance_CameraMoved;
		}

		private void Instance_CameraMoved(EngineASX sender)
		{
			Reposition();
		}

		private void OnDestroy()
		{
			EngineASX.Instance.CameraMoved -= Instance_CameraMoved;
		}

		private void FixedUpdate()
		{
			Reposition();
		}

		private void Update()
		{
			float num = (BillboardsHidden ? 0f : 1f);
			if (fade != num)
			{
				fade = Mathf.MoveTowards(fade, num, FadeSpeed * Time.deltaTime);
				ApplyFade();
			}
			if (GameController.Instance != null && GameController.Instance.MainCamera != null)
			{
				Vector3 forward = Vector3.Normalize(transform.parent.position - GameController.Instance.MainCamera.transform.position);
				transform.rotation = Quaternion.LookRotation(forward);
			}
		}

		private void ApplyFade()
		{
			if (childRenderers == null)
			{
				return;
			}
			fadeBlock.SetFloat("_GlobalFade", fade);
			bool enabled = fade > 0.001f;
			MeshRenderer[] array = childRenderers;
			foreach (MeshRenderer meshRenderer in array)
			{
				if (meshRenderer != null)
				{
					meshRenderer.SetPropertyBlock(fadeBlock);
					meshRenderer.enabled = enabled;
				}
			}
		}

		private void Reposition()
		{
			if (transform.parent != null && GameController.Instance != null && GameController.Instance.MainCamera != null)
			{
				Vector3 vector = GameController.Instance.MainCamera.transform.position - transform.parent.position;
				if (vector != Vector3.zero)
				{
					float num = Mathf.Min(Distance, vector.magnitude - MinDistanceFromCamera);
					transform.localPosition = Vector3.Normalize(vector) * num;
				}
			}
		}
	}
}
