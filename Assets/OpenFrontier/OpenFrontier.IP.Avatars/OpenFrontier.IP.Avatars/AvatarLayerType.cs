using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Avatars
{
	public class AvatarLayerType : MonoBehaviour
	{
		[FormerlySerializedAs("Order")]
		public int Id;

		public string Name;

		public AvatarAssetColourChannel ColourChannel;
	}
}
