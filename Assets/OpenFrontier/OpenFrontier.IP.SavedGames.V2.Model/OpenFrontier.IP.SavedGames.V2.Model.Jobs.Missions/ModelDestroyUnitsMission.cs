using System.Collections.Generic;
using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions
{
	public class ModelDestroyUnitsMission : ModelMission
	{
		public override MissionType MissionType => MissionType.DestroyGroup;

		public List<ModelUnit> TargetUnits { get; set; } = new List<ModelUnit>();

		public bool HasSetGroupHostileToPlayer { get; set; }

		public ModelFaction TargetFaction { get; set; }

		public ModelSector TargetSector { get; set; }

		public ModelFleet TargetFleet { get; set; }
	}
}
