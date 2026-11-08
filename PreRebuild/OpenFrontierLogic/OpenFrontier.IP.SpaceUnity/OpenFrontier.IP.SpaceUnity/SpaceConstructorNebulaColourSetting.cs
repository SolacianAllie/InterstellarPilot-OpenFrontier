using System;
using OpenFrontier.IP.Common;
using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.SpaceUnity
{
	[Serializable]
	public class SpaceConstructorNebulaColourSetting : IWeighted
	{
		public NebulaColour NebulaColours;

		public float Weighting;

		public float Weight => Weighting;
	}
}
