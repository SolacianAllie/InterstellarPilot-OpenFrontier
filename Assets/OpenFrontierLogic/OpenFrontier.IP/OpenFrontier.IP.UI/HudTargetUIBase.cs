using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class HudTargetUIBase : MonoBehaviour
	{
		protected EngineASX engine;

		private bool commsActive;

		public Graphic CommsWidget;

		public Graphic MissionPathGraphic;

		public Graphic CustomPathGraphic;

		public HudTargetController TargetController;

		public UnitConstructionControllerUI UnitConstructionController;

		protected Unit unit;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				if (unit != value)
				{
					unit = value;
					if (unit != null)
					{
						OnNewUnitSet();
					}
					UnitConstructionController.Unit = unit;
				}
			}
		}

		private void Awake()
		{
			engine = EngineASX.Instance;
			CustomPathGraphic.color = engine.CustomPathColor;
			MissionPathGraphic.color = engine.MissionPathColor;
			CommsWidget.gameObject.SetActive(value: false);
			MissionPathGraphic.enabled = false;
			CustomPathGraphic.enabled = false;
			awake();
		}

		protected virtual void awake()
		{
		}

		public virtual bool ShouldShowUnderConstructionInfo()
		{
			if (unit.IsUnderConstructionOrDismantling && unit.ActiveUnit != null)
			{
				return unit.ActiveUnit.LastDistanceFromCamera < engine.GameSettings.UIUnderConstructionDrawDistance;
			}
			return false;
		}

		public virtual void Refresh()
		{
			RefreshIconVisibility();
		}

		public virtual void RefreshIconVisibility()
		{
			if (Unit != null && Unit.IsValidAndNotDestroyed)
			{
				ShowOrHideComms(Unit);
				ShowOrHideMissionPathGraphic(Unit);
				ShowOrHideCustomPathGraphic(Unit);
				UnitConstructionController.Refresh();
			}
		}

		public Color CalculateBracketsColor(Unit unit)
		{
			EngineASX engineASX = TargetController.Engine;
			if (engineASX != null)
			{
				Color factionHostilityColor = engineASX.GetFactionHostilityColor(unit.Faction, engineASX.LocalFaction);
				factionHostilityColor.a = GetDrawAlpha(unit);
				return factionHostilityColor;
			}
			return Color.white;
		}

		protected virtual void OnNewUnitSet()
		{
		}

		protected virtual float GetDrawAlpha(Unit unit)
		{
			float num = Mathf.Lerp(TargetController.TargetSpriteMaxAlpha, TargetController.TargetSpriteMinAlpha, Mathf.Clamp01(unit.ActiveUnit.LastDistanceFromCamera / TargetController.TargetSpriteMinAlphaDist));
			UnitType unitType = unit.UnitType;
			if (unitType == UnitType.Wormhole || unitType == UnitType.Waypoint)
			{
				return Mathf.Max(TargetController.TargetSpriteMinWormholeAlpha, num);
			}
			return num;
		}

		private void ShowOrHideMissionPathGraphic(Unit unit)
		{
			MissionPathGraphic.enabled = unit.IsPlayerMissionPathTargetOrFirstWaypoint();
		}

		private void ShowOrHideCustomPathGraphic(Unit unit)
		{
			CustomPathGraphic.enabled = unit.IsPlayerCustomPathTargetOrFirstWaypointOrPathMarker();
		}

		private void ShowOrHideComms(Unit Unit)
		{
			bool active = false;
			bool flag = false;
			if (Unit.Components != null && Unit.Components.PilotPerson != null)
			{
				SpeechModel speechModel = TargetController.Hud.SpeechModel;
				SpeechModel.SpeechRequest requestFromPilot = speechModel.GetRequestFromPilot(Unit.Components.PilotPerson);
				if (requestFromPilot != null && requestFromPilot.ShowTimeElapsed && speechModel.ActiveRequest == requestFromPilot)
				{
					flag = true;
					active = true;
				}
			}
			if (flag != commsActive)
			{
				commsActive = flag;
			}
			CommsWidget.gameObject.SetActive(active);
		}
	}
}
