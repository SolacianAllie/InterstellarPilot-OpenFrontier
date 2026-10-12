using System;

namespace OpenFrontier.IP.Avatars
{
	[Serializable]
	public class AvatarProfileLayer
	{
		public AvatarLayerType Layer;

		public bool Optional;

		public float EnabledProbability = 1f;

		public bool EnabledForMale = true;

		public bool EnabledForFemale = true;

		public bool OptionalForMale;

		public bool OptionalForFemale;

		public AvatarAssetStyle Styles;
	}
}
