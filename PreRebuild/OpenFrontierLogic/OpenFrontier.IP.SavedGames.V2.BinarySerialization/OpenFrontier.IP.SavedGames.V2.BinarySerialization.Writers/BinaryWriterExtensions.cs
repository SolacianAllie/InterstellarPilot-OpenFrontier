using System;
using System.IO;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Writers
{
	public static class BinaryWriterExtensions
	{
		public static void WriteVec3(this BinaryWriter writer, Vec3 vector3)
		{
			writer.Write(vector3.X);
			writer.Write(vector3.Y);
			writer.Write(vector3.Z);
		}

		public static void WriteVec4(this BinaryWriter writer, Vec4 vector4)
		{
			writer.Write(vector4.X);
			writer.Write(vector4.Y);
			writer.Write(vector4.Z);
			writer.Write(vector4.W);
		}

		public static void WriteNullableVec3(this BinaryWriter writer, Vec3? vector3)
		{
			writer.Write(vector3.HasValue);
			if (vector3.HasValue)
			{
				writer.WriteVec3(vector3.Value);
			}
		}

		public static void WriteStringOrEmpty(this BinaryWriter writer, string str)
		{
			if (str != null)
			{
				writer.Write(str);
			}
			else
			{
				writer.Write(string.Empty);
			}
		}

		public static void WriteDamageType(this BinaryWriter writer, ModelDamageType damageType)
		{
			writer.Write(damageType.Damage);
			writer.Write(damageType.MiningDamage);
			writer.Write((int)damageType.ShieldDamageType);
		}

		public static void WriteUnitId(this BinaryWriter writer, ModelUnit unit)
		{
			writer.Write(unit?.Id ?? (-1));
		}

		public static void WriteMissionId(this BinaryWriter writer, ModelMission mission)
		{
			writer.Write(mission?.Id ?? (-1));
		}

		public static void WriteMissionObjectiveId(this BinaryWriter writer, ModelMissionObjective missionObjective)
		{
			writer.Write(missionObjective?.Id ?? (-1));
		}

		public static void WriteTriggerGroupId(this BinaryWriter writer, ModelTriggerGroup triggerGroup)
		{
			writer.Write(triggerGroup?.Id ?? (-1));
		}

		public static void WriteFactionId(this BinaryWriter writer, ModelFaction faction)
		{
			writer.Write(faction?.Id ?? (-1));
		}

		public static void WriteSectorId(this BinaryWriter writer, ModelSector sector)
		{
			writer.Write(sector?.Id ?? (-1));
		}

		public static void WritePersonId(this BinaryWriter writer, ModelPerson person)
		{
			writer.Write(person?.Id ?? (-1));
		}

		public static void WriteFleetId(this BinaryWriter writer, ModelFleet fleet)
		{
			writer.Write(fleet?.Id ?? (-1));
		}

		public static void WritePassengerGroupId(this BinaryWriter writer, ModelPassengerGroup passengerGroup)
		{
			writer.Write(passengerGroup?.Id ?? (-1));
		}

		public static void WriteVersion(this BinaryWriter writer, Version version)
		{
			writer.Write(version.Major);
			writer.Write(version.Minor);
			writer.Write(version.Build);
		}

		public static void WriteSectorPatrolPathId(this BinaryWriter writer, ModelSectorPatrolPath patrolPath)
		{
			writer.Write(patrolPath?.Id ?? (-1));
		}
	}
}
