using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs.JobTypes
{
	public class ModelCourierJob : ModelJob
	{
		public override JobType JobType => JobType.Courier;

		public ModelUnit PickupUnit { get; set; }

		public ModelUnit DestinationUnit { get; set; }

		public ModelComponentUnitCargoDataItem Cargo { get; set; }
	}
}
