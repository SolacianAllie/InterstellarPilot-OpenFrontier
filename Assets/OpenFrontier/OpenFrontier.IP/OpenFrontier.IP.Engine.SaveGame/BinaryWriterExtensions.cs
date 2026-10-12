using System;
using System.IO;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public static class BinaryWriterExtensions
	{
		public static void WriteNullableVector3(this BinaryWriter writer, Vector3? vector)
		{
			writer.Write(vector.HasValue);
			if (vector.HasValue)
			{
				writer.Write(vector.Value);
			}
		}

		public static void WriteVersion(this BinaryWriter writer, Version version)
		{
			writer.Write(version.Major);
			writer.Write(version.Minor);
			writer.Write(version.Build);
		}

		public static void Write(this BinaryWriter writer, Vector3 v)
		{
			writer.Write(v.x);
			writer.Write(v.y);
			writer.Write(v.z);
		}

		public static void Write(this BinaryWriter writer, Quaternion v)
		{
			writer.Write(v.x);
			writer.Write(v.y);
			writer.Write(v.z);
			writer.Write(v.w);
		}

		public static void WriteSceneId(this BinaryWriter writer, Sector scene)
		{
			if (scene != null)
			{
				writer.Write(scene.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteUnitId(this BinaryWriter writer, Unit unit)
		{
			if (unit != null)
			{
				writer.Write(unit.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteDamageType(this BinaryWriter writer, DamageType damage)
		{
			writer.Write(damage.Damage);
			writer.Write(damage.MiningDamage);
			writer.Write((int)damage.ShieldDamageType);
		}

		public static void WriteUniqueId(this BinaryWriter writer, IUnique obj)
		{
			if (obj != null)
			{
				writer.Write(obj.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WritePassengerGroupId(this BinaryWriter writer, PassengerGroup passengerGroup)
		{
			if (passengerGroup != null)
			{
				writer.Write(passengerGroup.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WritePilotId(this BinaryWriter writer, Person pilot)
		{
			if (pilot != null)
			{
				writer.Write(pilot.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WritePatrolPathId(this BinaryWriter writer, AIPatrolPath patrolPath)
		{
			if (patrolPath != null)
			{
				writer.Write(patrolPath.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteUnitTraderId(this BinaryWriter writer, CargoTrader unitTrader)
		{
			if (unitTrader != null)
			{
				writer.Write(unitTrader.Unit.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteUnitClassId(this BinaryWriter writer, UnitClass unitClass)
		{
			if (unitClass != null)
			{
				writer.Write(unitClass.UniqueID);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteDialogStageId(this BinaryWriter writer, DialogStage dialogStage)
		{
			if (dialogStage != null)
			{
				writer.Write(dialogStage.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteDialogId(this BinaryWriter writer, DialogBase dialog)
		{
			if (dialog != null)
			{
				writer.Write(dialog.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteCargoClassId(this BinaryWriter writer, CargoClass cargoClass)
		{
			if (cargoClass != null)
			{
				writer.Write(cargoClass.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteComponentClassId(this BinaryWriter writer, ComponentClass c)
		{
			if (c != null)
			{
				writer.Write(c.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteFactionId(this BinaryWriter writer, Faction faction)
		{
			if (faction != null)
			{
				writer.Write(faction.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteAIGroupId(this BinaryWriter writer, Fleet group)
		{
			if (group != null)
			{
				writer.Write(group.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteAIObjectiveId(this BinaryWriter writer, FleetOrder objective)
		{
			if (objective != null)
			{
				writer.Write(objective.UniqueId);
			}
			else
			{
				writer.Write(-1);
			}
		}

		public static void WriteSystemType(this BinaryWriter writer, Type type)
		{
			writer.Write(type.FullName);
		}

		public static void WriteObjectName(this BinaryWriter writer, UnityEngine.Object g)
		{
			if (g != null)
			{
				writer.WriteStringOrEmpty(g.name);
			}
			else
			{
				writer.Write(string.Empty);
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
	}
}
