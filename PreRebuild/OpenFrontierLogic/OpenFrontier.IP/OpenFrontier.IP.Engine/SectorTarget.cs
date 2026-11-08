using System;
using System.IO;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class SectorTarget
	{
		public bool HadSceneObject;

		public Vector3 SectorPosition;

		public Vector3 RelativeTargetPosition = Vector3.zero;

		[FormerlySerializedAs("Scene")]
		public Sector Sector;

		[FormerlySerializedAs("TargetGroup")]
		public Fleet TargetFleet;

		public Unit TargetUnit;

		public SectorObject TargetObject
		{
			get
			{
				if (TargetUnit != null)
				{
					return TargetUnit;
				}
				return TargetFleet;
			}
		}

		public bool IsMovingTarget
		{
			get
			{
				if (TargetObject != null && !TargetObject.IsStatic)
				{
					if (TargetUnit.UnitType == UnitType.Ship)
					{
						if (TargetUnit.Components != null)
						{
							return TargetUnit.Components.PilotPerson != null;
						}
						return false;
					}
					return true;
				}
				return false;
			}
		}

		public static SectorTarget FromUnit(Unit unit)
		{
			return new SectorTarget
			{
				HadSceneObject = (unit != null),
				TargetUnit = unit,
				Sector = unit.Sector,
				SectorPosition = unit.SectorPosition
			};
		}

		public static SectorTarget FromSectorPosition(Sector sector, Vector3 sectorPosition)
		{
			return new SectorTarget
			{
				Sector = sector,
				SectorPosition = sectorPosition
			};
		}

		public Vector3 GetTargetWorldPosition()
		{
			return GetTargetSector().ToWorldPosition(GetTargetSectorPosition());
		}

		public Vector3 GetTargetSectorPosition()
		{
			if (TargetObject != null)
			{
				return TargetObject.SectorPosition + TargetObject.transform.localRotation * RelativeTargetPosition;
			}
			return SectorPosition;
		}

		public Sector GetTargetSector()
		{
			if (TargetObject != null)
			{
				return TargetObject.Sector;
			}
			return Sector;
		}

		public float GetTargetRadius()
		{
			if (TargetUnit != null)
			{
				return TargetUnit.UnitClass.ShieldRingRadius;
			}
			return 0f;
		}

		public bool IsValid()
		{
			if (GetTargetSector() != null)
			{
				if (HadSceneObject)
				{
					if (TargetObject != null)
					{
						if (!(TargetUnit == null))
						{
							return TargetUnit.IsValidAndNotDestroyed;
						}
						return true;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			writer.Write(SectorPosition);
			writer.WriteSceneId(Sector);
			writer.WriteUnitId(TargetUnit);
			writer.WriteAIGroupId(TargetFleet);
			writer.Write(HadSceneObject);
		}

		public void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			SectorPosition = reader.ReadVector3();
			Sector = reader.ReadSectorFromId(engine);
			TargetUnit = reader.ReadUnitFromId(engine);
			TargetFleet = reader.ReadFleetFromId(engine);
			HadSceneObject = reader.ReadBoolean();
		}

		public SectorTarget Copy()
		{
			return new SectorTarget
			{
				HadSceneObject = HadSceneObject,
				RelativeTargetPosition = RelativeTargetPosition,
				Sector = Sector,
				SectorPosition = SectorPosition,
				TargetFleet = TargetFleet,
				TargetUnit = TargetUnit
			};
		}

		public bool IsSameTargetAs(SectorTarget homeBase)
		{
			if (TargetUnit == homeBase.TargetUnit && GetTargetSector() == homeBase.GetTargetSector())
			{
				return GetTargetSectorPosition() == homeBase.GetTargetSectorPosition();
			}
			return false;
		}
	}
}
