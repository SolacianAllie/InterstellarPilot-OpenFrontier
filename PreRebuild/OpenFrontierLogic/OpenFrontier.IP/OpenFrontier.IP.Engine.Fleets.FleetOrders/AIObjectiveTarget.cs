using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	[Serializable]
	public class AIObjectiveTarget
	{
		public enum InvalidNavpointResult
		{
			Valid,
			NullOrInvalidTargetSectorObject,
			DockTargetWithNoSectorObject,
			GateTargetWithNoSectorObject,
			NoTargetSector
		}

		private float? maxPilotDistFromNavpoint = 0f;

		public float ArrivalThreshold;

		public bool DockIfTargetUnitDocks;

		public bool HasAttemptedToCalculatedPath;

		public Sector LastPathCalculationTargetSector;

		public bool IsActive;

		public bool MatchTargetOrienation;

		public Vector3 SectorPosition;

		public Vector3 RelativeTargetPosition = Vector3.zero;

		[FormerlySerializedAs("Scene")]
		public Sector OriginalSector;

		public bool HadTargetSceneObject;

		public ActiveFleetOrder SourceOrder;

		private SectorObject targetSceneObject;

		public WorldNavpointTargetType TargetType;

		public bool waitingForLeaderToReachNavPoint;

		public List<WorldNavpoint> waypoints = new List<WorldNavpoint>(16);

		public Quaternion? Rotation;

		public float? PreferredMoveSpeedMultiplier;

		public SectorObject TargetSectorObject
		{
			get
			{
				return targetSceneObject;
			}
			set
			{
				targetSceneObject = value;
				HadTargetSceneObject = targetSceneObject != null;
				if (targetSceneObject != null)
				{
					OriginalSector = targetSceneObject.Sector;
					SectorPosition = targetSceneObject.SectorPosition;
				}
			}
		}

		public SectorObject TargetRootSceneObject
		{
			get
			{
				if (TargetSectorObject != null)
				{
					Unit unit = TargetSectorObject as Unit;
					if (unit != null)
					{
						return unit.GetRootUnit();
					}
				}
				return TargetSectorObject;
			}
		}

		public bool IsMovingTarget
		{
			get
			{
				if (TargetSectorObject != null)
				{
					return !TargetSectorObject.IsStatic;
				}
				return false;
			}
		}

		public float? MaxPilotDistFromNavpoint
		{
			get
			{
				return maxPilotDistFromNavpoint;
			}
			set
			{
				maxPilotDistFromNavpoint = value;
			}
		}

		public void Reset()
		{
			ArrivalThreshold = 0f;
			HadTargetSceneObject = false;
			PreferredMoveSpeedMultiplier = null;
			OriginalSector = null;
			TargetSectorObject = null;
			waitingForLeaderToReachNavPoint = false;
			IsActive = false;
			HasAttemptedToCalculatedPath = false;
			TargetType = WorldNavpointTargetType.None;
			SectorPosition = Vector3.zero;
			RelativeTargetPosition = Vector3.zero;
			MatchTargetOrienation = false;
			Rotation = null;
			MaxPilotDistFromNavpoint = null;
			LastPathCalculationTargetSector = null;
			waypoints.Clear();
		}

		public Vector3 GetTargetWorldPosition()
		{
			return GetTargetSector().transform.position + GetTargetSectorPosition();
		}

		public Vector3 GetTargetSectorPosition()
		{
			if (TargetSectorObject != null)
			{
				return TargetSectorObject.SectorPosition + targetSceneObject.transform.localRotation * RelativeTargetPosition;
			}
			return SectorPosition;
		}

		public Sector GetTargetSector()
		{
			if (TargetSectorObject != null)
			{
				return TargetSectorObject.Sector;
			}
			return OriginalSector;
		}

		public bool HasStalePath()
		{
			if (HasAttemptedToCalculatedPath)
			{
				return GetTargetSector() != LastPathCalculationTargetSector;
			}
			return false;
		}

		public InvalidNavpointResult GetIsValid()
		{
			if (HadTargetSceneObject && (targetSceneObject == null || !targetSceneObject.IsValid))
			{
				return InvalidNavpointResult.NullOrInvalidTargetSectorObject;
			}
			if (GetTargetSector() != null)
			{
				switch (TargetType)
				{
				case WorldNavpointTargetType.Dock:
					if (TargetSectorObject == null)
					{
						return InvalidNavpointResult.DockTargetWithNoSectorObject;
					}
					break;
				case WorldNavpointTargetType.Gate:
					if (TargetSectorObject == null)
					{
						return InvalidNavpointResult.DockTargetWithNoSectorObject;
					}
					break;
				}
				return InvalidNavpointResult.Valid;
			}
			return InvalidNavpointResult.NoTargetSector;
		}
	}
}
