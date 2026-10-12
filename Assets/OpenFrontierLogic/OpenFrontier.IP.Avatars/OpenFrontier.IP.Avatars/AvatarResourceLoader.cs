using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Avatars
{
	public class AvatarResourceLoader : MonoBehaviour
	{
		public string SpritesPath = "Avatars/Textures";

		public List<AvatarLayerType> LayerTypes = new List<AvatarLayerType>();

		private List<AvatarAsset> avatarAssets = new List<AvatarAsset>(64);

		private Dictionary<int, AvatarLayerType> layerTypesById = new Dictionary<int, AvatarLayerType>(24);

		private Dictionary<int, List<AvatarAsset>> assetsByLayerId = new Dictionary<int, List<AvatarAsset>>();

		public List<AvatarAssetColourChannel> ColourChannels = new List<AvatarAssetColourChannel>(8);

		public void Load()
		{
			IEnumerable<Sprite> enumerable = LoadAll<Sprite>(SpritesPath);
			layerTypesById.Clear();
			foreach (AvatarLayerType layerType in LayerTypes)
			{
				layerTypesById.Add(layerType.Id, layerType);
			}
			avatarAssets.Clear();
			foreach (Sprite item in enumerable)
			{
				try
				{
					AvatarAsset avatarAsset = LoadAvatarAssetFromSprite(item);
					if (!assetsByLayerId.TryGetValue(avatarAsset.AvatarLayerType.Id, out var value))
					{
						value = new List<AvatarAsset>();
						assetsByLayerId[avatarAsset.AvatarLayerType.Id] = value;
					}
					value.Add(avatarAsset);
				}
				catch (Exception exception)
				{
					Debug.LogError("Failed to load avatar asset from sprite " + item.name, item);
					Debug.LogException(exception);
				}
			}
		}

		public List<AvatarAsset> GetAssetsByLayerId(int layerId)
		{
			if (assetsByLayerId.TryGetValue(layerId, out var value))
			{
				return value;
			}
			return null;
		}

		private AvatarAsset LoadAvatarAssetFromSprite(Sprite sprite)
		{
			AvatarAsset avatarAsset = new AvatarAsset();
			string[] array = sprite.name.Split(" ");
			int key = int.Parse(array[0].Substring(1)) * 100;
			avatarAsset.AvatarLayerType = layerTypesById[key];
			avatarAsset.AvatarLayerGenderFlags = GetGenderFlags(array[1]);
			avatarAsset.Sprite = sprite;
			avatarAsset.Id = int.Parse(array[3]);
			if (array.Length > 5)
			{
				string text = array[5];
				foreach (AvatarAssetStyle value in Enum.GetValues(typeof(AvatarAssetStyle)))
				{
					if (text.Contains(value.ToString(), StringComparison.InvariantCultureIgnoreCase))
					{
						avatarAsset.Styles |= value;
					}
				}
			}
			if (avatarAsset.Styles == AvatarAssetStyle.None)
			{
				avatarAsset.Styles = AvatarAssetStyle.Normal;
			}
			return avatarAsset;
		}

		private AvatarLayerGenderFlags GetGenderFlags(string v)
		{
			return v switch
			{
				"M" => AvatarLayerGenderFlags.Male, 
				"F" => AvatarLayerGenderFlags.Female, 
				"B" => AvatarLayerGenderFlags.Male | AvatarLayerGenderFlags.Female, 
				_ => throw new Exception("Unknown avatar layer gender flag: \"" + v + "\""), 
			};
		}

		public static IEnumerable<T> LoadAll<T>(string path)
		{
			return Resources.LoadAll(path, typeof(T)).Cast<T>();
		}
	}
}
