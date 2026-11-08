using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class SkirmishScenarioData : ScenarioLoadData
	{
		[FormerlySerializedAs("Scene")]
		public Sector Sector;

		public List<SkirmishTeamParams> Teams;

		public bool RandomShipComponents;
	}
}
