using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions
{
	public class ModelDeliverShipMission : ModelMission
	{
		public override MissionType MissionType => MissionType.DeliverShip;

		public ModelUnitClass UnitClass { get; set; }

		public ModelUnit DestinationUnit { get; set; }
	}
}
