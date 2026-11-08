using UnityEngine;
using UnityEngine.SceneManagement;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI
{
	public class LogoSplash : Fader
	{
		public bool SkipWithMouse = true;

		public float StartDelay = 1f;

		private float startTime;

		public string TargetSceneName;

		protected override void OnFinished()
		{
			base.OnFinished();
			SceneManager.LoadScene(TargetSceneName, LoadSceneMode.Single);
		}

		protected override void OnStarted()
		{
			base.OnStarted();
			startTime = Time.time;
		}

		protected override void OnUpdating()
		{
			base.OnUpdating();
			if (State == SplashState.NotStarted && Time.time - startTime > StartDelay)
			{
				StartAnimation();
			}
			else if (State == SplashState.Started && SkipWithMouse && Input.GetMouseButton(0))
			{
				FinishAnimation();
			}
		}
	}
}
