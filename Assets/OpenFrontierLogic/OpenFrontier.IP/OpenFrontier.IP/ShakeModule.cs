using UnityEngine;

namespace OpenFrontier.IP
{
	public class ShakeModule : MonoBehaviour
	{
		private float currentMagnitude;

		public Vector3 CurrentOffset = Vector3.zero;

		private Vector3 desiredOffset = Vector3.zero;

		public float MaxMagnitude = 2f;

		public float MaxMoveRate = 1f;

		public float MaxShakeTime = 0.2f;

		public float MinMoveRate = 0.5f;

		public float MinShakeTime = 0.1f;

		private float moveRate = 1f;

		private float nextShakeTime;

		private float shakeEndTime;

		private float shakeStartTime;

		public void Update()
		{
			if (Time.time > shakeEndTime)
			{
				CurrentOffset = Vector3.zero;
				enabled = false;
				return;
			}
			if (Time.time > nextShakeTime)
			{
				float num = (Time.time - shakeEndTime) / (shakeEndTime - shakeStartTime) * currentMagnitude;
				nextShakeTime = Time.time + Random.Range(MinShakeTime, MaxShakeTime);
				desiredOffset = Random.rotation * (Vector3.forward * num) * Random.value;
				moveRate = Random.Range(MinMoveRate, MaxMoveRate);
			}
			CurrentOffset = Vector3.Lerp(CurrentOffset, desiredOffset, moveRate * Time.deltaTime);
		}

		public void StartShake(float duration, float magnitude)
		{
			currentMagnitude = Mathf.Clamp(magnitude, 0f, MaxMagnitude);
			enabled = true;
			shakeStartTime = Time.time;
			shakeEndTime = shakeStartTime + duration;
		}
	}
}
