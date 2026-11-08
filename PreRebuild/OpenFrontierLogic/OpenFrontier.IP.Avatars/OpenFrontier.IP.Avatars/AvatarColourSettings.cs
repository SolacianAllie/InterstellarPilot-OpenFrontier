using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Avatars
{
	public class AvatarColourSettings : MonoBehaviour
	{
		public List<AvatarChannelColours> ChannelColours2;

		private static List<AvatarSettingsChannelColour> channelColoursCache = new List<AvatarSettingsChannelColour>(10);

		internal Color GetRandomColourChannelColour(AvatarAssetColourChannel colourChannel, AvatarLayerGenderFlags genderFlags, System.Random random)
		{
			AvatarChannelColours avatarChannelColours = ChannelColours2.FirstOrDefault((AvatarChannelColours e) => e.ColourChannel.Id == colourChannel.Id);
			channelColoursCache.Clear();
			channelColoursCache.AddRange(avatarChannelColours.WeightedColours.Where((AvatarSettingsChannelColour e) => (e.GenderFlags & genderFlags) != 0));
			return channelColoursCache.GetRandomWeighted(random).Color;
		}
	}
}
