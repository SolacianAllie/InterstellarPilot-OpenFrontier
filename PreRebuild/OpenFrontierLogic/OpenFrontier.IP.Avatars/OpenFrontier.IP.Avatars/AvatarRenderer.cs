using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Avatars
{
	public class AvatarRenderer : MonoBehaviour
	{
		private AvatarRenderLayer[] renderLayers;

		private void Awake()
		{
			renderLayers = GetComponentsInChildren<AvatarRenderLayer>();
		}

		public void SetAvatar(AvatarData avatarData)
		{
			AvatarRenderLayer[] array = renderLayers;
			foreach (AvatarRenderLayer layer in array)
			{
				AvatarDataAsset avatarDataAsset = avatarData.DataItems.FirstOrDefault((AvatarDataAsset e) => e.LayerType == layer.AvatarLayerType);
				if (avatarDataAsset != null)
				{
					layer.Image.enabled = true;
					layer.Image.sprite = avatarDataAsset.Asset.Sprite;
					if (layer.AvatarLayerType.ColourChannel != null)
					{
						layer.Image.color = avatarData.GetColour(layer.AvatarLayerType.ColourChannel);
					}
				}
				else
				{
					layer.Image.enabled = false;
				}
			}
		}
	}
}
