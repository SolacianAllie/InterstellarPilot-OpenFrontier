using UnityEngine;

namespace OpenFrontier.IP
{
	public class TimedDeactivator : MonoBehaviour
	{
		private float deactivateTime;

		public float MaxTime = 5f;

		public float MinTime = 3f;

		private void OnEnable()
		{
			deactivateTime = Time.time + Random.Range(MinTime, MaxTime);
		}

		private void Update()
		{
			if (Time.time > deactivateTime)
			{
				gameObject.SetActive(value: false);
			}
		}
	}
}
