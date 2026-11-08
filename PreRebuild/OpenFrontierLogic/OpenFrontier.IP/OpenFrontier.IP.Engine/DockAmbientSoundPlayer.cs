using System.Collections.Generic;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class DockAmbientSoundPlayer : MonoBehaviour
	{
		private struct PlayingSound
		{
			public AudioSource audioSource;
		}

		public Unit CurrentShip;

		public int MaxConcurrentSounds = 3;

		public float MaxTimeBeforePlay = 4f;

		public float MinTimeBeforePlay = 1f;

		private float nextSoundPlay;

		private List<PlayingSound> playingSounds = new List<PlayingSound>();

		private AudioSource TryGetDockAmbientSound()
		{
			if (CurrentShip != null && CurrentShip.ActiveUnit != null && CurrentShip.ActiveUnit.ActiveUnitClass != null)
			{
				AudioSource random = CurrentShip.ActiveUnit.ActiveUnitClass.DockedAmbientSounds.GetRandom();
				if (random != null)
				{
					GameObject gameObject = Object.Instantiate(random.gameObject, CurrentShip.transform.position, Quaternion.identity);
					gameObject.transform.SetParent(transform, worldPositionStays: true);
					return gameObject.GetComponent<AudioSource>();
				}
			}
			return null;
		}

		private void Update()
		{
			if (playingSounds.Count < MaxConcurrentSounds && Time.realtimeSinceStartup > nextSoundPlay)
			{
				AudioSource audioSource = TryGetDockAmbientSound();
				if (audioSource != null)
				{
					audioSource.Play();
					playingSounds.Add(new PlayingSound
					{
						audioSource = audioSource
					});
				}
				nextSoundPlay = Time.realtimeSinceStartup + Random.Range(MinTimeBeforePlay, MaxTimeBeforePlay);
			}
			for (int i = 0; i < playingSounds.Count; i++)
			{
				if (!playingSounds[i].audioSource.isPlaying)
				{
					Object.Destroy(playingSounds[i].audioSource.gameObject);
					playingSounds.RemoveAt(i);
					i--;
				}
			}
		}

		private void OnDisable()
		{
			DestroyDockAmbientSounds();
		}

		private void DestroyDockAmbientSounds()
		{
			while (playingSounds.Count > 0)
			{
				if (playingSounds[0].audioSource != null)
				{
					Object.Destroy(playingSounds[0].audioSource.gameObject);
					playingSounds.RemoveAt(0);
				}
			}
		}
	}
}
