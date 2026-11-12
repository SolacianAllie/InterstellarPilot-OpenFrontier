using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.SpaceUnity.Nebulas
{
	public class NebulaAsset : MonoBehaviour
	{
		public bool Enabled = true;

		public string Group;

		public int TypeId = -1;

		public int VariantId = -1;

		public NebulaBrightness Brightness = NebulaBrightness.BRIGHT;

		public NebulaStyle Style = NebulaStyle.CLOUDY;

		public NebulaColour Colour = NebulaColour.BLUE;
	}
}
