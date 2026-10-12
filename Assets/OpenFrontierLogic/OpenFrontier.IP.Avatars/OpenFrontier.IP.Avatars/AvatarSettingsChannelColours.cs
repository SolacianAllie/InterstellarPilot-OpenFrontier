using System;
using System.Collections.Generic;

namespace OpenFrontier.IP.Avatars
{
	[Serializable]
	public class AvatarSettingsChannelColours
	{
		public AvatarAssetColourChannel ColourChannel;

		public List<AvatarSettingsChannelColour> WeightedColours;
	}
}
