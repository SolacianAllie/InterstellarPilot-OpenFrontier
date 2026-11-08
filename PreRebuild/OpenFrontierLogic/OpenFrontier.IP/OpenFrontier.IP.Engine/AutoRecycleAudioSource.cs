using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(AudioSource))]
	public class AutoRecycleAudioSource : MonoBehaviour
	{
		private AudioSource audioSource;

		private void Awake()
		{
			audioSource = gameObject.GetComponent<AudioSource>();
		}

		private void Update()
		{
			if (!audioSource.isPlaying || audioSource.volume == 0f)
			{
				EngineASX instance = EngineASX.Instance;
				if (instance.Pooler != null)
				{
					instance.Pooler.RecyclePoolObject(gameObject);
				}
			}
		}
	}
}
