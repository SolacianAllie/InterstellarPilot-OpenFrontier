using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.JobTypes;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers.Helpers
{
	public static class JobReader
	{
		public static ModelJob Read(BinaryReader reader, JobType jobType, Dictionary<int, ModelSector> sectors, Dictionary<int, ModelFaction> factions, Dictionary<int, ModelUnit> units)
		{
			ModelJob modelJob = CreateJobFromJobType.CreateJob(jobType);
			modelJob.Id = reader.ReadInt32();
			int key = reader.ReadInt32();
			modelJob.Unit = units.GetValueOrDefault(key);
			int key2 = reader.ReadInt32();
			modelJob.Faction = factions.GetValueOrDefault(key2);
			modelJob.ExpiryTime = reader.ReadDouble();
			modelJob.RewardCredits = reader.ReadInt32();
			modelJob.ProfitCredits = reader.ReadInt32();
			switch (modelJob.JobType)
			{
			case JobType.Courier:
			{
				ModelCourierJob modelCourierJob = (ModelCourierJob)modelJob;
				int key5 = reader.ReadInt32();
				modelCourierJob.PickupUnit = units.GetValueOrDefault(key5);
				int key6 = reader.ReadInt32();
				modelCourierJob.DestinationUnit = units.GetValueOrDefault(key6);
				modelCourierJob.Cargo = ComponentUnitCargoDataItemReader.Read(reader);
				break;
			}
			case JobType.DeliverShip:
			{
				ModelDeliverShipJob modelDeliverShipJob = (ModelDeliverShipJob)modelJob;
				modelDeliverShipJob.UnitClass = (ModelUnitClass)reader.ReadInt32();
				int key4 = reader.ReadInt32();
				modelDeliverShipJob.DestinationUnit = units.GetValueOrDefault(key4);
				break;
			}
			case JobType.Breakdown:
			{
				ModelBreakdownJob modelBreakdownJob = (ModelBreakdownJob)modelJob;
				modelBreakdownJob.BreakdownUnitClass = (ModelUnitClass)reader.ReadInt32();
				int key3 = reader.ReadInt32();
				modelBreakdownJob.BreakdownDestinationSector = sectors.GetValueOrDefault(key3);
				modelBreakdownJob.BreakdownDestinationPosition = reader.ReadVec3();
				break;
			}
			case JobType.DestroyGroup:
				((ModelDestroyFleetJob)modelJob).FleetSpawnParams = FleetSpawnParamsReader.Read(reader, factions, sectors, units);
				break;
			}
			return modelJob;
		}
	}
}
