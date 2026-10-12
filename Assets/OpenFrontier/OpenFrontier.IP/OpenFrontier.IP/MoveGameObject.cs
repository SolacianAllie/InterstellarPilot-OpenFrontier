using UnityEngine;

namespace OpenFrontier.IP
{
	public class MoveGameObject : MonoBehaviour
	{
		public bool DisableOnReachTarget;

		public float MoveSpeed = 10f;

		public bool SnapOnDisable = true;

		public GameObject TargetObject;

		public Vector3 TargetPosition = Vector3.zero;

		public bool UseRealTime;

		public void Update()
		{
			UpdateTargetPosition();
			Vector3 value = TargetPosition - transform.position;
			float magnitude = value.magnitude;
			if (!(magnitude > 0f))
			{
				return;
			}
			float num = (UseRealTime ? RealTime.deltaTime : Time.deltaTime);
			float num2 = MoveSpeed * num;
			if (num2 < magnitude)
			{
				transform.position += Vector3.Normalize(value) * num2;
				return;
			}
			SnapToTarget();
			if (DisableOnReachTarget)
			{
				enabled = false;
			}
		}

		private void OnDisable()
		{
			if (SnapOnDisable)
			{
				UpdateTargetPosition();
				SnapToTarget();
			}
		}

		private void SnapToTarget()
		{
			transform.position = TargetPosition;
		}

		private void UpdateTargetPosition()
		{
			if (TargetObject != null)
			{
				TargetPosition = TargetObject.transform.position;
			}
		}
	}
}
