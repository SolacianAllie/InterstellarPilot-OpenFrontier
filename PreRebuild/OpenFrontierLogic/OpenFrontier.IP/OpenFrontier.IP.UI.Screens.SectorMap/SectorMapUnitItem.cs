using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapUnitItem : SectorMapItem
	{
		public Unit Unit;

		public TextMeshProUGUI UnitNameLabel;

		public bool UpdateImageSpriteEnabled = true;

		public bool ShowLabel { get; set; }

		public bool ShowSprite { get; set; }

		public override float MinDrawSize
		{
			get
			{
				if (Unit.IsPlayerCurrentUnitOrRoot)
				{
					return SectorMap.MinPlayerUnitDrawSize;
				}
				if (Unit.UnitClass.DisplayData != null && Unit.UnitClass.DisplayData.SectorMapMinDrawSize >= 0f)
				{
					return Unit.UnitClass.DisplayData.SectorMapMinDrawSize;
				}
				return SectorMap.MinDrawSize;
			}
		}

		public override void Refresh()
		{
			base.Refresh();
			if (Unit != null)
			{
				IsSelectable = !Unit.IsPlayerCurrentUnit;
				if (UpdateImageSpriteEnabled)
				{
					UpdateImageSprite();
				}
			}
		}

		public string GetLabelText()
		{
			if (Unit.IsOwnedByPlayer)
			{
				Fleet fleet = Unit.GetFleet();
				if (fleet != null)
				{
					if (fleet.Ships.Count > 1)
					{
						if (SectorMap.SectorMapCurrentTargetController != null && fleet == SectorMap.SectorMapCurrentTargetController.SelectedFleet())
						{
							return "<b>" + Unit.GetFleet().GetFriendlyName() + "</b><br>" + OrdersHelper.GetOrdersTextAndFleetStatus(Unit.GetFleet(), EngineASX.Instance.LocalFaction);
						}
						return "<b>" + Unit.GetFleet().GetFriendlyName() + "</b>";
					}
					if (SectorMap.SectorMapCurrentTargetController != null && fleet == SectorMap.SectorMapCurrentTargetController.SelectedFleet())
					{
						return "<b>" + UnitNamer.GetNameAndFactionShortNameInParenthesisForPlayer(Unit, shortName: true) + "</b><br>" + OrdersHelper.GetOrdersTextAndFleetStatus(Unit.GetFleet(), EngineASX.Instance.LocalFaction);
					}
					return "<b>" + UnitNamer.GetNameAndFactionShortNameInParenthesisForPlayer(Unit, shortName: true) + "</b>";
				}
			}
			return UnitNamer.GetNameAndFactionShortNameInParenthesisForPlayer(Unit, shortName: true);
		}

		public void UpdateImageSprite()
		{
			Image.sprite = EngineASX.Instance.EngineResources.GetUnitClassThumbnailIconSpriteOrDefault(Unit.UnitClass);
			Image.color = GetImageSpriteColor();
		}

		private Color GetImageSpriteColor()
		{
			if (Unit.ShowWithOwnershipColours)
			{
				return EngineASX.Instance.GetFactionHostilityColor(Unit.Faction, EngineASX.Instance.LocalFaction);
			}
			UnitType unitType = Unit.UnitClass.UnitType;
			if ((unitType == UnitType.GasCloud || unitType == UnitType.Waypoint) && Unit.UnitClass.DisplayData != null)
			{
				return Unit.UnitClass.DisplayData.ThumbnailIconSpriteColor;
			}
			return Color.white;
		}

		protected override Vector3 GetRotation()
		{
			return new Vector3(0f, 0f, 0f - Unit.transform.eulerAngles.y);
		}

		protected override Vector3 GetWorldPosition()
		{
			return Unit.transform.position;
		}

		protected override Vector2? GetSize()
		{
			return SectorMap.GetUnitDrawSizes(Unit);
		}

		protected override bool ShouldShowSprite()
		{
			if (Unit != null)
			{
				return ShowSprite;
			}
			return false;
		}

		protected virtual string GetLabel()
		{
			if (Unit.UnitType == UnitType.Wormhole)
			{
				Wormhole component = Unit.GetComponent<Wormhole>();
				if (component != null)
				{
					Sector actualTargetSector = component.ActualTargetSector;
					if (actualTargetSector != null)
					{
						return "To " + actualTargetSector.Name;
					}
					return "Wormhole";
				}
			}
			return Unit.GetFriendlyName();
		}
	}
}
