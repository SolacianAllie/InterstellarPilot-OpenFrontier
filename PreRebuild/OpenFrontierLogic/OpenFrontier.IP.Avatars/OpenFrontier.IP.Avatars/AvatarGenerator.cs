using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Avatars
{
	public class AvatarGenerator : MonoBehaviour
	{
		public AvatarResourceLoader AvatarResourceLoader;

		public AvatarData GenerateRandom(AvatarProfile profile, bool isMale, System.Random random)
		{
			if (profile == null)
			{
				throw new Exception("profile is required");
			}
			AvatarData avatarData = new AvatarData();
			AvatarLayerGenderFlags genderFlags = (isMale ? AvatarLayerGenderFlags.Male : AvatarLayerGenderFlags.Female);
			foreach (AvatarLayerType layerType in AvatarResourceLoader.LayerTypes)
			{
				AvatarProfileLayer profileLayer = profile.ProfileLayers.FirstOrDefault((AvatarProfileLayer e) => e.Layer.Id == layerType.Id);
				if (profileLayer == null)
				{
					continue;
				}
				switch (genderFlags)
				{
				case AvatarLayerGenderFlags.Male:
					if (!profileLayer.EnabledForMale || (profileLayer.OptionalForMale && random.NextFloat() >= profileLayer.EnabledProbability))
					{
						continue;
					}
					break;
				case AvatarLayerGenderFlags.Female:
					if (!profileLayer.EnabledForFemale || (profileLayer.OptionalForFemale && random.NextFloat() >= profileLayer.EnabledProbability))
					{
						continue;
					}
					break;
				}
				List<AvatarAsset> assetsByLayerId = AvatarResourceLoader.GetAssetsByLayerId(layerType.Id);
				if (assetsByLayerId != null && assetsByLayerId.Any())
				{
					AvatarAsset random2 = assetsByLayerId.Where((AvatarAsset e) => e.MatchesGender(genderFlags) && e.MatchesStyles(profileLayer.Styles)).GetRandom(random);
					if (random2 != null)
					{
						avatarData.DataItems.Add(new AvatarDataAsset
						{
							Asset = random2,
							LayerType = layerType
						});
					}
				}
			}
			foreach (AvatarAssetColourChannel colourChannel in AvatarResourceLoader.ColourChannels)
			{
				Color randomColourChannelColour = profile.ColourSettings.GetRandomColourChannelColour(colourChannel, genderFlags, random);
				avatarData.SetColour(colourChannel, randomColourChannelColour);
			}
			return avatarData;
		}
	}
}
