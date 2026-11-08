using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public static class AudioHelper
	{
		public static AudioSource PlaySound(AudioClip clip, float volume = 1f)
		{
			AudioSource mainCameraAudioSource = GameController.Instance.MainCameraAudioSource;
			mainCameraAudioSource.PlayOneShot(clip, volume);
			return mainCameraAudioSource;
		}
	}
}
