using System.Collections.Generic;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitCompass : MonoBehaviour
	{
		public Sprite DefaultArrowSprite;

		public Canvas Canvas;

		public UnitCompassItem CompassItemPrefab;

		public bool AlwaysShowHostiles = true;

		public List<UnitCompassPoint> CompassPoints = new List<UnitCompassPoint>(20);

		private PriorityQueue<Unit, float> compassPointQueue = new PriorityQueue<Unit, float>(64);

		public Vector3 CompassPointScale = new Vector3(1f, 1f, 1f);

		public float CompassPointScalingMultiplier = 0.5f;

		public Vector3 CurrentTargetScaleMultiplier = new Vector3(2f, 2f, 2f);

		public float ManualOffsetFromUnit = 5f;

		public float DefaultUnitRadius = 7.3f;

		private EngineASX engine;

		public int MaxPoints = 8;

		public float RefreshFrequency = 1f;

		public Vector3 OffsetFromUnit = Vector3.zero;

		public Unit TargetUnit;

		public float UnitShieldRingRadiusMultiplier = 1.5f;

		private List<UnitCompassItem> activeCompassItems = new List<UnitCompassItem>();

		public bool CompassPointsBelowMax => CompassPoints.Count < MaxPoints;

		private void Start()
		{
			engine = EngineASX.Instance;
		}

		private bool AddPointForWaypoint(ref Vector3 scale, PlayerWaypointPath path, bool isMissionPath, out SectorObject sceneObject)
		{
			sceneObject = null;
			WorldNavpoint n = default;
			if (path.GetFirstWaypoint(ref n))
			{
				sceneObject = n.TargetSectorObject;
				if (n.GetTargetSector() == TargetUnit.Sector)
				{
					Color color = ((!(n.TargetSectorObject is Unit)) ? engine.GetFactionHostilityColor(0.5f) : engine.GetFactionHostilityColor(((Unit)n.TargetSectorObject).Faction, TargetUnit.Faction));
					Vector3 targetWorldPosition = n.GetTargetWorldPosition();
					targetWorldPosition.y = 0f;
					UnitCompassPoint item = new UnitCompassPoint
					{
						Color = color,
						WorldPosition = targetWorldPosition,
						Scale = scale
					};
					item.IsMissionWaypoint = isMissionPath;
					item.IsCustomWaypoint = !isMissionPath;
					item.RelatedPath = path;
					item.IsSelectedTarget = EngineASX.Instance.Hud.CurrentTarget != null && n.TargetSectorObject == EngineASX.Instance.Hud.CurrentTarget;
					CompassPoints.Add(item);
				}
				return true;
			}
			return false;
		}

		private void Update()
		{
			SectorObject sceneObject = null;
			SectorObject sceneObject2 = null;
			if (!(GameController.Instance.MainCamera != null))
			{
				return;
			}
			CompassPoints.Clear();
			compassPointQueue.Clear();
			if (!(TargetUnit != null) || !(TargetUnit.Sector != null))
			{
				return;
			}
			if (engine.Hud != null)
			{
				Canvas.transform.position = TargetUnit.transform.TransformPoint(OffsetFromUnit);
				Vector3 vector = Vector3.one + Vector3.one * (TargetUnit.UnitClass.ShieldRingRadius / DefaultUnitRadius * CompassPointScalingMultiplier);
				Vector3 scale = vector;
				scale.Scale(CurrentTargetScaleMultiplier);
				AddPointForWaypoint(ref scale, engine.LocalPlayer.WaypointController.CustomPath, isMissionPath: false, out sceneObject);
				foreach (PlayerWaypointPath missionPath in engine.LocalPlayer.WaypointController.MissionPaths)
				{
					AddPointForWaypoint(ref scale, missionPath, isMissionPath: true, out sceneObject2);
				}
				AddScannedTargets(sceneObject2, sceneObject, vector, scale);
			}
			RefreshImages();
		}

		private void AddScannedTargets(SectorObject missionPathSceneObject, SectorObject customPathSceneObject, Vector3 defaultScale, Vector3 currentTargetScale)
		{
			// Locked missiles first, and by identity, so the scan below can skip
			// them. A missile locked onto the player is also hostile and armed and
			// within 10000, so it already passed ShouldShowCompassPointForUnit at
			// priority ~50 - and the queue has no dedupe, so without this skip it
			// was enqueued twice and drew two arrows for one missile.
			List<Missile> missileLocks = EngineASX.Instance.MissileLockController.GetMissileLocks(EngineASX.Instance.LocalUnit);
			HashSet<Unit> lockedMissiles = null;

			if (missileLocks != null && missileLocks.Count > 0)
			{
				lockedMissiles = new HashSet<Unit>();

				foreach (Missile missile in missileLocks)
				{
					if (missile.Projectile.Unit.IsValid)
					{
						lockedMissiles.Add(missile.Projectile.Unit);
					}
				}
			}

			foreach (HudScannerUnit item in engine.Hud.AutoScanner.ScannedUnitCache)
			{
				Unit unit = item.Unit;
				if (unit != null && unit != missionPathSceneObject && unit != customPathSceneObject && (lockedMissiles == null || !lockedMissiles.Contains(unit)) && ShouldShowCompassPointForUnit(unit, item.DistanceFromLocalUnitIgnoringY, out var priority))
				{
					compassPointQueue.Enqueue(unit, priority);
				}
			}

			if (lockedMissiles == null)
			{
				return;
			}

			// Above everything, including the current target at 100.
			foreach (Unit lockedMissile in lockedMissiles)
			{
				compassPointQueue.Enqueue(lockedMissile, 1000f);
			}
		}

		private void ShowCompassPointForUnit(Vector3 defaultScale, Vector3 currentTargetScale, Unit unit)
		{
			Color factionHostilityColor = TargetUnit.Engine.GetFactionHostilityColor(unit.Faction, TargetUnit.Faction);
			Vector3 scale = defaultScale;
			if (unit == engine.Hud.CurrentTarget)
			{
				scale = currentTargetScale;
			}
			Vector3 position = unit.transform.position;
			position.y = 0f;
			UnitCompassPoint item = new UnitCompassPoint
			{
				Color = factionHostilityColor,
				WorldPosition = position,
				Scale = scale
			};
			item.UnitClass = unit.UnitClass;
			item.IsSpeaking = UnitIsSpeaking(unit);
			item.IsSelectedTarget = EngineASX.Instance.Hud.CurrentTarget == unit;
			item.IsMissileLockedOnTarget = IsMissileLockedOnTarget(unit);
			CompassPoints.Add(item);
		}

		/// <summary>
		/// Whether this unit is a missile that has locked onto the ship the
		/// compass is drawn for.
		/// </summary>
		/// <remarks>
		/// The same test HudAutoPickTarget uses to decide an auto-pick target,
		/// so the compass and auto-target cannot disagree about which missiles
		/// are actually coming for you. Disrupted missiles are excluded: they
		/// are still travelling toward the player but no longer going to arrive,
		/// and pulsing the lock icon for one is a lie.
		/// </remarks>
		private bool IsMissileLockedOnTarget(Unit unit)
		{
			if (unit == null || TargetUnit == null || unit.UnitType != UnitType.Projectile)
			{
				return false;
			}

			Projectile projectile = unit.GetComponent<Projectile>();

			if (projectile == null || !projectile.IsMissile)
			{
				return false;
			}

			Missile missile = projectile.GetComponent<Missile>();

			return missile != null && missile.Target == TargetUnit && !missile.IsDisrupted && missile.HasAquiredTarget;
		}

		public float GetDistanceFromUnit(UnitClass unitClass)
		{
			return ManualOffsetFromUnit + unitClass.ShieldRingRadius * UnitShieldRingRadiusMultiplier * unitClass.DisplayData.HudCompassRadiusFudgeMultiplier;
		}

		private Sprite GetPointSprite(UnitClass unitClass)
		{
			if (unitClass == null)
			{
				return DefaultArrowSprite;
			}
			Sprite thumbnailIconSprite = unitClass.GetThumbnailIconSprite();
			if (thumbnailIconSprite != null)
			{
				return thumbnailIconSprite;
			}
			return DefaultArrowSprite;
		}

		private void RefreshImages()
		{
			Vector3 vector = Vector3.one + Vector3.one * (TargetUnit.UnitClass.ShieldRingRadius / DefaultUnitRadius * CompassPointScalingMultiplier);
			Vector3 currentTargetScale = vector;
			currentTargetScale.Scale(CurrentTargetScaleMultiplier);
			while (CompassPoints.Count < MaxPoints && compassPointQueue.Count > 0)
			{
				Unit value = compassPointQueue.Dequeue().Value;
				ShowCompassPointForUnit(vector, currentTargetScale, value);
			}
			int num = 0;
			foreach (UnitCompassPoint compassPoint in CompassPoints)
			{
				UnitCompassItem unitCompassItem = null;
				if (num >= activeCompassItems.Count)
				{
					unitCompassItem = UnityObjectHelper.InstantiateAndGetComponent(CompassItemPrefab);
					unitCompassItem.transform.SetParent(Canvas.transform, worldPositionStays: true);
					unitCompassItem.Init();
					activeCompassItems.Add(unitCompassItem);
				}
				else
				{
					unitCompassItem = activeCompassItems[num];
				}
				unitCompassItem.transform.localScale = Vector3.Scale(compassPoint.Scale, CompassPointScale);
				Vector3 vector2 = Vector3.Normalize(compassPoint.WorldPosition - Canvas.transform.position);
				Vector3 localPosition = vector2 * GetDistanceFromUnit(TargetUnit.UnitClass) + TargetUnit.UnitClass.DisplayData.HudCompassManualFudgeOffset;
				unitCompassItem.transform.localPosition = localPosition;
				unitCompassItem.transform.rotation = Quaternion.Euler(90f, Unit.GetYBearing(vector2), 0f);
				bool flag = compassPoint.IsCustomWaypoint || compassPoint.IsMissionWaypoint;
				unitCompassItem.WaypointTypeImage.enabled = flag;
				unitCompassItem.SelectedImage.enabled = compassPoint.IsSelectedTarget;
				unitCompassItem.UnitTypeImage.enabled = compassPoint.UnitClass != null;
				unitCompassItem.MissileLockImage.enabled = compassPoint.IsMissileLockedOnTarget;
				if (compassPoint.UnitClass != null)
				{
					unitCompassItem.UnitTypeImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassThumbnailIconSpriteOrDefault(compassPoint.UnitClass);
					unitCompassItem.UnitTypeImage.color = compassPoint.Color;
				}
				if (flag)
				{
					unitCompassItem.WaypointTypeImage.color = compassPoint.RelatedPath.WaypointColor;
				}
				unitCompassItem.SpeechImage.enabled = compassPoint.IsSpeaking && !flag;
				num++;
			}
			for (int i = num; i < activeCompassItems.Count; i++)
			{
				activeCompassItems[i].Deactivate();
			}
		}

		private bool UnitIsSpeaking(Unit u)
		{
			if (u.Components != null && u.Components.PilotPerson != null && engine.Hud.SpeechModel.ActiveRequest != null)
			{
				return engine.Hud.SpeechModel.ActiveRequest.SourcePerson == u.Components.PilotPerson;
			}
			return false;
		}

		private bool ShouldShowCompassPointForUnit(Unit unit, float distance, out float priority)
		{
			priority = 0f;
			if (unit != TargetUnit && unit.IsTargettable(TargetUnit.Faction) && unit.Sector == TargetUnit.Sector)
			{
				bool flag = engine.Hud.CurrentTarget == unit;
				bool flag2 = unit.Faction != null && unit.IsHostileTo(TargetUnit);
				bool isArmed = unit.IsArmed;
				if (flag)
				{
					priority = 100f;
					return !IsInScreen(unit);
				}
				if (UnitIsSpeaking(unit))
				{
					priority = 50f;
					return true;
				}
				if (flag2 && (isArmed || unit.IsMajorStation()) && distance < 10000f)
				{
					priority = Mathf.Lerp(50f, 0f, distance / 10000f);
					return true;
				}
				if (unit.IsOwnedByPlayer && unit.IsStationOrShip() && !IsInScreen(unit) && distance < 10000f)
				{
					priority = Mathf.Lerp(25f, 0f, distance / 10000f);
					return true;
				}
				if (unit.WormholeComponent != null)
				{
					double? scenarioTimeOfDiscovery = engine.LocalPlayer.Faction.Intel.GetScenarioTimeOfDiscovery(unit);
					if (scenarioTimeOfDiscovery.HasValue && EngineASX.Instance.ScenarioElapsedTime - scenarioTimeOfDiscovery < 10.0)
					{
						priority = 10f;
						return true;
					}
				}
			}
			return false;
		}

		private bool IsInScreen(Unit unit)
		{
			Vector3 vector = GameController.Instance.MainCamera.WorldToScreenPoint(unit.transform.position);
			if (vector.x > 0f && vector.x < (float)Screen.width && vector.y > 0f && vector.y < (float)Screen.height)
			{
				return vector.z > 0f;
			}
			return false;
		}
	}
}
