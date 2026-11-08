using System;
using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Factions
{
	[Serializable]
	public class FactionAIBuildInfo
	{
		public double CreationTime;

		public int RpCost;

		private List<UnitClass> unitClasses = new List<UnitClass>();

		public List<UnitClass> UnitClasses => unitClasses;

		public static FactionAIBuildInfo Create(EngineASX engine)
		{
			return new FactionAIBuildInfo
			{
				CreationTime = engine.ScenarioElapsedTime
			};
		}

		public override string ToString()
		{
			return $"{UnitClasses.Count} ships. Cost: {RpCost}";
		}
	}
}
