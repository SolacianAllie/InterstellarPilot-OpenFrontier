using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs.JobTypes
{
	public class ModelDestroyFleetJob : ModelJob
	{
		public override JobType JobType => JobType.DestroyGroup;

		public ModelFleetSpawnParams FleetSpawnParams { get; set; }
	}
}
