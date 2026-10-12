using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class HudTargetController : MonoBehaviour
	{
		public Transform HudTargetTransform;

		public Sprite SmallBracketsSprite;

		public Sprite SmallBracketsCargoSprite;

		public Sprite SmallBracketsShipSprite;

		private int activeTargetsCount;

		public float CurrentTargetAlpha = 1f;

		public HudCurrentTargetUI CurrentTargetUI;

		private float drawSize;

		private EngineASX engine;

		private HudScreen hud;

		public int InitialPoolSize = 10;

		private Unit lastTarget;

		public float MaxTargetBracketsDist = 256f;

		public float MinTargetBracketsDist = 64f;

		private List<HudTargetUI> pool = new List<HudTargetUI>(20);

		public float SmallBracketsUnimportantDrawDist = 1000f;

		public float CargoSmallBracketstDrawDist = 200f;

		public float TargetBracketSizeLerpRate = 10f;

		public float TargetSpriteMaxAlpha = 1f;

		public float TargetSpriteMinAlpha = 0.65f;

		public float TargetSpriteMinWormholeAlpha = 0.9f;

		public float TargetSpriteMinAlphaDist = 1000f;

		public HudTargetUI TargetUIPrefab;

		public float TargetWaypointBracketDefaultWorldRadius = 5f;

		public float TargetWaypointBracketsSizeMultiplier = 1.2f;

		public float RepopulateSmallWidgetsFrequency = 0.2f;

		private float nextRepopulateSmallWidgetsTime;

		public int MaxSmallWidgets = 30;

		private PriorityQueue<Unit, float> possibleTargetPriorityQueue = new PriorityQueue<Unit, float>(30);

		public float SmallBracketsPriorityDistanceWeight = 2f;

		public float SmallBracketsPriorityDistanceReferenceValue = 3000f;

		public float SmallBracketsPriorityHostileWeight = 3f;

		public float SmallBracketsPriorityWormholeWeight = 10f;

		public float SmallBracketsPriorityWaypointWeight = 10f;

		public float SmallBracketsPriorityPlayerOwnedWeight = 0.2f;

		public HudScreen Hud => hud;

		public EngineASX Engine => engine;

		public int ActiveTargetsCount => activeTargetsCount;

		public static bool GetWidgetPos(Vector3 worldPos, out Vector3 targetScreenPos)
		{
			targetScreenPos = GameController.Instance.MainCamera.WorldToViewportPoint(worldPos);
			bool result = HudScreen.IsViewportPositionInScreen(ref targetScreenPos);
			ViewportPosToScreenPos(ref targetScreenPos);
			targetScreenPos.z = 0f;
			return result;
		}

		public static bool GetWorldPos(Vector3 worldPos, out Vector3 targetScreenPos)
		{
			targetScreenPos = GameController.Instance.MainCamera.WorldToViewportPoint(worldPos);
			bool result = HudScreen.IsViewportPositionInScreen(ref targetScreenPos);
			ViewportPosToCamRect(ref targetScreenPos);
			targetScreenPos.z = 0f;
			return result;
		}

		public static void ViewportPosToScreenPos(ref Vector3 viewpointPos)
		{
			viewpointPos.x *= Screen.width;
			viewpointPos.y *= Screen.height;
		}

		public static void ViewportPosToCamRect(ref Vector3 viewpointPos)
		{
			viewpointPos.x = (viewpointPos.x - 0.5f) * 2f * (float)Screen.width / (float)Screen.height;
			viewpointPos.y = (viewpointPos.y - 0.5f) * 2f;
			viewpointPos.z = 0f;
		}

		public HudTargetUI AddToPool()
		{
			HudTargetUI component = Object.Instantiate(TargetUIPrefab.gameObject).GetComponent<HudTargetUI>();
			component.transform.SetParent(HudTargetTransform, worldPositionStays: true);
			component.transform.localScale = Vector3.one;
			component.transform.position = Vector3.zero;
			component.TargetController = this;
			pool.Add(component);
			return component;
		}

		public HudTargetUI GetActiveTarget(int index)
		{
			return pool[index];
		}

		public void ClearActiveTargets()
		{
			for (int i = 0; i < activeTargetsCount; i++)
			{
				pool[i].gameObject.SetActive(value: false);
			}
			activeTargetsCount = 0;
		}

		public HudTargetUI GetOrAddToPool()
		{
			if (activeTargetsCount < pool.Count)
			{
				return pool[activeTargetsCount];
			}
			return AddToPool();
		}

		private bool ShouldShowForUnit(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed && unit.ActiveUnit != null)
			{
				return !unit.IsDocked;
			}
			return false;
		}

		public float GetSmallBracketsPriority(Unit unit, float distance)
		{
			float num = 0f - distance / SmallBracketsPriorityDistanceReferenceValue * SmallBracketsPriorityDistanceWeight;
			num += GetSmallBracketsPriorityFromUnitType(unit);
			if (unit.IsOwnedByPlayer)
			{
				num += SmallBracketsPriorityPlayerOwnedWeight;
			}
			if (unit.IsPlayerMissionPathTargetOrFirstWaypoint() || unit.IsPlayerCustomPathTargetOrFirstWaypointOrPathMarker())
			{
				num += 1000f;
			}
			if (unit.IsHostileTo(EngineASX.Instance.LocalFaction) && unit.IsStationOrShip())
			{
				num += SmallBracketsPriorityHostileWeight;
			}
			return num;
		}

		private float GetSmallBracketsPriorityFromUnitType(Unit unit)
		{
			return unit.UnitType switch
			{
				UnitType.Wormhole => SmallBracketsPriorityWormholeWeight, 
				UnitType.Waypoint => SmallBracketsPriorityWaypointWeight, 
				UnitType.Station => GetSmallBracketsStationPriority(unit), 
				UnitType.Projectile => 0.1f, 
				UnitType.Ship => 0.5f, 
				UnitType.Asteroid => 0.5f, 
				UnitType.Cargo => 0.1f, 
				_ => 0.1f, 
			};
		}

		private float GetSmallBracketsStationPriority(Unit unit)
		{
			switch (unit.UnitClass.StationPurpose)
			{
			case StationPurpose.Satellite:
				return 0.1f;
			case StationPurpose.Defence:
				return 0.5f;
			case StationPurpose.Factory:
			case StationPurpose.Scrapyard:
				return 1f;
			case StationPurpose.TradeStation:
				return 5f;
			default:
				return 0.5f;
			}
		}

		public bool ShouldShowSmallTargetBrackets(Unit unit, float distance)
		{
			if (unit.IsPlayerMissionPathTargetOrFirstWaypoint() || unit.IsPlayerCustomPathTargetOrFirstWaypointOrPathMarker())
			{
				return true;
			}
			switch (unit.UnitType)
			{
			case UnitType.Projectile:
				if (!GameController.Instance.GameSettings.HudSettings.ShowTargetBracketsForProjectiles)
				{
					return false;
				}
				if (engine.LocalPlayer != null && distance < SmallBracketsUnimportantDrawDist)
				{
					return unit.IsHostileTo(engine.LocalPlayer.Faction);
				}
				return false;
			case UnitType.Cargo:
				return distance < CargoSmallBracketstDrawDist;
			case UnitType.Ship:
			case UnitType.Wormhole:
			case UnitType.NavBuoy:
			case UnitType.Waypoint:
				return true;
			case UnitType.Station:
				if (unit.IsMinorStation())
				{
					return distance < SmallBracketsUnimportantDrawDist;
				}
				return true;
			default:
				if (unit.ActiveUnit != null)
				{
					return distance < SmallBracketsUnimportantDrawDist;
				}
				return false;
			}
		}

		private void Awake()
		{
			hud = UnityObjectHelper.FindInParentsOrSelf<HudScreen>(gameObject);
		}

		private void Start()
		{
			engine = EngineASX.Instance;
			Hud.CurrentTargetChanged += Hud_PlayerTargetChanged;
		}

		private void FillPool()
		{
			for (int i = 0; i < InitialPoolSize; i++)
			{
				AddToPool();
			}
		}

		private void Hud_PlayerTargetChanged(HudScreen sender, Unit oldTarget)
		{
			drawSize = 0f;
			int oldActiveCount = activeTargetsCount;
			RepopulateSmallWidgets();
			DeactivateUnusedPoolItems(oldActiveCount);
		}

		public void Tick()
		{
			if (engine.LocalPlayer != null)
			{
				UpdateSmallTargetWidgets();
				UpdateCurrentTargetWidget();
				CurrentTargetUI.Tick();
			}
		}

		private void UpdateSmallTargetWidgets()
		{
			int oldActiveCount = activeTargetsCount;
			if (RealTime.time > nextRepopulateSmallWidgetsTime)
			{
				RepopulateSmallWidgets();
				nextRepopulateSmallWidgetsTime = RealTime.time + RepopulateSmallWidgetsFrequency;
			}
			DeactivateUnusedPoolItems(oldActiveCount);
			RefreshActiveSmallWidgets();
		}

		private void RefreshActiveSmallWidgets()
		{
			Vector3 targetScreenPos = Vector3.zero;
			for (int i = 0; i < activeTargetsCount; i++)
			{
				HudTargetUI hudTargetUI = pool[i];
				Unit unit = hudTargetUI.Unit;
				if (ShouldShowForUnit(unit))
				{
					if (GetWidgetPos(unit.transform.position, out targetScreenPos))
					{
						hudTargetUI.gameObject.SetActive(value: true);
						hudTargetUI.transform.position = targetScreenPos;
						hudTargetUI.RefreshIconVisibility();
					}
					else
					{
						hudTargetUI.gameObject.SetActive(value: false);
					}
				}
			}
		}

		private void RepopulateSmallWidgets()
		{
			activeTargetsCount = 0;
			Unit playerUnit = Engine.PlayerUnit;
			possibleTargetPriorityQueue.Clear();
			if (playerUnit != null)
			{
				List<HudScannerUnit> scannedUnitCache = hud.AutoScanner.ScannedUnitCache;
				for (int i = 0; i < scannedUnitCache.Count; i++)
				{
					HudScannerUnit hudScannerUnit = scannedUnitCache[i];
					Unit unit = hudScannerUnit.Unit;
					if (unit != null && unit != Hud.CurrentTarget && ShouldShowForUnit(unit) && ShouldShowSmallTargetBrackets(unit, hudScannerUnit.DistanceFromLocalUnitIgnoringY) && GetWidgetPos(unit.transform.position, out var _))
					{
						float smallBracketsPriority = GetSmallBracketsPriority(unit, hudScannerUnit.DistanceFromLocalUnitIgnoringY);
						possibleTargetPriorityQueue.Enqueue(unit, smallBracketsPriority);
					}
				}
			}
			while (possibleTargetPriorityQueue.Count > 0 && activeTargetsCount < MaxSmallWidgets)
			{
				Unit value = possibleTargetPriorityQueue.Dequeue().Value;
				HudTargetUI orAddToPool = GetOrAddToPool();
				GetWidgetPos(value.transform.position, out var targetScreenPos2);
				orAddToPool.transform.position = targetScreenPos2;
				orAddToPool.Unit = value;
				orAddToPool.gameObject.SetActive(value: true);
				orAddToPool.RefreshBracketsColor();
				orAddToPool.RefreshIconVisibility();
				orAddToPool.BracketsWidget.sprite = GetBracketsSprite(value);
				activeTargetsCount++;
			}
		}

		private Sprite GetBracketsSprite(Unit unit)
		{
			return unit.UnitType switch
			{
				UnitType.Cargo => SmallBracketsCargoSprite, 
				UnitType.Ship => SmallBracketsShipSprite, 
				_ => SmallBracketsSprite, 
			};
		}

		private void DeactivateUnusedPoolItems(int oldActiveCount)
		{
			for (int i = activeTargetsCount; i < oldActiveCount; i++)
			{
				pool[i].gameObject.SetActive(value: false);
			}
		}

		private void OnDisable()
		{
			ClearActiveTargets();
		}

		private void UpdateCurrentTargetWidget()
		{
			Vector3 targetScreenPos = Vector3.zero;
			bool active = false;
			Unit currentTarget = Hud.CurrentTarget;
			if (currentTarget != lastTarget)
			{
				lastTarget = currentTarget;
				drawSize = 0f;
			}
			if (currentTarget != null && ShouldShowForUnit(currentTarget))
			{
				Vector3 position = currentTarget.transform.position;
				if (GetWidgetPos(position, out targetScreenPos))
				{
					CurrentTargetUI.transform.position = targetScreenPos;
					Vector3 targetScreenPos2 = Vector3.zero;
					GetWidgetPos(position + GameController.Instance.MainCamera.transform.right * currentTarget.UnitClass.ShieldRingRadius * currentTarget.UnitClass.DisplayData.HUDTargetBracketsRadiusMultiplier, out targetScreenPos2);
					Vector3 vector = CurrentTargetUI.transform.InverseTransformPoint(targetScreenPos2);
					vector.z = 0f;
					float b = Mathf.Clamp(vector.magnitude * 2f, MinTargetBracketsDist, MaxTargetBracketsDist);
					drawSize = Mathf.Lerp(drawSize, b, Mathf.Clamp01(TargetBracketSizeLerpRate * RealTime.deltaTime));
					CurrentTargetUI.RectTransform.SetWidth(drawSize);
					CurrentTargetUI.RectTransform.SetHeight(drawSize);
					active = true;
					CurrentTargetUI.Unit = currentTarget;
					CurrentTargetUI.Refresh();
				}
				else
				{
					drawSize = 0f;
				}
			}
			CurrentTargetUI.gameObject.SetActive(active);
		}
	}
}
