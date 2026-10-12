using UnityEngine;

namespace OpenFrontier.IP.MusicPlayer
{
	public class PlayingTrack
	{
		public AudioSource AudioSource { get; set; }

		public Track Track { get; set; }

		public SimpleMusicPlayer.PlayerState State { get; set; }
	}
}
