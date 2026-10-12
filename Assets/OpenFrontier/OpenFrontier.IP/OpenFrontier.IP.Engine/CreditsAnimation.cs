using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CreditsAnimation : MonoBehaviour
	{
		public bool AllowCreditBeepAudio;

		private int animatedValue = -1;

		public float CreditsAnimMaxLerp = 0.2f;

		public float CreditsAnimMinLerp = 0.1f;

		public int CreditsAnimMinMovement = 50;

		public AudioSource CreditsDownAudioSource;

		public AudioSource CreditsUpAudioSource;

		private EngineASX engine;

		private bool isAnimatingPlayerCredits;

		private float nextCreditsLabelAnimUpdate;

		public float TimeBetweenCreditLabelAnimUpdate = 0.05f;

		public int AnimatedValue => animatedValue;

		private void Start()
		{
			engine = EngineASX.Instance;
			engine.WorldLoaded += engine_WorldLoaded;
		}

		private void engine_WorldLoaded(EngineASX sender)
		{
			engine.WorldLoaded -= engine_WorldLoaded;
			animatedValue = ((engine.LocalPlayer != null) ? engine.LocalPlayer.Credits : 0);
		}

		private void Update()
		{
			GamePlayer localPlayer = engine.LocalPlayer;
			if (!(localPlayer != null))
			{
				return;
			}
			int num = 0;
			if (localPlayer.Faction != null)
			{
				num = localPlayer.Faction.Credits;
			}
			if (isAnimatingPlayerCredits)
			{
				int num2 = num - animatedValue;
				if (num2 == 0)
				{
					StopCreditsAnimation(num);
				}
				else
				{
					if (!(Time.realtimeSinceStartup > nextCreditsLabelAnimUpdate))
					{
						return;
					}
					int num3 = Mathf.Abs(num2);
					int num4 = Mathf.CeilToInt((float)num3 * Random.Range(CreditsAnimMinLerp, CreditsAnimMaxLerp));
					if (num4 < CreditsAnimMinMovement)
					{
						num4 = CreditsAnimMinMovement;
					}
					if (num4 > num3)
					{
						num4 = num3;
					}
					num4 *= (int)Mathf.Sign(num2);
					if (AllowCreditBeepAudio)
					{
						if (num4 > 0)
						{
							CreditsUpAudioSource.Play();
						}
						else
						{
							CreditsDownAudioSource.Play();
						}
					}
					animatedValue += num4;
					nextCreditsLabelAnimUpdate = RealTime.time + TimeBetweenCreditLabelAnimUpdate;
				}
			}
			else if (num != animatedValue)
			{
				isAnimatingPlayerCredits = true;
			}
		}

		private void StopCreditsAnimation(int playerCredits)
		{
			AllowCreditBeepAudio = false;
			isAnimatingPlayerCredits = false;
			animatedValue = playerCredits;
		}
	}
}
