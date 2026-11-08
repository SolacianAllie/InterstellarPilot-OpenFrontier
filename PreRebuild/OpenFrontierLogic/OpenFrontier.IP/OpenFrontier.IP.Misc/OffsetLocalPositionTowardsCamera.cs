using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Misc
{
	public class OffsetLocalPositionTowardsCamera : MonoBehaviour
	{
		public float Distance = 100f;

		public float MinDistanceFromCamera = 50f;

		private void Awake()
		{
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
