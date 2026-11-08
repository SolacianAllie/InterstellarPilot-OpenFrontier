using UnityEngine;

namespace OpenFrontier.IP.Engine.AmbientSounds
{
	public class AmbientSound
	{
		private float volumeMultiplier = 1f;

		public AudioSource AudioSource { get; set; }

		public Transform Parent { get; set; }

		public float MaxVolume
		{
			get
			{
				return volumeMultiplier;
			}
			set
			{
				volumeMultiplier = value;
			}
		}
	}
}
