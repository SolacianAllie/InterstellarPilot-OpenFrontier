using System;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Avatars
{
	[Serializable]
	public class AvatarSettingsChannelColour : IWeighted
	{
		public Color Color = Color.white;

		public float Weighting = 1f;

		public AvatarLayerGenderFlags GenderFlags = AvatarLayerGenderFlags.Male | AvatarLayerGenderFlags.Female;

		public float Weight => Weighting;
	}
}
