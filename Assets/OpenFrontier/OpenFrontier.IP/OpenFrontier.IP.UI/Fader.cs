using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class Fader : MonoBehaviour
	{
		public enum SplashState
		{
			NotStarted,
			Started,
			Finished
		}

		public enum TextureStretchMode
		{
			OriginalSize,
			FitScreen
		}

		public delegate void FinishedHandler(Fader sender);

		public Color EndColor = new Color(1f, 1f, 1f, 1f);

		public float FadeDuration = 1f;

		public Color InitialColor = new Color(1f, 1f, 1f, 0f);

		public bool StartAnimOnInitialise = true;

		private SplashState state;

		private float stateChangeTime;

		public TextureStretchMode StretchMode;

		public Texture2D Texture;

		public float WaitTime = 1f;

		public SplashState State => state;

		public event FinishedHandler Finished;

		public void FinishAnimation()
		{
			SetState(SplashState.Finished);
		}

		public void StartAnimation()
		{
			SetState(SplashState.Started);
		}

		public void SetState(SplashState newState)
		{
			state = newState;
			stateChangeTime = Time.time;
			if (state == SplashState.Finished)
			{
				OnFinished();
				if (Finished != null)
				{
					Finished(this);
				}
			}
		}

		protected virtual void OnStarted()
		{
		}

		protected virtual void OnUpdating()
		{
		}

		protected virtual void OnFinished()
		{
		}

		private void Start()
		{
			if (StartAnimOnInitialise)
			{
				SetState(SplashState.Started);
			}
			OnStarted();
		}

		private void Update()
		{
			OnUpdating();
			if (state == SplashState.Started && Time.time - stateChangeTime > FadeDuration + WaitTime)
			{
				FinishAnimation();
			}
		}

		private void OnGUI()
		{
			if (Texture != null && state == SplashState.Started && state != SplashState.Finished)
			{
				float drawColor = Time.time - stateChangeTime;
				SetDrawColor(drawColor);
				GUI.DrawTexture(GetDrawPosition(), Texture);
				GUI.color = Color.white;
			}
		}

		private Rect GetDrawPosition()
		{
			Rect result = default;
			switch (StretchMode)
			{
			case TextureStretchMode.OriginalSize:
				result = new Rect((float)Screen.width / 2f - (float)Texture.width / 2f, (float)Screen.height / 2f - (float)Texture.height / 2f, Texture.width, Texture.height);
				break;
			case TextureStretchMode.FitScreen:
				result = new Rect(0f, 0f, Screen.width, Screen.height);
				break;
			}
			return result;
		}

		private void SetDrawColor(float elapsedTime)
		{
			float num = 1f;
			if (state != SplashState.Finished)
			{
				num = elapsedTime / FadeDuration;
				if (num > 1f)
				{
					num = 1f;
				}
			}
			GUI.color = Color.Lerp(InitialColor, EndColor, num);
		}
	}
}
