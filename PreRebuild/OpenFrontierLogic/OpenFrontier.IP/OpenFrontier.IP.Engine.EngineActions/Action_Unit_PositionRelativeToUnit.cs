using System.IO;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Unit_PositionRelativeToUnit : EngineAction
	{
		public Vector3 RelativePosition = Vector3.zero;

		public Unit RelativeToUnit;

		public Unit TargetUnit;

		public bool UseSafePos = true;

		public bool UseYRotOnly = true;

		public override ActionType Type => ActionType.Unit_PositionRelativeToUnit;

		public override void Execute()
		{
			base.Execute();
			if (TargetUnit != null && RelativeToUnit != null)
			{
				TargetUnit.Sector = RelativeToUnit.Sector;
				Vector3 zero = Vector3.zero;
				if (UseYRotOnly)
				{
					Quaternion quaternion = Quaternion.Euler(0f, RelativeToUnit.transform.eulerAngles.y, 0f);
					zero = RelativeToUnit.SectorPosition + quaternion * RelativePosition;
				}
				else
				{
					zero = RelativeToUnit.SectorPosition + RelativeToUnit.transform.localRotation * RelativePosition;
				}
				if (UseSafePos && RelativeToUnit.Sector != null)
				{
					zero = PhysicsNonOverlappingPositionFinder.FindSectorPosition(TargetUnit.Sector, zero, 50f, GameController.Instance.NonOVerlappingUnitsMask);
				}
				TargetUnit.transform.localPosition = zero;
				Fleet fleet = TargetUnit.GetFleet();
				if (fleet != null && fleet.LeaderUnit == TargetUnit)
				{
					fleet.SetPositionToLeaderShipPosition();
				}
			}
		}

		public override void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			base.WriteBinary(writer, engine);
			writer.WriteUnitId(TargetUnit);
			writer.WriteUnitId(RelativeToUnit);
		}

		public override void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			base.ReadBinary(reader, engine);
			TargetUnit = reader.ReadUnitFromId(engine);
			RelativeToUnit = reader.ReadUnitFromId(engine);
		}
	}
}
