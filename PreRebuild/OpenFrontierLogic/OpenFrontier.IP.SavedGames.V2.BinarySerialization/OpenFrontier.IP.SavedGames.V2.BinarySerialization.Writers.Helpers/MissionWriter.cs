using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers.Helpers
{
	public static class MissionWriter
	{
		public static void Write(BinaryWriter writer, ModelMission mission)
		{
			writer.Write((int)mission.MissionType);
			writer.Write(mission.Id);
			writer.WriteStringOrEmpty(mission.Title);
			writer.Write(mission.IsPrimary);
			writer.Write(mission.NotificationsEnabled);
			writer.Write(mission.IsActive);
			writer.Write(mission.StageIndex);
			writer.Write(mission.IsFinished);
			writer.Write(mission.CompletionSuccess);
			writer.Write(mission.ShowInJournal);
			writer.WriteFactionId(mission.OwnerFaction);
			writer.WriteFactionId(mission.MissionGiverFaction);
			writer.Write(mission.CompletionOpinionChange);
			writer.Write(mission.FailureOpinionChange);
			writer.Write(mission.StartTime);
			writer.Write(mission.RewardCredits);
			writer.Write(mission.Stages.Count);
			foreach (ModelMissionStage stage in mission.Stages)
			{
				WriteMissionStage(writer, stage);
			}
			writer.Write(mission.Objectives.Count);
			for (int i = 0; i < mission.Objectives.Count; i++)
			{
				ModelMissionObjective objective = mission.Objectives[i];
				WriteMissionObjective(writer, objective);
			}
			switch (mission.MissionType)
			{
			case MissionType.Courier:
			{
				ModelCourierMission modelCourierMission = (ModelCourierMission)mission;
				writer.WriteUnitId(modelCourierMission.PickupUnit);
				writer.WriteUnitId(modelCourierMission.DestinationUnit);
				writer.Write((int)modelCourierMission.CargoItem.CargoClass);
				writer.Write(modelCourierMission.CargoItem.Quantity);
				writer.Write(modelCourierMission.HasPlayerPickedUpCargo);
				break;
			}
			case MissionType.DestroyGroup:
			{
				ModelDestroyUnitsMission modelDestroyUnitsMission = (ModelDestroyUnitsMission)mission;
				writer.Write(modelDestroyUnitsMission.TargetUnits.Count);
				foreach (ModelUnit targetUnit in modelDestroyUnitsMission.TargetUnits)
				{
					writer.WriteUnitId(targetUnit);
				}
				writer.Write(modelDestroyUnitsMission.HasSetGroupHostileToPlayer);
				writer.WriteFactionId(modelDestroyUnitsMission.TargetFaction);
				writer.WriteSectorId(modelDestroyUnitsMission.TargetSector);
				writer.WriteFleetId(modelDestroyUnitsMission.TargetFleet);
				break;
			}
			case MissionType.DeliverShip:
			{
				ModelDeliverShipMission modelDeliverShipMission = (ModelDeliverShipMission)mission;
				writer.Write((int)modelDeliverShipMission.UnitClass);
				writer.WriteUnitId(modelDeliverShipMission.DestinationUnit);
				break;
			}
			case MissionType.Breakdown:
			{
				ModelBreakdownMission modelBreakdownMission = (ModelBreakdownMission)mission;
				writer.WriteUnitId(modelBreakdownMission.BaseUnit);
				writer.WriteUnitId(modelBreakdownMission.BreakdownUnit);
				break;
			}
			}
		}

		public static void WriteMissionObjective(BinaryWriter writer, ModelMissionObjective objective)
		{
			writer.Write(objective.Id);
			writer.WriteStringOrEmpty(objective.Title);
			writer.WriteStringOrEmpty(objective.Description);
			writer.Write(objective.IsOptional);
			writer.Write(objective.Order);
			writer.Write(objective.IsActive);
			writer.Write(objective.IsComplete);
			writer.Write(objective.Success);
			writer.Write(objective.ShowInJournal);
		}

		public static void WriteMissionStage(BinaryWriter writer, ModelMissionStage missionStage)
		{
			writer.Write(missionStage.CompletesMission);
			writer.WriteStringOrEmpty(missionStage.JournalEntry);
			writer.Write(missionStage.MissionSuccess);
		}
	}
}
