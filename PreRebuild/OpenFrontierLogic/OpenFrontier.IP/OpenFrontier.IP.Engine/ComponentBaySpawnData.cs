using System;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class ComponentBaySpawnData
	{
		public float InitialCondition = 1f;

		public ComponentBay TargetBay;
	}
}
