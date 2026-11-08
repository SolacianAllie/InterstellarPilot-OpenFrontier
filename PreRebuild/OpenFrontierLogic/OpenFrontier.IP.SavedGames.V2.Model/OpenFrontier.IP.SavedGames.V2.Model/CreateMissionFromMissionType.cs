using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public static class CreateMissionFromMissionType
	{
		public static ModelMission Create(MissionType jobType)
		{
			return jobType switch
			{
				MissionType.DeliverShip => new ModelDeliverShipMission(), 
				MissionType.Courier => new ModelCourierMission(), 
				MissionType.DestroyGroup => new ModelDestroyUnitsMission(), 
				MissionType.Breakdown => new ModelBreakdownMission(), 
				_ => new ModelMission(), 
			};
		}
	}
}
