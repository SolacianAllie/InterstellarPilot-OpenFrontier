using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Unit))]
	public class Wormhole : MonoBehaviour
	{
		private Sector actualTargetSector;

		public float DistanceMultiplier = 1f;

		private EngineASX engine;

		public bool IsUnstable;

		[FormerlySerializedAs("ManualTargetPosition")]
		public Vector3 ManualTargetSectorPosition = Vector3.zero;

		public Vector3 ManualTargetRotation = Vector3.zero;

		[FormerlySerializedAs("manualTargetScene")]
		[SerializeField]
		private Sector manualTargetSector;

		[SerializeField]
		private double nextUnstableChgTargetTime;

		public Wormhole TargetGate;

		private Unit unit;

		public Sector ManualTargetSector
		{
			get
			{
				return manualTargetSector;
			}
			set
			{
				if (manualTargetSector != value)
				{
					manualTargetSector = value;
					UpdateActualTargetScene();
				}
			}
		}

		public Sector ActualTargetSector
		{
			get
			{
				return actualTargetSector;
			}
			private set
			{
				if (!(actualTargetSector != value))
				{
					return;
				}
				Sector sector = actualTargetSector;
				actualTargetSector = value;
				if (Sector != null)
				{
					if (sector != null)
					{
						Sector.RemoveNeighbor(sector, this);
					}
					if (actualTargetSector != null)
					{
						Sector.AddNeighbor(actualTargetSector, this);
					}
				}
			}
		}

		public Unit Unit => unit;

		public Sector Sector
		{
			get
			{
				if (unit != null)
				{
					return unit.Sector;
				}
				return null;
			}
		}

		public double NextUnstableChgTargetTime
		{
			get
			{
				return nextUnstableChgTargetTime;
			}
			set
			{
				nextUnstableChgTargetTime = value;
			}
		}

		internal bool IsEnterable()
		{
			Unit unit = this.unit;
			if ((object)unit != null && unit.IsValidAndNotDestroyed)
			{
				return actualTargetSector != null;
			}
			return false;
		}

		public void Init(Unit unit)
		{
			engine = EngineASX.Instance;
			this.unit = unit;
			if (this.unit.Destructable != null)
			{
				this.unit.Destructable.IsInvulnerable = true;
				this.unit.Destructable.AllowDestruction = false;
			}
		}

		public void InitGateTargets()
		{
			UpdateActualTargetScene();
			if (TargetGate != null)
			{
				OnTargetGateChanged();
			}
		}

		public void OnTargetGateChanged()
		{
			if (TargetGate != null)
			{
				TargetGate.unit.Init();
			}
			UpdateActualTargetScene();
		}

		public void ValidateTargetGate()
		{
			if (TargetGate == this)
			{
				Debug.LogError($"My Target gate is myself!", this);
			}
			if (TargetGate.TargetGate != null && TargetGate.TargetGate != this)
			{
				Debug.LogError($"My Target gate does not point back to me", this);
			}
		}

		public void UpdateActualTargetScene()
		{
			ActualTargetSector = CalcTargetSector();
		}

		public void AutoTransformGate(bool position = true, bool rotate = true)
		{
			AutoTransformGate(Sector, engine.World, position, rotate);
		}

		public void AutoTransformGate(Sector sector, WorldBase world, bool position, bool rotate)
		{
			AutoTransformGate(sector.GetActualGateDistance(world) * DistanceMultiplier, position, rotate);
		}

		public void AutoTransformGate(float gateDistance, bool position, bool rotate)
		{
			if (IsUnstable)
			{
				return;
			}
			Sector sector = UnityObjectHelper.FindInParentsOrSelf<Sector>(gameObject);
			Sector sector2 = null;
			if (TargetGate != null)
			{
				sector2 = UnityObjectHelper.FindInParentsOrSelf<Sector>(TargetGate.gameObject);
			}
			if (sector != null && sector2 != null)
			{
				Vector3 mapPosition = sector2.MapPosition;
				Vector3 vector = Vector3.forward;
				if (mapPosition != sector.MapPosition)
				{
					vector = Vector3.Normalize(mapPosition - sector.MapPosition);
				}
				if (position)
				{
					transform.localPosition = vector * gateDistance;
				}
				if (rotate)
				{
					transform.rotation = Quaternion.LookRotation(-vector);
				}
			}
		}

		public string GetEditorName(Sector sector, Sector targetSector)
		{
			string arg = ((targetSector != null) ? targetSector.Name : "NULL");
			if (IsUnstable)
			{
				arg = "Unstable";
			}
			Unit component = GetComponent<Unit>();
			return string.Format("Wormhole_{0}_{1}_to_{2}", component.UniqueId, (sector != null) ? sector.Name : "NULL", arg);
		}

		public void AutoNameGameObject(Sector sector, Sector targetSector)
		{
			gameObject.name = GetEditorName(sector, targetSector);
		}

		public void AutoNameGameObject()
		{
			gameObject.name = GetEditorName();
		}

		public string GetEditorName()
		{
			Sector sector = UnityObjectHelper.FindInParentsOrSelf<Sector>(gameObject);
			Sector targetSector = null;
			if (TargetGate != null)
			{
				targetSector = UnityObjectHelper.FindInParentsOrSelf<Sector>(TargetGate.gameObject);
			}
			return GetEditorName(sector, targetSector);
		}

		public Vector3 GetSafeTargetSectorPosition(float entererRadius)
		{
			Vector3 targetSectorPosition = GetTargetSectorPosition();
			if (TargetGate != null)
			{
				float num = engine.GameSettings.JumpGateExitDistance + entererRadius;
				targetSectorPosition += TargetGate.transform.rotation * (Vector3.forward * num);
			}
			return targetSectorPosition;
		}

		public Vector3 GetSafeTargetSectorPositionWithExitDistanceMultiplier(float multiplier)
		{
			Vector3 targetSectorPosition = GetTargetSectorPosition();
			if (TargetGate != null)
			{
				float num = engine.GameSettings.JumpGateExitDistance * multiplier;
				targetSectorPosition += TargetGate.transform.rotation * (Vector3.forward * num);
			}
			return targetSectorPosition;
		}

		public Vector3 GetTargetSectorPosition()
		{
			if (TargetGate != null)
			{
				return TargetGate.unit.SectorPosition;
			}
			return ManualTargetSectorPosition;
		}

		public Vector3 GetTargetWorldPosition()
		{
			return ActualTargetSector.ToWorldPosition(GetTargetSectorPosition());
		}

		public Quaternion GetTargetRotation()
		{
			if (TargetGate != null)
			{
				return TargetGate.transform.rotation;
			}
			return Quaternion.Euler(ManualTargetRotation);
		}

		[ContextMenu("Randomize Target")]
		public void RandomizeTarget()
		{
			Sector sector = (from e in Unit.Engine.Sectors
				where SectorCanTargetOtherSectorWithUnstableWormhole(Sector, e)
				orderby ScoreUnstableWormholeTargetSector(Sector, e)
				select e).LastOrDefault();
			if (sector != null)
			{
				Vector3 checkSectorPosition = Geometry.RandomXZUnitVector() * Mathf.Lerp(0f, sector.GetActualGateDistance() * 1.15f, Random.value);
				checkSectorPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(sector, checkSectorPosition, 200f, GameController.Instance.NonOVerlappingUnitsMask);
				TargetGate = null;
				ManualTargetSector = sector;
				ManualTargetSectorPosition = checkSectorPosition;
				ManualTargetRotation = new Vector3(0f, Random.value * 360f, 0f);
			}
			RemoveAllFactionIntelOfWormholeEntry();
			OnTargetGateChanged();
		}

		private bool SectorCanTargetOtherSectorWithUnstableWormhole(Sector sector, Sector otherSector)
		{
			if (sector == otherSector)
			{
				return false;
			}
			if (sector.IsNeighbourOfIncludingUnstable(otherSector) || otherSector.IsNeighbourOfIncludingUnstable(sector))
			{
				return false;
			}
			if (sector.GetJumpDistanceTo(otherSector) < 2)
			{
				return false;
			}
			return true;
		}

		private float ScoreUnstableWormholeTargetSector(Sector sourceSector, Sector sector)
		{
			float num = (float)(-sector.Neighbours.Count) * 1.5f;
			if (sector == sourceSector || sector.IsNeighbourOfIgnoringUnstable(sourceSector))
			{
				num -= 20f;
			}
			num += sector.DistanceFromUniverseCenter01 * 5f;
			num += Mathf.Pow(Random.value, 3f) * 5f;
			foreach (SectorNeighbour neighbour in sourceSector.Neighbours)
			{
				if (neighbour.ConnectingGate != this)
				{
					Vector3 normalized = neighbour.ConnectingGate.transform.localPosition.normalized;
					Vector3 normalized2 = (sector.MapPosition - sourceSector.MapPosition).normalized;
					float num2 = Vector3.Dot(normalized, normalized2);
					if (num2 > 0.5f)
					{
						num -= num2 * 50f;
					}
				}
			}
			return num;
		}

		public void UpdateUnstableWormhole()
		{
			if (unit.Engine.ScenarioElapsedTime > nextUnstableChgTargetTime)
			{
				EngineASX.Instance.DebugInfo.NumUnstableWormholesChangedTarget++;
				RandomizeTargetAndSetNextChangeTime();
				RemoveAllFactionIntelOfWormholeEntry();
			}
		}

		public void OnEnteredByPlayer()
		{
		}

		public void RemoveAllFactionIntelOfWormholeEntry()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.Intel != null)
				{
					faction.Intel.RemoveWormholeEntry(this);
				}
			}
		}

		public void RandomizeTargetAndSetNextChangeTime()
		{
			RandomizeTarget();
			nextUnstableChgTargetTime = Unit.Engine.ScenarioElapsedTime + (double)Mathf.Lerp(EngineASX.Instance.GameSettings.UnstableWormholeSettings.MinTimeBeforeDestinationChange, EngineASX.Instance.GameSettings.UnstableWormholeSettings.MaxTimeBeforeDestinationChange, Random.value);
		}

		private Sector CalcTargetSector()
		{
			if (manualTargetSector != null)
			{
				return manualTargetSector;
			}
			if (TargetGate != null)
			{
				return TargetGate.Sector;
			}
			return null;
		}

		public string GetUnexploredName()
		{
			return "Wormhole " + unit.GetDesignation();
		}

		public string GetFriendlyName(bool shortName = false)
		{
			if (IsUnstable)
			{
				return "Unstable Wormhole " + unit.GetDesignation();
			}
			Sector sector = ActualTargetSector;
			if (sector != null)
			{
				if (shortName)
				{
					return sector.Name;
				}
				return "Wormhole to " + sector.Name;
			}
			return "Wormhole";
		}
	}
}
