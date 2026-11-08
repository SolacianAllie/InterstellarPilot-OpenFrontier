using System;
using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SpaceUnity
{
	[Serializable]
	public class SpaceConstructorParams
	{
		public NebulaBrightness NebulaBrightness = NebulaBrightness.VERY_DARK | NebulaBrightness.DARK | NebulaBrightness.MEDIUM | NebulaBrightness.BRIGHT | NebulaBrightness.VERY_BRIGHT;

		public NebulaColour NebulaColors = NebulaColour.BLUE | NebulaColour.PINK | NebulaColour.PURPLE | NebulaColour.GREEN | NebulaColour.YELLOW | NebulaColour.ORANGE | NebulaColour.RED | NebulaColour.CYAN;

		public NebulaStyle NebulaStyles = NebulaStyle.CLOUDY | NebulaStyle.STREAKY | NebulaStyle.GLITTERY | NebulaStyle.DARKMATTER;

		public float NebulaComplexity = 0.6f;

		public int NebulaCount = 28;

		public int NebulaTextureCount = 10;

		public float StarsIntensity = 1f;

		public StarsCount StarsCount = StarsCount.MEDIUM | StarsCount.HIGH;
	}
}
