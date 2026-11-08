using UnityEngine;

namespace OpenFrontier.IP.Avatars
{
	public class AvatarAsset
	{
		public AvatarLayerGenderFlags AvatarLayerGenderFlags = AvatarLayerGenderFlags.Male | AvatarLayerGenderFlags.Female;

		public AvatarLayerType AvatarLayerType;

		public Sprite Sprite;

		public AvatarAssetStyle Styles;

		public int Id { get; set; }

		public bool MatchesGender(AvatarLayerGenderFlags gender)
		{
			return (AvatarLayerGenderFlags & gender) != 0;
		}

		public bool MatchesStyles(AvatarAssetStyle styles)
		{
			if (Styles == AvatarAssetStyle.None || styles == AvatarAssetStyle.None)
			{
				return true;
			}
			return (Styles & styles) == Styles;
		}
	}
}
