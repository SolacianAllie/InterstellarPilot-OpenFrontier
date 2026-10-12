using System;
using System.Collections.Generic;
using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.SavedGames.V2.Model;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;
using OpenFrontier.IP.SavedGames.V2.Model.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.BinarySerialization.Readers
{
	public static class BinaryReaderExtensions
	{
		public static Vec3 ReadVec3(this BinaryReader reader)
		{
			return new Vec3
			{
				X = reader.ReadSingle(),
				Y = reader.ReadSingle(),
				Z = reader.ReadSingle()
			};
		}

		public static Vec4 ReadVec4(this BinaryReader reader)
		{
			return new Vec4
			{
				X = reader.ReadSingle(),
				Y = reader.ReadSingle(),
				Z = reader.ReadSingle(),
				W = reader.ReadSingle()
			};
		}

		public static Vec3? ReadNullableVec3(this BinaryReader reader)
		{
			if (reader.ReadBoolean())
			{
				return reader.ReadVec3();
			}
			return null;
		}

		public static ModelDamageType ReadDamageType(this BinaryReader reader)
		{
			return new ModelDamageType
			{
				Damage = reader.ReadSingle(),
				MiningDamage = reader.ReadSingle(),
				ShieldDamageType = (ShieldDamageType)reader.ReadInt32()
			};
		}

		public static ModelMission ReadMission(this BinaryReader reader, Dictionary<int, ModelMission> missionsById)
		{
			int key = reader.ReadInt32();
			return missionsById.GetValueOrDefault(key);
		}

		public static ModelMissionObjective ReadMissionObjective(this BinaryReader reader, Dictionary<int, ModelMissionObjective> missionObjectivesById)
		{
			int key = reader.ReadInt32();
			return missionObjectivesById.GetValueOrDefault(key);
		}

		public static ModelTriggerGroup ReadTriggerGroup(this BinaryReader reader, Dictionary<int, ModelTriggerGroup> triggersGroupsById)
		{
			int key = reader.ReadInt32();
			return triggersGroupsById.GetValueOrDefault(key);
		}

		public static ModelUnit ReadUnit(this BinaryReader reader, Dictionary<int, ModelUnit> unitsById)
		{
			int key = reader.ReadInt32();
			return unitsById.GetValueOrDefault(key);
		}

		public static ModelFaction ReadFaction(this BinaryReader reader, Dictionary<int, ModelFaction> factions)
		{
			int key = reader.ReadInt32();
			return factions.GetValueOrDefault(key);
		}

		public static ModelSector ReadSector(this BinaryReader reader, Dictionary<int, ModelSector> sectors)
		{
			int key = reader.ReadInt32();
			return sectors.GetValueOrDefault(key);
		}

		public static ModelPerson ReadPerson(this BinaryReader reader, Dictionary<int, ModelPerson> people)
		{
			int key = reader.ReadInt32();
			return people.GetValueOrDefault(key);
		}

		public static ModelFleet ReadFleet(this BinaryReader reader, Dictionary<int, ModelFleet> fleets)
		{
			int key = reader.ReadInt32();
			return fleets.GetValueOrDefault(key);
		}

		public static Version ReadVersion(this BinaryReader reader)
		{
			int major = reader.ReadInt32();
			int minor = reader.ReadInt32();
			int build = reader.ReadInt32();
			return new Version(major, minor, build);
		}
	}
}
