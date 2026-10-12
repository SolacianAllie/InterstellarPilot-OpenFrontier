using UnityEngine;

namespace OpenFrontier.IP
{
	public class WormholeAnimator : MonoBehaviour
	{
		public delegate void AnimationFinishedHandler(WormholeAnimator sender);

		public AudioSource EntryAudioSourcePrefab;

		public AudioSource FinishAudioSourcePrefab;

		public MoveGameObject Mover;

		public event AnimationFinishedHandler AnimationFinished;

		private void Awake()
		{
			Object.Instantiate(EntryAudioSourcePrefab);
		}

		private void Update()
		{
			if (!Mover.enabled)
			{
				if (AnimationFinished != null)
				{
					AnimationFinished(this);
				}
				if (FinishAudioSourcePrefab != null)
				{
					Object.Instantiate(FinishAudioSourcePrefab.gameObject);
				}
				enabled = false;
			}
		}
	}
}
