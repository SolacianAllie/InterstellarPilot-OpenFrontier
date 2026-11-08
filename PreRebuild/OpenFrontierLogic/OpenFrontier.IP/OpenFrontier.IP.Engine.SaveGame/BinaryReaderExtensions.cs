using System;
using System.IO;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public static class BinaryReaderExtensions
	{
		public static Vector3? ReadNullableVector3(this BinaryReader reader)
		{
			if (reader.ReadBoolean())
			{
				return reader.ReadVector3();
			}
			return null;
		}

		public static void SetActiveObjectiveAndResetTargetPosition(this Fleet group, ActiveFleetOrder o)
		{
			group.ActiveOrder = o;
			if (group.ActiveOrder != null)
			{
				group.ActiveOrder.ResetTargetPosition();
			}
		}

		public static Version ReadVersion(this BinaryReader reader)
		{
			int major = reader.ReadInt32();
			int minor = reader.ReadInt32();
			int build = reader.ReadInt32();
			return new Version(major, minor, build);
		}

		public static Quaternion ReadQuaternion(this BinaryReader reader)
		{
			return new Quaternion
			{
				x = reader.ReadSingle(),
				y = reader.ReadSingle(),
				z = reader.ReadSingle(),
				w = reader.ReadSingle()
			};
		}

		public static Vector3 ReadVector3(this BinaryReader reader)
		{
			return new Vector3
			{
				x = reader.ReadSingle(),
				y = reader.ReadSingle(),
				z = reader.ReadSingle()
			};
		}

		public static Color ReadColorFromVector3(this BinaryReader reader)
		{
			Vector3 vector = reader.ReadVector3();
			return new Color(vector.x, vector.y, vector.z);
		}

		public static PassengerGroup ReadPassengerGroupFromId(this BinaryReader reader, EngineASX engine)
		{
			int num = reader.ReadInt32();
			if (num > -1)
			{
				PassengerGroup passengerGroupByid = engine.GetPassengerGroupByid(num);
				if (passengerGroupByid == null)
				{
					Debug.LogError($"Passenger Group ID {num} does not exist");
				}
				return passengerGroupByid;
			}
			return null;
		}

		public static Sector ReadSectorFromId(this BinaryReader reader, EngineASX engine)
		{
			int num = reader.ReadInt32();
			if (num > -1)
			{
				Sector sectorById = engine.GetSectorById(num);
				if (sectorById == null)
				{
					Debug.LogError($"Scene ID {num} does not exist");
				}
				return sectorById;
			}
			return null;
		}

		public static Unit ReadUnitFromId(this BinaryReader reader, EngineASX engine)
		{
			int num = reader.ReadInt32();
			if (num > -1)
			{
				Unit unitByid = engine.GetUnitByid(num);
				if (unitByid == null)
				{
					Debug.LogError($"Unit ID {num} does not exist");
				}
				return unitByid;
			}
			return null;
		}

		public static DamageType ReadDamageType(this BinaryReader reader)
		{
			return new DamageType
			{
				Damage = reader.ReadSingle(),
				MiningDamage = reader.ReadSingle(),
				ShieldDamageType = (ShieldDamageType)reader.ReadInt32()
			};
		}

		public static AIPatrolPath ReadPatrolPathFromId(this BinaryReader reader, EngineASX engine)
		{
			int num = reader.ReadInt32();
			if (num > -1)
			{
				AIPatrolPath patrolPathByid = engine.GetPatrolPathByid(num);
				if (patrolPathByid == null)
				{
					Debug.LogError($"PatrolPath ID {num} does not exist");
				}
				return patrolPathByid;
			}
			return null;
		}

		public static Person ReadPilotFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetPersonById(id);
		}

		public static void WriteMessageDataId(this BinaryWriter writer, MessageTemplate message)
		{
			if (message != null)
			{
				writer.Write(message.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static MessageTemplate ReadMessageDataFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.World.GetMessageDataById(id);
		}

		public static CargoTrader ReadUnitTraderFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			Unit unitByid = engine.GetUnitByid(id);
			if (unitByid != null)
			{
				return unitByid.GetComponent<CargoTrader>();
			}
			return null;
		}

		public static UnitClass ReadUnitClass(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetUnitClassById(id);
		}

		public static DialogStage ReadDialogStageFromId(this BinaryReader reader, EngineASX engine)
		{
			int num = reader.ReadInt32();
			if (num > -1)
			{
				return engine.World.GetDialogStageById(num);
			}
			return null;
		}

		public static DialogBase ReadDialogFromId(this BinaryReader reader, EngineASX engine)
		{
			int num = reader.ReadInt32();
			if (num > -1)
			{
				return engine.World.GetDialogById(num);
			}
			return null;
		}

		public static CargoClass ReadCargoClassFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetCargoClassById(id);
		}

		public static ComponentClass ReadComponentClassFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetComponentClassById(id);
		}

		public static Faction ReadFactionFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetFactionByid(id);
		}

		public static MissionData ReadMissionDataFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetMissionDataById(id);
		}

		public static Fleet ReadFleetFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetFleetByid(id);
		}

		public static FleetOrder ReadAIObjectiveFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetFleetOrderById(id);
		}

		public static Mission ReadMissionFromId(this BinaryReader reader, EngineASX engine)
		{
			int id = reader.ReadInt32();
			return engine.GetMissionById(id);
		}

		public static Type ReadSystemType(this BinaryReader reader)
		{
			return Type.GetType(reader.ReadString());
		}

		public static T ReadComponentFromName<T>(this BinaryReader reader) where T : Component
		{
			return UnityObjectHelper.FindComponent<T>(reader.ReadString());
		}
	}
}
