using UnityEngine;

namespace OpenFrontier.IP.Engine.Pooling
{
	public class FadeOutAudioSource : MonoBehaviour
	{
		private AudioSource audioSource;

		private void Awake()
		{
			audioSource = GetComponent<AudioSource>();
		}

		private void Update()
		{
			if (audioSource != null)
			{
				float volume = audioSource.volume;
				volume -= Time.deltaTime * 2f;
				if (volume <= 0f)
				{
					audioSource.volume = 0f;
					enabled = false;
				}
				else
				{
					audioSource.volume = volume;
				}
			}
			else
			{
				enabled = false;
			}
		}
	}
}
