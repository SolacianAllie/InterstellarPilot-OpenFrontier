using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Avatars
{
	[RequireComponent(typeof(Image))]
	public class AvatarRenderLayer : MonoBehaviour
	{
		public AvatarLayerType AvatarLayerType;

		private Image image;

		public Image Image => image;

		private void Awake()
		{
			image = GetComponent<Image>();
		}
	}
}
