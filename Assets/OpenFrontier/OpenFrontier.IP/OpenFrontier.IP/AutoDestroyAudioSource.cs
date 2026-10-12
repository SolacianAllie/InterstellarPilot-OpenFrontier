using UnityEngine;

namespace OpenFrontier.IP
{
	public class AutoDestroyAudioSource : MonoBehaviour
	{
		private AudioSource audioSource;

		private void Start()
		{
			audioSource = gameObject.GetComponent<AudioSource>();
		}

		private void Update()
		{
			if (audioSource == null || !audioSource.isPlaying)
			{
				Object.Destroy(gameObject);
			}
		}
	}
}
