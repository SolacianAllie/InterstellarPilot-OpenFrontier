using UnityEngine;

namespace OpenFrontier.IP
{
	[RequireComponent(typeof(CanvasGroup))]
	public class FadeCanvasGroup : MonoBehaviour
	{
		public enum FadeCanvasGroupState
		{
			In,
			Displaying,
			Out,
			Done
		}

		private CanvasGroup canvasGroup;

		public bool UseRealTime = true;

		public float FadeInTime = 2f;

		public float FadeOutTime = 2f;

		public float DisplayTime = 5f;

		private FadeCanvasGroupState state;

		public FadeCanvasGroupState State => state;

		private void Awake()
		{
			canvasGroup = GetComponent<CanvasGroup>();
			canvasGroup.alpha = 0f;
		}

		private void Update()
		{
			float timeDelta = GetTimeDelta(UseRealTime);
			switch (state)
			{
			case FadeCanvasGroupState.In:
				FadeIn(timeDelta);
				break;
			case FadeCanvasGroupState.Out:
				FadeOut(timeDelta);
				break;
			}
		}

		private float FadeOut(float timeDelta)
		{
			float num = 1f / FadeInTime;
			canvasGroup.alpha -= num * timeDelta;
			if (canvasGroup.alpha <= 1f)
			{
				canvasGroup.alpha = 0f;
				state = FadeCanvasGroupState.Done;
			}
			return num;
		}

		private float FadeIn(float timeDelta)
		{
			float num = 1f / FadeInTime;
			canvasGroup.alpha += num * timeDelta;
			if (canvasGroup.alpha >= 1f)
			{
				canvasGroup.alpha = 1f;
				state = FadeCanvasGroupState.Displaying;
			}
			return num;
		}

		private float GetTimeDelta(bool useRealTime)
		{
			if (useRealTime)
			{
				return RealTime.deltaTime;
			}
			return Time.deltaTime;
		}
	}
}
