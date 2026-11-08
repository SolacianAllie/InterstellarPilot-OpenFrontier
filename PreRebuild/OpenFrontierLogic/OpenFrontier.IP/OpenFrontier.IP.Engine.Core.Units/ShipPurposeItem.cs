using System;
using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Core.Units
{
	[Serializable]
	public class ShipPurposeItem
	{
		public FactionStrategy FactionStrategy;

		public float Effectiveness = 1f;
	}
}
