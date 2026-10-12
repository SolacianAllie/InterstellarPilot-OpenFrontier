using UnityEngine;

namespace OpenFrontier.IP
{
	public class SineTargetScaler : MonoBehaviour
	{
		public Vector2 MinScaleMultiplier = Vector2.one;

		public Vector2 MaxScaleMultiplier = new Vector2(1.5f, 1.5f);

		public float TimeMultiplier = 2f;

		[SerializeField]
		private Transform targetTransform;

		private Vector3? defaultScale;

		public bool UseRealTime;

		public Transform Graphic
		{
			get
			{
				return targetTransform;
			}
			set
			{
				if (targetTransform != value)
				{
					targetTransform = value;
					if (targetTransform != null)
					{
						defaultScale = targetTransform.transform.localScale;
					}
				}
			}
		}

		private void Update()
		{
			if (targetTransform != null)
			{
				if (!defaultScale.HasValue)
				{
					defaultScale = targetTransform.transform.localScale;
				}
				float num = Mathf.Sin((UseRealTime ? RealTime.time : Time.time) * TimeMultiplier);
				Vector2 vector = Vector2.Lerp(MinScaleMultiplier, MaxScaleMultiplier, (num + 1f) / 2f);
				targetTransform.transform.localScale = new Vector3(defaultScale.Value.x * vector.x, defaultScale.Value.y * vector.y, defaultScale.Value.z);
			}
		}
	}
}
