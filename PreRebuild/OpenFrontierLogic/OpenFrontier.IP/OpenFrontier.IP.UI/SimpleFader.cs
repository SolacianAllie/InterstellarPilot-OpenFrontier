using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class SimpleFader : MonoBehaviour
	{
		public enum FadeState
		{
			Inactive,
			Showing,
			Fading,
			Faded
		}

		public delegate void FadeCompleteHandler(SimpleFader sender);

		public bool DestroyOnComplete;

		private float elapsedTime;

		public Color EndColor = Color.black;

		public float FadeDuration = 2f;

		public bool PlayOnAwake;

		public Color StartColor = Color.white;

		private FadeState state;

		public Texture2D Texture;

		public bool UseRealTime = true;

		public float WaitDuration = 2f;

		public FadeState State
		{
			get
			{
				return state;
			}
			set
			{
				state = value;
				elapsedTime = 0f;
			}
		}

		public event FadeCompleteHandler FadeComplete;

		public void StartFade()
		{
			State = FadeState.Showing;
		}

		private void Awake()
		{
			if (PlayOnAwake)
			{
				StartFade();
			}
		}

		private float GetDeltaTime()
		{
			if (!UseRealTime)
			{
				return Time.deltaTime;
			}
			return RealTime.deltaTime;
		}

		private void Update()
		{
			elapsedTime += GetDeltaTime();
			switch (state)
			{
			case FadeState.Showing:
				if (elapsedTime > WaitDuration)
				{
					State = FadeState.Fading;
				}
				break;
			case FadeState.Fading:
				if (elapsedTime > FadeDuration)
				{
					OnFadeComplete();
				}
				break;
			}
		}

		private void OnFadeComplete()
		{
			State = FadeState.Faded;
			if (FadeComplete != null)
			{
				FadeComplete(this);
			}
			if (DestroyOnComplete)
			{
				Object.Destroy(gameObject);
			}
		}

		private void OnGUI()
		{
			switch (state)
			{
			case FadeState.Showing:
				GUI.color = StartColor;
				GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture);
				GUI.color = Color.white;
				break;
			case FadeState.Fading:
				GUI.color = Color.Lerp(StartColor, EndColor, elapsedTime / FadeDuration);
				GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture);
				GUI.color = Color.white;
				break;
			}
		}
	}
}
