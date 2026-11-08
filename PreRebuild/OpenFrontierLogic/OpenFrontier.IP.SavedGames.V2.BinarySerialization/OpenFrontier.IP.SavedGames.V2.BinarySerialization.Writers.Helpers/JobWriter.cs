using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.JobTypes;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class JobWriter
	{
		public static void Write(BinaryWriter writer, ModelJob job)
		{
			writer.Write(job.Id);
			writer.WriteUnitId(job.Unit);
			writer.WriteFactionId(job.Faction);
			writer.Write(job.ExpiryTime);
			writer.Write(job.RewardCredits);
			writer.Write(job.ProfitCredits);
			switch (job.JobType)
			{
			case JobType.Courier:
			{
				ModelCourierJob modelCourierJob = (ModelCourierJob)job;
				writer.WriteUnitId(modelCourierJob.PickupUnit);
				writer.WriteUnitId(modelCourierJob.DestinationUnit);
				ComponentUnitCargoDataItemWriter.Write(writer, modelCourierJob.Cargo);
				break;
			}
			case JobType.DeliverShip:
			{
				ModelDeliverShipJob modelDeliverShipJob = (ModelDeliverShipJob)job;
				writer.Write((int)modelDeliverShipJob.UnitClass);
				writer.WriteUnitId(modelDeliverShipJob.DestinationUnit);
				break;
			}
			case JobType.Breakdown:
			{
				ModelBreakdownJob modelBreakdownJob = (ModelBreakdownJob)job;
				writer.Write((int)modelBreakdownJob.BreakdownUnitClass);
				writer.WriteSectorId(modelBreakdownJob.BreakdownDestinationSector);
				writer.WriteVec3(modelBreakdownJob.BreakdownDestinationPosition);
				break;
			}
			case JobType.DestroyGroup:
			{
				ModelDestroyFleetJob modelDestroyFleetJob = (ModelDestroyFleetJob)job;
				FleetSpawnParamsWriter.Write(writer, modelDestroyFleetJob.FleetSpawnParams);
				break;
			}
			}
		}
	}
}
