using UnityEngine;

namespace OpenFrontier.IP
{
	public class SineRotator : MonoBehaviour
	{
		public GameObject Target;

		public Vector3 MinRotation = new Vector3(0f, -10f, 0f);

		public Vector3 MaxRotation = new Vector3(0f, 10f, 0f);

		public float Speed = 0.5f;

		public bool UseRealTime = true;

		private void Update()
		{
			if (Target != null)
			{
				float num = (UseRealTime ? RealTime.time : Time.time);
				Target.transform.localRotation = Quaternion.Euler(Vector3.Lerp(MinRotation, MaxRotation, (1f + Mathf.Sin(num * Speed)) / 2f));
			}
		}

		public void Reset()
		{
			Target.transform.localRotation = Quaternion.identity;
		}
	}
}
