using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Avatars
{
	public class AvatarData
	{
		public List<AvatarDataAsset> DataItems = new List<AvatarDataAsset>();

		private Dictionary<int, Color> colourData = new Dictionary<int, Color>(8);

		public void SetColour(AvatarAssetColourChannel colourChannel, Color colour)
		{
			colourData[colourChannel.Id] = colour;
		}

		public Color GetColour(AvatarAssetColourChannel colourChannel)
		{
			if (colourData.TryGetValue(colourChannel.Id, out var value))
			{
				return value;
			}
			return Color.white;
		}
	}
}
