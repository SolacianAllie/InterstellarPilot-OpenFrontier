using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.MusicPlayer
{
	public class SimpleMusicPlayer : MonoBehaviour
	{
		public enum PlayerState
		{
			Playing,
			FadingOut
		}

		public float FadeOutDuration = 2f;

		private float fadeOutStartTime;

		private float? customFadeOutDuration = 0.5f;

		private float maxVolume = 1f;

		private PlayingTrack playingTrack;

		private List<Track> queuedTracks = new List<Track>();

		public PlayingTrack PlayingTrack
		{
			get
			{
				return playingTrack;
			}
			private set
			{
				if (playingTrack != value)
				{
					playingTrack = value;
				}
			}
		}

		public float Volume
		{
			get
			{
				return maxVolume;
			}
			set
			{
				maxVolume = value;
				SetTrackVolumeToMax();
			}
		}

		public List<Track> QueuedTracks => queuedTracks;

		public void EnqueueTrack(string audioSourceResourceName, TrackFinishMode finishMode = TrackFinishMode.Discard, bool loop = false)
		{
			Track track = new Track
			{
				TrackName = audioSourceResourceName,
				FinishMode = finishMode,
				Loop = loop
			};
			EnqueueTrack(track);
		}

		public void EnqueueTrack(Track track)
		{
			queuedTracks.Add(track);
		}

		public void InsertInQueue(Track track)
		{
			queuedTracks.Insert(0, track);
		}

		[ContextMenu("Stop")]
		public void StopPlayingTrack()
		{
			customFadeOutDuration = null;
			if (PlayingTrack != null)
			{
				DestroyCurrentTrackAudioSource();
				if (PlayingTrack.Track.FinishMode == TrackFinishMode.Requeue)
				{
					EnqueueTrack(playingTrack.Track);
				}
				PlayingTrack = null;
			}
		}

		[ContextMenu("FadeOutAndDequeue")]
		public void ClearAllTracksWithFadeOut(float? fadeOutDuration = null)
		{
			queuedTracks.Clear();
			if (PlayingTrack != null && PlayingTrack.State == PlayerState.Playing)
			{
				customFadeOutDuration = fadeOutDuration;
				StartFadeOut();
			}
		}

		[ContextMenu("FadeOut")]
		public void FadeOutIfPlaying()
		{
			if (playingTrack != null && playingTrack.State == PlayerState.Playing)
			{
				StartFadeOut();
			}
		}

		private void Update()
		{
			if (playingTrack != null)
			{
				switch (playingTrack.State)
				{
				case PlayerState.Playing:
					if (!ClearPlayingTrackIfInvalid() && !playingTrack.AudioSource.isPlaying)
					{
						if (playingTrack.Track.Loop)
						{
							playingTrack.AudioSource.Play();
						}
						else
						{
							StopPlayingTrack();
						}
					}
					break;
				case PlayerState.FadingOut:
					UpdateFadeOut();
					break;
				}
			}
			else
			{
				CheckQueuedTracks();
			}
		}

		private bool ClearPlayingTrackIfInvalid()
		{
			if (playingTrack.AudioSource == null)
			{
				PlayingTrack = null;
				return true;
			}
			return false;
		}

		private void UpdateFadeOut()
		{
			float num = Time.realtimeSinceStartup - fadeOutStartTime;
			if (num > (customFadeOutDuration ?? FadeOutDuration))
			{
				StopPlayingTrack();
			}
			else
			{
				FadeOutTrackVolume(num);
			}
		}

		private void FadeOutTrackVolume(float elapsedTime)
		{
			float num = customFadeOutDuration ?? FadeOutDuration;
			PlayingTrack.AudioSource.volume = (1f - Mathf.Clamp01(elapsedTime / num)) * maxVolume;
		}

		private void StartFadeOut()
		{
			if (playingTrack != null)
			{
				PlayingTrack.State = PlayerState.FadingOut;
				fadeOutStartTime = Time.realtimeSinceStartup;
			}
		}

		private void CheckQueuedTracks()
		{
			if (queuedTracks.Count > 0)
			{
				Track track = queuedTracks[0];
				queuedTracks.RemoveAt(0);
				string trackName = track.TrackName;
				AudioSource audioSource = EngineASX.LoadMusicAudioSource(trackName);
				if (audioSource != null)
				{
					PlayingTrack = PlayTrack(track, audioSource);
					PlayingTrack.State = PlayerState.Playing;
				}
				else
				{
					Debug.LogWarning("MusicPlayer could not load up audiosource from prefab: " + trackName);
				}
				SetTrackVolumeToMax();
			}
		}

		private void DestroyCurrentTrackAudioSource()
		{
			if (playingTrack != null && playingTrack.AudioSource != null)
			{
				Object.Destroy(playingTrack.AudioSource.gameObject);
				playingTrack.AudioSource = null;
			}
		}

		private PlayingTrack PlayTrack(Track track, AudioSource prefab)
		{
			AudioSource audioSource = Object.Instantiate(prefab);
			audioSource.transform.SetParent(transform);
			audioSource.transform.localPosition = Vector3.zero;
			audioSource.volume = maxVolume;
			return new PlayingTrack
			{
				AudioSource = audioSource,
				Track = track
			};
		}

		private void SetTrackVolumeToMax()
		{
			if (playingTrack != null && playingTrack.AudioSource != null)
			{
				playingTrack.AudioSource.volume = maxVolume;
			}
		}
	}
}
