using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public static class CreateMissionFromJobType
	{
		public static ModelMission Create(JobType jobType)
		{
			return jobType switch
			{
				JobType.DeliverShip => new ModelDeliverShipMission(), 
				JobType.Courier => new ModelCourierMission(), 
				JobType.DestroyGroup => new ModelDestroyUnitsMission(), 
				JobType.Breakdown => new ModelBreakdownMission(), 
				_ => new ModelMission(), 
			};
		}
	}
}
