using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Components;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Hud.CurrentTargetUI.WeaponsReadyIndicator;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class HudCurrentTargetUI : HudTargetUIBase
	{
		public CloakImageComponent CloakImageComponent;

		public GameObject CloakedObject;

		public Graphic CornerImage;

		public Vector3 IconOffset;

		public float IconSpacing = 32f;

		public GameObject InCombatObject;

		public RectTransform RectTransform;

		public float AlphaChangeRate = 1f;

		public TextMeshProUGUI CurrentFleetText;

		public TextMeshProUGUI CurrentFleetOrderText;

		public Image IdleImage;

		private float currentAlpha;

		public WeaponsReadyIndicatorController WeaponsReadyIndicatorController;

		private float lastTimeUpdatedName;

		protected override void awake()
		{
			base.awake();
			SetTransparentAndUpdateColor();
			InCombatObject.SetActive(value: false);
			CurrentFleetText.enabled = false;
		}

		private void OnEnable()
		{
			SetTransparentAndUpdateColor();
		}

		private void SetTransparentAndUpdateColor()
		{
			currentAlpha = 0f;
			UpdateColor();
		}

		private void OnDisable()
		{
			SetTransparentAndUpdateColor();
		}

		protected override void OnNewUnitSet()
		{
			base.OnNewUnitSet();
			SetTransparentAndUpdateColor();
			RefreshCurrentFleetName();
			CloakImageComponent.ClearState();
			CloakImageComponent.TargetCloakComponent = unit.CloakComponent;
		}

		private void RefreshCurrentFleetName()
		{
			if (unit != null && unit.IsOwnedByPlayer)
			{
				Fleet fleet = unit.GetFleet();
				if (fleet != null)
				{
					bool flag = OrdersHelper.HasFleetGotStatusToDisplay(fleet);
					CurrentFleetText.enabled = true;
					if (fleet.Ships.Count > 1)
					{
						CurrentFleetText.text = fleet.GetFriendlyName();
					}
					else
					{
						CurrentFleetText.text = UnitNamer.GetNameAndFactionShortNameInParenthesisForPlayer(Unit, shortName: true);
					}
					CurrentFleetOrderText.enabled = flag;
					IdleImage.enabled = !flag;
					if (flag)
					{
						CurrentFleetOrderText.text = OrdersHelper.GetOrdersTextAndFleetStatus(fleet, EngineASX.Instance.LocalFaction);
					}
				}
				else
				{
					TextMeshProUGUI currentFleetText = CurrentFleetText;
					TextMeshProUGUI currentFleetOrderText = CurrentFleetOrderText;
					bool flag2 = (IdleImage.enabled = false);
					bool flag4 = (currentFleetOrderText.enabled = flag2);
					currentFleetText.enabled = flag4;
				}
			}
			else
			{
				TextMeshProUGUI currentFleetText2 = CurrentFleetText;
				TextMeshProUGUI currentFleetOrderText2 = CurrentFleetOrderText;
				bool flag2 = (IdleImage.enabled = false);
				bool flag4 = (currentFleetOrderText2.enabled = flag2);
				currentFleetText2.enabled = flag4;
			}
		}

		public override void Refresh()
		{
			base.Refresh();
			bool active = false;
			if (EngineASX.Instance.LocalUnit != null && EngineASX.Instance.LocalUnit.Components != null)
			{
				WeaponsReadyIndicatorController.Refresh(EngineASX.Instance.LocalUnit.Components);
			}
			if (unit == TargetController.Hud.CurrentTarget && unit.NpcPilot != null && unit.NpcPilot.HasCombatTargetOrGroupInCombat)
			{
				active = true;
			}
			InCombatObject.gameObject.SetActive(active);
			UpdateColor();
		}

		public override bool ShouldShowUnderConstructionInfo()
		{
			return unit.IsUnderConstructionOrDismantling;
		}

		private void UpdateColor()
		{
			Color cornerImageColor = ((!(Unit != null)) ? CornerImage.color : GetWidgetColor(unit));
			cornerImageColor.a = currentAlpha;
			SetCornerImageColor(cornerImageColor);
			Color color = CurrentFleetText.color;
			color.a = currentAlpha;
			CurrentFleetText.color = color;
		}

		protected override float GetDrawAlpha(Unit unit)
		{
			return TargetController.CurrentTargetAlpha;
		}

		private Color GetWidgetColor(Unit unit)
		{
			return CalculateBracketsColor(unit);
		}

		private void SetCornerImageColor(Color c)
		{
			CornerImage.color = c;
		}

		public void Tick()
		{
			if (currentAlpha < 1f)
			{
				currentAlpha += AlphaChangeRate * RealTime.deltaTime;
			}
			if (Time.time > lastTimeUpdatedName + 1f)
			{
				RefreshCurrentFleetName();
				lastTimeUpdatedName = Time.time;
			}
			Unit localUnit = EngineASX.Instance.LocalUnit;
			if (localUnit != null)
			{
				WeaponsReadyIndicatorController.Refresh(localUnit.Components);
			}
			CloakImageComponent.Tick();
		}
	}
}
